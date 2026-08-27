using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class RangeEstimationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectSimilarityService _similarityService;
        private readonly EstimationSettingsService _settingsService;
        private readonly EstimationTargetService _targetService;

        public RangeEstimationService(
            ApplicationDbContext context,
            ProjectSimilarityService similarityService,
            EstimationSettingsService settingsService,
            EstimationTargetService targetService)
        {
            _context = context;
            _similarityService = similarityService;
            _settingsService = settingsService;
            _targetService = targetService;
        }

        public Task<ServiceResult>
            EstimateMaterialRangeAsync(
                int targetProjectId,
                int materialId)
        {
            return EstimateMaterialRangeAsync(
                new EstimationTargetRequest
                {
                    TargetMode =
                        EstimationTargetMode.HistoricalProject,

                    HistoricalProjectId =
                        targetProjectId
                },
                materialId
            );
        }

        public async Task<ServiceResult>
            EstimateMaterialRangeAsync(
                EstimationTargetRequest targetRequest,
                int materialId)
        {
            var targetResult =
                await _targetService.ResolveAsync(
                    targetRequest
                );

            if (!targetResult.Success)
            {
                return targetResult;
            }

            if (
                targetResult.Data
                is not ResolvedEstimationTarget resolvedTarget
            )
            {
                return ServiceResult.Validation(
                    "估算目標解析失敗"
                );
            }

            return await EstimateResolvedMaterialRangeAsync(
                resolvedTarget,
                materialId
            );
        }

        public Task<ServiceResult>
            EstimateMaterialRangeAsync(
                ResolvedEstimationTarget resolvedTarget,
                int materialId,
                List<ProjectSimilarityDto> similarityCandidates)
        {
            if (resolvedTarget == null)
            {
                return Task.FromResult(
                    ServiceResult.Validation(
                        "估算目標不可為空"
                    )
                );
            }

            if (similarityCandidates == null)
            {
                return Task.FromResult(
                    ServiceResult.Validation(
                        "相似建案資料不可為空"
                    )
                );
            }

            return EstimateResolvedMaterialRangeAsync(
                resolvedTarget,
                materialId,
                similarityCandidates
            );
        }


        private async Task<ServiceResult>
            EstimateResolvedMaterialRangeAsync(
                ResolvedEstimationTarget resolvedTarget,
                int materialId,
                List<ProjectSimilarityDto>? precomputedSimilarityCandidates = null)
        {
            var settings =
                await _settingsService.GetCurrentAsync();



            // ==========================================
            // 2. 標準材料
            // ==========================================

            var material =
                await _context.MaterialMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.MaterialId == materialId
                    );

            if (material == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的標準材料"
                );
            }


            var result =
                new RangeEstimationDto
                {
                    TargetMode =
                        resolvedTarget.SourceMode,

                    TargetProjectId =
                        resolvedTarget.HistoricalProjectId,

                    TargetProjectName =
                        resolvedTarget.DisplayName,

                    TargetProjectType =
                        resolvedTarget
                            .SimilarityTarget
                            .ProjectType,

                    TargetStructureType =
                        resolvedTarget
                            .SimilarityTarget
                            .StructureType,

                    TargetCity =
                        resolvedTarget
                            .SimilarityTarget
                            .City,

                    TargetFloorArea =
                        resolvedTarget
                            .SimilarityTarget
                            .TotalFloorArea,

                    TargetFloorCount =
                        resolvedTarget
                            .SimilarityTarget
                            .FloorCount,

                    TargetBasementFloorCount =
                        resolvedTarget
                            .SimilarityTarget
                            .BasementFloorCount,

                    TargetProjectDate =
                        resolvedTarget
                            .SimilarityTarget
                            .ProjectDate,

                    MaterialId =
                        material.MaterialId,

                    MaterialName =
                        material.MaterialName,

                    BaseUnit =
                        material.BaseUnit,

                    UsagePerAreaUnit =
                        $"{material.BaseUnit}/m²",

                    TopK =
                        settings.TopK,

                    MinimumSampleCount =
                        settings.MinimumSampleCount,

                    LowerQuantile =
                        settings.LowerQuantile,

                    MedianQuantile =
                        settings.MedianQuantile,

                    UpperQuantile =
                        settings.UpperQuantile,

                    SimilarityExponent =
                        settings.SimilarityExponent,

                    TimeHalfLifeYears =
                        settings.TimeHalfLifeYears
                };


            // ==========================================
            // 3. 取得所有相似候選建案
            //
            // 注意：
            // 這裡還不能 Take Top K。
            //
            // 要先找「有這個材料」的建案，
            // 再做材料自己的 Top K。
            // ==========================================

            var similarityCandidates =
                precomputedSimilarityCandidates
                ??
                await _similarityService
                    .GetScoredCandidateProjectsAsync(
                        resolvedTarget
                    );

            result.CandidateProjectCount =
                similarityCandidates.Count;

            if (!similarityCandidates.Any())
            {
                result.HasEnoughSamples = false;

                result.StatusMessage =
                    "沒有可用的相似建案";

                return ServiceResult.Ok(result);
            }

            var candidateIds =
                similarityCandidates
                    .Select(x => x.ProjectId)
                    .ToList();


            // ==========================================
            // 4. 同一建案 + 同一材料
            //
            // HistoricalTransaction 已經是
            // UnitConversionService 標準化後的數量。
            //
            // 同一案可能有：
            //
            // 柱 SD420
            // 梁 SD420
            // 樓板 SD420
            //
            // 目前第一版先 SUM 成整案 SD420 總量。
            // ==========================================

            var usageData =
                await _context.HistoricalTransactions
                    .AsNoTracking()
                    .Where(x =>
                        x.ProjectMasterId.HasValue &&
                        candidateIds.Contains(
                            x.ProjectMasterId.Value
                        ) &&
                        x.MaterialId == materialId &&
                        !x.IsDeleted &&
                        !x.IsOutlier
                    )
                    .GroupBy(x =>
                        x.ProjectMasterId!.Value
                    )
                    .Select(g => new
                    {
                        ProjectId =
                            g.Key,

                        ActualQuantity =
                            g.Sum(x =>
                                x.ActualQuantity
                            )
                    })
                    .ToListAsync();

            if (!usageData.Any())
            {
                result.HasEnoughSamples = false;

                result.StatusMessage =
                    "相似建案中沒有此材料的有效歷史資料";

                return ServiceResult.Ok(result);
            }


            // ==========================================
            // 5. 取得這些建案的面積與年月
            // ==========================================

            var usageProjectIds =
                usageData
                    .Select(x => x.ProjectId)
                    .Distinct()
                    .ToList();

            var projectMetadata =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .Where(x =>
                        usageProjectIds.Contains(
                            x.ProjectId
                        ) &&
                        !x.IsDeleted
                    )
                    .Select(x => new
                    {
                        x.ProjectId,
                        x.ProjectName,
                        x.TotalFloorArea,
                        x.StartDate
                    })
                    .ToListAsync();

            var projectMetadataDictionary =
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


            // ==========================================
            // 6. 建立每個歷史建案的一筆 Reference
            // ==========================================

            var allReferences =
                new List<RangeEstimationReferenceDto>();

            foreach (var usage in usageData)
            {
                if (!projectMetadataDictionary.TryGetValue(
                        usage.ProjectId,
                        out var project))
                {
                    continue;
                }

                if (!similarityDictionary.TryGetValue(
                        usage.ProjectId,
                        out var similarity))
                {
                    continue;
                }

                if (!project.TotalFloorArea.HasValue ||
                    project.TotalFloorArea.Value <= 0)
                {
                    continue;
                }

                if (!project.StartDate.HasValue)
                {
                    continue;
                }

                if (usage.ActualQuantity <= 0)
                {
                    continue;
                }

                if (similarity.SimilarityScore <= 0)
                {
                    continue;
                }


                // ======================================
                // 單位面積用量
                // ======================================

                decimal usagePerArea =
                    usage.ActualQuantity /
                    project.TotalFloorArea.Value;


                // ======================================
                // Similarity Weight
                //
                // (Similarity / 100) ^ exponent
                // ======================================

                decimal similarityWeight =
                    (decimal)Math.Pow(
                        similarity.SimilarityScore /
                        100.0,
                        (double)
                        settings.SimilarityExponent
                    );


                // ======================================
                // Time Weight
                //
                // 使用「目標建案年月」
                // 與「歷史建案年月」的時間距離。
                //
                // 每經過一個 Half-Life：
                // 權重減半。
                // ======================================

                double ageYears =
                    Math.Abs(
                        (
                            resolvedTarget
                                .SimilarityTarget
                                .ProjectDate -
                            project
                                .StartDate
                                .Value
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


                // ======================================
                // 最終權重
                // ======================================

                decimal finalWeight =
                    similarityWeight *
                    timeWeight;

                if (finalWeight <= 0)
                {
                    continue;
                }


                allReferences.Add(
                    new RangeEstimationReferenceDto
                    {
                        ProjectId =
                            project.ProjectId,

                        ProjectName =
                            project.ProjectName,

                        ProjectDate =
                            project.StartDate.Value,

                        SimilarityScore =
                            similarity.SimilarityScore,

                        FloorArea =
                            project.TotalFloorArea.Value,

                        ActualQuantity =
                            usage.ActualQuantity,

                        UsagePerArea =
                            usagePerArea,

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


            result.MaterialAvailableProjectCount =
                allReferences.Count;


            // ==========================================
            // 7. 這時候才取材料自己的 Top K
            // ==========================================

            var references =
                allReferences
                    .OrderByDescending(x =>
                        x.SimilarityScore
                    )
                    .ThenByDescending(x =>
                        x.FinalWeight
                    )
                    .Take(
                        settings.TopK
                    )
                    .ToList();

            result.ReferenceProjectCount =
                references.Count;

            result.References =
                references;


            // ==========================================
            // 8. 最低樣本數判定
            // ==========================================

            if (references.Count <
                settings.MinimumSampleCount)
            {
                result.HasEnoughSamples =
                    false;

                result.StatusMessage =
                    $"樣本不足：目前只有 {references.Count} 個有效參考建案，最低需要 {settings.MinimumSampleCount} 個。";

                RoundReferences(
                    result.References
                );

                return ServiceResult.Ok(
                    result
                );
            }


            // ==========================================
            // 9. Weighted Quantile
            // ==========================================

            decimal lowerUsagePerArea =
                CalculateWeightedQuantile(
                    references,
                    settings.LowerQuantile
                );

            decimal medianUsagePerArea =
                CalculateWeightedQuantile(
                    references,
                    settings.MedianQuantile
                );

            decimal upperUsagePerArea =
                CalculateWeightedQuantile(
                    references,
                    settings.UpperQuantile
                );


            // ==========================================
            // 10. 乘回目標建案總樓地板面積
            // ==========================================

            decimal targetArea =
                resolvedTarget
                    .SimilarityTarget
                    .TotalFloorArea;

            result.LowerUsagePerArea =
                Math.Round(
                    lowerUsagePerArea,
                    6
                );

            result.MedianUsagePerArea =
                Math.Round(
                    medianUsagePerArea,
                    6
                );

            result.UpperUsagePerArea =
                Math.Round(
                    upperUsagePerArea,
                    6
                );


            result.LowerQuantity =
                Math.Round(
                    lowerUsagePerArea *
                    targetArea,
                    2
                );

            result.MedianQuantity =
                Math.Round(
                    medianUsagePerArea *
                    targetArea,
                    2
                );

            result.UpperQuantity =
                Math.Round(
                    upperUsagePerArea *
                    targetArea,
                    2
                );


            result.HasEnoughSamples =
                true;

            result.StatusMessage =
                "合理用量區間估算完成";


            RoundReferences(
                result.References
            );

            return ServiceResult.Ok(
                result
            );
        }



        // ==========================================
        // Weighted Quantile
        //
        // 不是平均 ± 標準差。
        //
        // 將建案依 UnitConsumption 排序，
        // 再利用 FinalWeight 找累積權重位置。
        // ==========================================
        private static decimal CalculateWeightedQuantile(
            List<RangeEstimationReferenceDto> references,
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
            List<RangeEstimationReferenceDto> references)
        {
            foreach (var reference in references)
            {
                reference.SimilarityScore =
                    Math.Round(
                        reference.SimilarityScore,
                        2
                    );

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