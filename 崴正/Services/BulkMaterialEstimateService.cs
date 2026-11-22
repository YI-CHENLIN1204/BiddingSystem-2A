using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services;

public class BulkMaterialEstimateService(ApplicationDbContext context, ProjectSimilarityService similarityService,
    EstimationSettingsService settingsService)
{
    public async Task<object> GetOptionsAsync(CancellationToken ct)
    {
        var materials = await context.MaterialMasters.AsNoTracking().Where(x => x.IsActive && x.UnitMaster.IsActive)
            .Select(x => new BulkMaterialOption(x.MaterialId, x.MaterialName, x.UnitMaster.UnitName, x.MaterialCategory.CategoryName))
            .ToListAsync(ct);
        return new { profiles = BulkMaterialRangeCalculator.Profiles.Select(p => new {
            p.Key, p.Name, p.Unit, p.Basis, p.Structures, p.Notice,
            materials = materials.Where(m => BulkMaterialRangeCalculator.MatchesMaterial(p, m)).ToArray()
        }) };
    }

    public async Task<IReadOnlyList<BulkMaterialObservedQuantity>?> GetVersionQuantitiesAsync(
        int caseId, long versionId, MaterialMappingService mapping, CancellationToken ct)
    {
        var version = await context.AnalysisBidVersions.AsNoTracking()
            .Where(v => v.AnalysisCaseId == caseId && v.AnalysisBidVersionId == versionId)
            .Select(v => new { Items = v.Items.Select(i => new {
                i.ItemName, i.Spec, i.Unit, i.Quantity, i.MajorCategory,
                i.SourceCategory1, i.SourceCategory2, i.SourceCategory3
            }).ToList() }).SingleOrDefaultAsync(ct);
        if (version == null) return null;

        var lookup = await mapping.GetReadOnlyLookupAsync();
        var activeMaterials = await context.MaterialMasters.AsNoTracking()
            .Where(m => m.IsActive && m.UnitMaster.IsActive)
            .Select(m => m.MaterialId).ToListAsync(ct);
        var activeIds = activeMaterials.ToHashSet();
        var matches = version.Items
            .Where(i => !BulkMaterialRangeCalculator.IsServiceOnly(i.ItemName))
            .Select(i => (Item: i, Material: mapping.FindBestMatch(lookup, i.ItemName, i.Spec)))
            .Where(x => x.Material != null && activeIds.Contains(x.Material.MaterialId))
            .GroupBy(x => x.Material!.MaterialId);
        var results = new List<BulkMaterialObservedQuantity>();
        foreach (var group in matches)
        {
            var material = group.First().Material!;
            var option = new BulkMaterialOption(material.MaterialId, material.MaterialName, material.UnitName, material.Category ?? "");
            foreach (var profile in BulkMaterialRangeCalculator.Profiles.Where(p => BulkMaterialRangeCalculator.MatchesMaterial(p, option)))
            {
                var rows = group.Select(x => x.Item).Where(i => BulkMaterialRangeCalculator.IsAllowedMajor(profile.Key,
                    TwoAScopePolicy.ResolveEffectiveMajorCategory(i.MajorCategory, i.SourceCategory1, i.SourceCategory2, i.SourceCategory3))).ToList();
                if (rows.Count == 0) continue;
                decimal total = 0;
                bool complete = true;
                foreach (var row in rows)
                {
                    if (row.Quantity is not >= 0 || !BulkMaterialRangeCalculator.TryNormalizeQuantity(row.Quantity.Value, row.Unit, profile.Unit, out var quantity))
                    {
                        complete = false;
                        break;
                    }
                    total += quantity;
                }
                results.Add(new(profile.Key, material.MaterialId, material.MaterialName, profile.Unit,
                    complete ? total : null, rows.Count));
            }
        }
        return results;
    }

    // Exposed as IQueryable so the regression harness can validate Npgsql SQL
    // translation without opening a production or synthetic DB connection.
    public IQueryable<BulkMaterialHistoricalRow> BuildHistoryQuery(int materialId, int[] projectIds) =>
        from tx in context.HistoricalTransactions.AsNoTracking()
        join bid in context.FinalBidItems.AsNoTracking() on tx.FinalBidItemId equals (long?)bid.FinalBidItemId into sources
        from bid in sources.DefaultIfEmpty()
        where tx.MaterialId == materialId && tx.ProjectMasterId.HasValue && projectIds.Contains(tx.ProjectMasterId.Value) &&
            !tx.IsDeleted
        select new BulkMaterialHistoricalRow(tx.TransactionId, tx.ProjectMasterId!.Value, tx.FinalBidItemId, tx.ActualQuantity,
            bid == null ? null : bid.ProjectMasterId, bid == null ? null : bid.MaterialId,
            bid == null ? null : bid.MappingStatus, bid != null && bid.IsIgnored,
            bid == null ? "" : bid.ItemName, bid == null ? null : bid.Unit, bid == null ? null : bid.Qty,
            bid == null ? null : bid.MajorCategoryOverride, bid == null ? null : bid.Category1,
            bid == null ? null : bid.Category2, bid == null ? null : bid.Category3, tx.IsOutlier);

    public IQueryable<BulkMaterialSource> BuildCoverageQuery(int materialId, int[] projectIds) =>
        context.FinalBidItems.AsNoTracking()
            .Where(x => projectIds.Contains(x.ProjectMasterId) && x.MaterialId == materialId && !x.IsIgnored)
            .Select(x => new BulkMaterialSource(x.ProjectMasterId, x.FinalBidItemId));

    public IQueryable<BulkMaterialHistoricalRow> BuildSourceQuery(int materialId, int[] projectIds) =>
        context.FinalBidItems.AsNoTracking()
            .Where(x => projectIds.Contains(x.ProjectMasterId) && x.MaterialId == materialId && !x.IsIgnored)
            .Select(x => new BulkMaterialHistoricalRow(0, x.ProjectMasterId, x.FinalBidItemId, 0,
                x.ProjectMasterId, x.MaterialId, x.MappingStatus, x.IsIgnored, x.ItemName, x.Unit, x.Qty,
                x.MajorCategoryOverride, x.Category1, x.Category2, x.Category3, false, false));

    public IQueryable<long?> BuildBlockedSourcesQuery(int materialId, int[] projectIds) =>
        context.HistoricalTransactions.AsNoTracking()
            .Where(x => x.MaterialId == materialId && x.ProjectMasterId.HasValue &&
                projectIds.Contains(x.ProjectMasterId.Value) && x.IsDeleted && x.FinalBidItemId.HasValue)
            .Select(x => x.FinalBidItemId).Distinct();

    public async Task<BulkMaterialEstimateResult> EstimateAsync(BulkMaterialEstimateRequest request, CancellationToken ct)
    {
        BulkMaterialRangeCalculator.ValidateRequest(request);
        await using var snapshot = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead, ct);
        var selected = await context.MaterialMasters.AsNoTracking()
            .Where(x => x.MaterialId == request.MaterialId && x.IsActive && x.UnitMaster.IsActive)
            .Select(x => new BulkMaterialOption(x.MaterialId, x.MaterialName, x.UnitMaster.UnitName, x.MaterialCategory.CategoryName))
            .SingleOrDefaultAsync(ct) ?? throw new ArgumentException("所選標準材料不存在或已停用。");
        var profile = BulkMaterialRangeCalculator.Profiles.Single(x => x.Key == request.ProfileKey);
        if (!BulkMaterialRangeCalculator.MatchesMaterial(profile, selected))
            throw new ArgumentException("所選材料與工項或單位不符。");
        var settings = await settingsService.GetCurrentAsync();
        var scores = await similarityService.GetScoredCandidateProjectsAsync(request.Target);
        // All new profiles keep exact structure equality, including SC targets.
        var ids = scores.Where(x => x.StructureType == request.Target.StructureType && x.ProjectId != request.ExcludeProjectId)
            .Select(x => x.ProjectId).ToArray();
        var projects = await context.ProjectMasters.AsNoTracking().Where(x => ids.Contains(x.ProjectId))
            .Select(x => new { x.ProjectId, x.ProjectName, x.ProjectType, x.StructureType, x.City, x.TotalFloorArea, x.SiteArea,
                x.FloorCount, x.BasementFloorCount, x.StartDate, x.HistoryStatus, x.IsDeleted }).ToListAsync(ct);
        var byId = scores.ToDictionary(x => x.ProjectId);
        var candidates = projects.Select(x => new BulkMaterialCandidate(x.ProjectId, x.ProjectName,
            x.ProjectType ?? "", x.StructureType ?? "", x.TotalFloorArea ?? 0, x.SiteArea ?? 0, x.FloorCount ?? 0,
            x.BasementFloorCount ?? -1, x.StartDate ?? default, x.HistoryStatus, x.IsDeleted,
            byId[x.ProjectId].SimilarityScore, x.City ?? "")).ToArray();
        var rows = await BuildHistoryQuery(selected.MaterialId, ids).ToListAsync(ct);
        var finalRows = await BuildSourceQuery(selected.MaterialId, ids).ToListAsync(ct);
        var linked = rows.Where(x => x.FinalBidItemId.HasValue).Select(x => x.FinalBidItemId!.Value).ToHashSet();
        var blocked = (await BuildBlockedSourcesQuery(selected.MaterialId, ids).ToListAsync(ct)).ToHashSet();
        // The legacy publisher intentionally writes only rebar/concrete. New
        // bulk materials therefore use approved mapped final bid rows directly.
        // Never resurrect explicitly deleted quantity history as a fallback.
        rows.AddRange(finalRows.Where(x => !linked.Contains(x.FinalBidItemId!.Value))
            .Select(x => blocked.Contains(x.FinalBidItemId) ? x with { HistoryIsOutlier = true } : x));
        // Resolve aliases using the same project-wide source heading policy.
        rows = rows.Select(x => x with { MajorCategory = TwoAScopePolicy.ResolveEffectiveMajorCategory(
            x.MajorCategory, x.SourceCategory1, x.SourceCategory2, x.SourceCategory3) }).ToList();
        var sources = await BuildCoverageQuery(selected.MaterialId, ids).ToListAsync(ct);
        var result = BulkMaterialRangeCalculator.Calculate(request, selected, settings, candidates, rows, sources);
        await snapshot.CommitAsync(ct);
        return result;
    }
}

