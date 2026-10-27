using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class DetectionCategoryRangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectSimilarityService _similarityService;
        private readonly EstimationSettingsService _settingsService;
        private readonly UnitConversionService _unitConversionService;


        public DetectionCategoryRangeService(
            ApplicationDbContext context,
            ProjectSimilarityService similarityService,
            EstimationSettingsService settingsService,
            UnitConversionService unitConversionService)
        {
            _context = context;
            _similarityService =
                similarityService;
            _settingsService =
                settingsService;
            _unitConversionService =
                unitConversionService;
        }


        public async Task<ServiceResult> EstimateAsync(
            int detectionCategoryId,
            DetectionCategoryRangeRequest request)
        {
            if (request == null ||
                request.Target == null)
            {
                return ServiceResult.Validation(
                    "請提供建案條件");
            }

            if (request.TopCount.HasValue &&
                request.TopCount.Value <= 0)
            {
                return ServiceResult.Validation(
                    "TopCount 必須大於 0");
            }

            var category =
                await _context.DetectionCategories
                    .AsNoTracking()
                    .Include(x => x.UnitMaster)
                    .FirstOrDefaultAsync(x =>
                        x.DetectionCategoryId ==
                            detectionCategoryId);

            if (category == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的偵錯細項");
            }

            if (!category.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的偵錯細項目前已停用");
            }

            var materialIds =
                await _context
                    .DetectionCategoryMaterialMaps
                    .AsNoTracking()
                    .Where(x =>
                        x.DetectionCategoryId ==
                            detectionCategoryId &&
                        x.IsActive)
                    .Select(x =>
                        x.MaterialId)
                    .Distinct()
                    .ToListAsync();

            var settings =
                await _settingsService
                    .GetCurrentAsync();

            int takeCount =
                request.TopCount ??
                settings.TopK;

            var result =
                new DetectionCategoryRangeResultDto
                {
                    DetectionCategoryId =
                        category.DetectionCategoryId,

                    MajorCategoryName =
                        category.MajorCategoryName,

                    DetectionCategoryName =
                        category.DetectionCategoryName,

                    UnitName =
                        category.UnitMaster.UnitName,

                    UsagePerAreaUnit =
                        $"{category.UnitMaster.UnitName}/m²"
                };

            if (!materialIds.Any())
            {
                result.StatusMessage =
                    "此偵錯細項尚未對應任何標準材料";

                return ServiceResult.Ok(
                    result
                );
            }

            List<ProjectSimilarityDto>
                similarityCandidates;

            try
            {
                similarityCandidates =
                    await _similarityService
                        .GetScoredCandidateProjectsAsync(
                            request.Target
                        );
            }
            catch (ArgumentException ex)
            {
                return ServiceResult.Validation(
                    ex.Message
                );
            }

            result.CandidateProjectCount =
                similarityCandidates.Count;

            if (!similarityCandidates.Any())
            {
                result.StatusMessage =
                    "沒有符合條件的相似建案";

                return ServiceResult.Ok(
                    result
                );
            }

            var candidateIds =
                similarityCandidates
                    .Select(x => x.ProjectId)
                    .Distinct()
                    .ToList();

            var rows =
                await (
                    from tx
                    in _context.HistoricalTransactions
                        .AsNoTracking()

                    join material
                    in _context.MaterialMasters
                        .AsNoTracking()
                    on tx.MaterialId
                    equals material.MaterialId

                    join unit
                    in _context.UnitMasters
                        .AsNoTracking()
                    on material.UnitId
                    equals unit.UnitId

                    where
                        tx.ProjectMasterId.HasValue &&
                        candidateIds.Contains(
                            tx.ProjectMasterId.Value
                        ) &&
                        materialIds.Contains(
                            tx.MaterialId
                        ) &&
                        !tx.IsDeleted &&
                        !tx.IsOutlier

                    select new
                    {
                        ProjectId =
                            tx.ProjectMasterId!.Value,

                        tx.ActualQuantity,

                        SourceUnitName =
                            unit.UnitName
                    }
                )
                .ToListAsync();

            var quantityByProject =
                new Dictionary<int, decimal>();

            foreach (var row in rows)
            {
                var conversion =
                    _unitConversionService.Convert(
                        row.ActualQuantity,
                        0m,
                        row.SourceUnitName,
                        category.UnitId
                    );

                if (!conversion.Success)
                {
                    result.SkippedTransactionCount++;
                    continue;
                }

                quantityByProject[
                    row.ProjectId] =
                    quantityByProject
                        .GetValueOrDefault(
                            row.ProjectId
                        )
                    +
                    conversion.Quantity;
            }

            result.AvailableProjectCount =
                quantityByProject.Count;

            if (!quantityByProject.Any())
            {
                result.StatusMessage =
                    "相似建案中沒有可換算至偵錯細項標準單位的歷史資料";

                return ServiceResult.Ok(
                    result
                );
            }

            var projectIds =
                quantityByProject.Keys
                    .ToList();

            var projectMetadata =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .Where(x =>
                        projectIds.Contains(
                            x.ProjectId
                        ) &&
                        !x.IsDeleted)
                    .Select(x => new
                    {
                        x.ProjectId,
                        x.ProjectName,
                        x.TotalFloorArea,
                        x.StartDate
                    })
                    .ToListAsync();

            var projectDictionary =
                projectMetadata
                    .ToDictionary(
                        x => x.ProjectId,
                        x => x
                    );

            var similarityDictionary =
                similarityCandidates
                    .ToDictionary(
                        x => x.ProjectId,
                        x => x
                    );

            var allReferences =
                new List<
                    DetectionCategoryRangeReferenceDto>();

            foreach (var entry
                in quantityByProject)
            {
                if (!projectDictionary
                    .TryGetValue(
                        entry.Key,
                        out var project))
                {
                    continue;
                }

                if (!similarityDictionary
                    .TryGetValue(
                        entry.Key,
                        out var similarity))
                {
                    continue;
                }

                if (!project.TotalFloorArea
                        .HasValue ||
                    project.TotalFloorArea.Value <= 0 ||
                    !project.StartDate.HasValue ||
                    entry.Value <= 0 ||
                    similarity.SimilarityScore <= 0)
                {
                    continue;
                }

                decimal usagePerArea =
                    entry.Value /
                    project.TotalFloorArea.Value;

                decimal similarityWeight =
                    (decimal)Math.Pow(
                        similarity.SimilarityScore /
                            100.0,
                        (double)
                        settings.SimilarityExponent
                    );

                double ageYears =
                    Math.Abs(
                        (
                            request.Target.ProjectDate -
                            project.StartDate.Value
                        )
                        .TotalDays
                    )
                    /
                    365.2425;

                decimal timeWeight =
                    (decimal)Math.Pow(
                        0.5,
                        ageYears /
                        (double)
                        settings.TimeHalfLifeYears
                    );

                decimal finalWeight =
                    similarityWeight *
                    timeWeight;

                if (finalWeight <= 0)
                {
                    continue;
                }

                allReferences.Add(
                    new
                    DetectionCategoryRangeReferenceDto
                    {
                        ProjectId =
                            project.ProjectId,

                        ProjectName =
                            project.ProjectName,

                        ProjectDate =
                            project.StartDate.Value,

                        FloorArea =
                            project.TotalFloorArea.Value,

                        ActualQuantity =
                            entry.Value,

                        UsagePerArea =
                            usagePerArea,

                        SimilarityScore =
                            similarity.SimilarityScore,

                        AgeYears =
                            ageYears,

                        SimilarityWeight =
                            similarityWeight,

                        TimeWeight =
                            timeWeight,

                        FinalWeight =
                            finalWeight
                    }
                );
            }

            var references =
                allReferences
                    .OrderByDescending(x =>
                        x.SimilarityScore)
                    .ThenByDescending(x =>
                        x.FinalWeight)
                    .Take(takeCount)
                    .ToList();

            result.ReferenceProjectCount =
                references.Count;

            result.References =
                references;

            if (references.Count <
                settings.MinimumSampleCount)
            {
                result.StatusMessage =
                    $"樣本不足：目前只有 {references.Count} 個有效參考建案，最低需要 {settings.MinimumSampleCount} 個。" +
                    " " + GetNormalDistributionResearchNote(references);

                RoundReferences(
                    result.References
                );

                return ServiceResult.Ok(
                    result
                );
            }

            decimal lowerUsage =
                CalculateWeightedQuantile(
                    references,
                    settings.LowerQuantile
                );

            decimal medianUsage =
                CalculateWeightedQuantile(
                    references,
                    settings.MedianQuantile
                );

            decimal upperUsage =
                CalculateWeightedQuantile(
                    references,
                    settings.UpperQuantile
                );

            result.LowerUsagePerArea =
                Math.Round(
                    lowerUsage,
                    6
                );

            result.MedianUsagePerArea =
                Math.Round(
                    medianUsage,
                    6
                );

            result.UpperUsagePerArea =
                Math.Round(
                    upperUsage,
                    6
                );

            result.LowerQuantity =
                Math.Round(
                    lowerUsage *
                    request.Target.TotalFloorArea,
                    2
                );

            result.MedianQuantity =
                Math.Round(
                    medianUsage *
                    request.Target.TotalFloorArea,
                    2
                );

            result.UpperQuantity =
                Math.Round(
                    upperUsage *
                    request.Target.TotalFloorArea,
                    2
                );

            result.HasEnoughSamples =
                true;

            result.StatusMessage =
                "偵錯細項合理用量區間估算完成。 " +
                GetNormalDistributionResearchNote(references);

            RoundReferences(
                result.References
            );

            return ServiceResult.Ok(
                result
            );
        }


        // Diagnostic only: sparse observations do not justify assuming a Gaussian distribution.
        // This does not change the published quantiles or anomaly classification.
        private static string GetNormalDistributionResearchNote(
            List<DetectionCategoryRangeReferenceDto> references)
        {
            var values = references.Select(x => (double)x.UsagePerArea)
                .Where(x => double.IsFinite(x) && x > 0).ToArray();
            if (values.Length < 8)
                return "常態分布候選研究：有效案件少於 8 件，無法可靠檢查分布形態；不可用常態假設補足樣本或直接正式判定。";

            double mean = values.Average();
            double variance = values.Sum(x => (x - mean) * (x - mean)) / (values.Length - 1);
            double sd = Math.Sqrt(variance);
            if (mean <= 0 || sd <= 1e-12)
                return "常態分布候選研究：數據變異過低，暫不適用以標準差推導區間。";

            double skewness = values.Sum(x => Math.Pow((x - mean) / sd, 3))
                * values.Length / ((values.Length - 1.0) * (values.Length - 2.0));
            double cv = sd / mean;
            return $"常態分布候選研究（僅描述統計，未套用模型）：樣本 {values.Length}，變異係數 {cv:F3}，偏態係數 {skewness:F3}；樣本仍須檢查來源真實性、獨立性及分布形態，不代表已符合常態假設。";
        }


        private static decimal
            CalculateWeightedQuantile(
                List<
                    DetectionCategoryRangeReferenceDto>
                        references,
                decimal quantile)
        {
            var ordered =
                references
                    .Where(x =>
                        x.FinalWeight > 0)
                    .OrderBy(x =>
                        x.UsagePerArea)
                    .ToList();

            if (!ordered.Any())
            {
                return 0m;
            }

            decimal totalWeight =
                ordered.Sum(x =>
                    x.FinalWeight);

            if (totalWeight <= 0)
            {
                return 0m;
            }

            decimal targetWeight =
                totalWeight *
                quantile;

            decimal cumulativeWeight =
                0m;

            foreach (var item in ordered)
            {
                cumulativeWeight +=
                    item.FinalWeight;

                if (cumulativeWeight >=
                    targetWeight)
                {
                    return item.UsagePerArea;
                }
            }

            return ordered
                .Last()
                .UsagePerArea;
        }


        private static void RoundReferences(
            List<
                DetectionCategoryRangeReferenceDto>
                    references)
        {
            foreach (var reference
                in references)
            {
                reference.FloorArea =
                    Math.Round(
                        reference.FloorArea,
                        2
                    );

                reference.ActualQuantity =
                    Math.Round(
                        reference.ActualQuantity,
                        4
                    );

                reference.UsagePerArea =
                    Math.Round(
                        reference.UsagePerArea,
                        6
                    );

                reference.SimilarityScore =
                    Math.Round(
                        reference.SimilarityScore,
                        2
                    );

                reference.AgeYears =
                    Math.Round(
                        reference.AgeYears,
                        2
                    );

                reference.SimilarityWeight =
                    Math.Round(
                        reference.SimilarityWeight,
                        6
                    );

                reference.TimeWeight =
                    Math.Round(
                        reference.TimeWeight,
                        6
                    );

                reference.FinalWeight =
                    Math.Round(
                        reference.FinalWeight,
                        6
                    );
            }
        }
    }
}
