using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class SimilarityEstimationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectSimilarityService _similarityService;

        public SimilarityEstimationService(
            ApplicationDbContext context,
            ProjectSimilarityService similarityService)
        {
            _context = context;
            _similarityService = similarityService;
        }

        public async Task<SimilarityEstimationDto?>
            EstimateMaterialQuantityAsync(
                int targetProjectId,
                int materialId,
                int topCount = 5)
        {
            // 1. 取得目標建案
            var targetProject = await _context.ProjectMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProjectId == targetProjectId &&
                    !p.IsDeleted);

            if (targetProject == null ||
                !targetProject.TotalFloorArea.HasValue ||
                targetProject.TotalFloorArea.Value <= 0)
            {
                return null;
            }

            // 2. 找 Top N 相似案件
            var similarProjects =
                await _similarityService.FindSimilarProjectsAsync(
                    targetProjectId,
                    topCount);

            if (!similarProjects.Any())
            {
                return new SimilarityEstimationDto
                {
                    TargetProjectId = targetProjectId,
                    MaterialId = materialId,
                    TargetFloorArea =
                        targetProject.TotalFloorArea.Value
                };
            }

            var similarProjectIds =
                similarProjects
                    .Select(x => x.ProjectId)
                    .ToList();

            /*
             * 3. 找這些相似案件中的指定材料
             *
             * 注意：
             * 同一建案可能有多筆相同材料，
             * 所以必須先依 ProjectMasterId 分組，
             * 再 SUM ActualQuantity。
             */
            var usageData =
                await _context.HistoricalTransactions
                    .AsNoTracking()
                    .Where(h =>
                        h.ProjectMasterId.HasValue &&
                        similarProjectIds.Contains(
                            h.ProjectMasterId.Value) &&
                        h.MaterialId == materialId &&
                        !h.IsDeleted &&
                        !h.IsOutlier)
                    .GroupBy(h => h.ProjectMasterId!.Value)
                    .Select(g => new
                    {
                        ProjectId = g.Key,

                        ActualQuantity =
                            g.Sum(x => x.ActualQuantity)
                    })
                    .ToListAsync();

            // 4. 結合相似度與建案面積
            var references =
                new List<SimilarityEstimationReferenceDto>();

            foreach (var usage in usageData)
            {
                var similarity =
                    similarProjects.FirstOrDefault(
                        x => x.ProjectId == usage.ProjectId);

                if (similarity == null ||
                    !similarity.TotalFloorArea.HasValue ||
                    similarity.TotalFloorArea.Value <= 0)
                {
                    continue;
                }

                decimal usagePerArea =
                    usage.ActualQuantity /
                    similarity.TotalFloorArea.Value;

                references.Add(
                    new SimilarityEstimationReferenceDto
                    {
                        ProjectId = similarity.ProjectId,

                        ProjectName =
                            similarity.ProjectName,

                        SimilarityScore =
                            similarity.SimilarityScore,

                        FloorArea =
                            similarity.TotalFloorArea.Value,

                        ActualQuantity =
                            usage.ActualQuantity,

                        UsagePerArea =
                            usagePerArea
                    });
            }

            if (!references.Any())
            {
                return new SimilarityEstimationDto
                {
                    TargetProjectId = targetProjectId,
                    MaterialId = materialId,
                    TargetFloorArea =
                        targetProject.TotalFloorArea.Value
                };
            }

            /*
             * 5. 相似度加權平均
             *
             * 單位用量 =
             * Σ(單位用量 × 相似度)
             * -------------------
             * Σ(相似度)
             */
            double totalWeight =
                references.Sum(x => x.SimilarityScore);

            if (totalWeight <= 0)
            {
                return null;
            }

            decimal weightedUsagePerArea =
                references.Sum(x =>
                    x.UsagePerArea *
                    (decimal)x.SimilarityScore)
                /
                (decimal)totalWeight;

            // 6. 乘回目標建案面積
            decimal estimatedQuantity =
                weightedUsagePerArea *
                targetProject.TotalFloorArea.Value;

            return new SimilarityEstimationDto
            {
                TargetProjectId = targetProjectId,

                MaterialId = materialId,

                TargetFloorArea =
                    targetProject.TotalFloorArea.Value,

                WeightedUsagePerArea =
                    Math.Round(weightedUsagePerArea, 6),

                EstimatedQuantity =
                    Math.Round(estimatedQuantity, 2),

                ReferenceProjectCount =
                    references.Count,

                References =
                    references
            };
        }
    }
}