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

        public async Task<ServiceResult>
            EstimateMaterialRangesAsync(
                ResolvedEstimationTarget resolvedTarget,
                IEnumerable<int> materialIds,
                List<ProjectSimilarityDto> similarityCandidates)
        {
            if (resolvedTarget == null)
            {
                return ServiceResult.Validation(
                    "估算目標不可為空"
                );
            }

            if (similarityCandidates == null)
            {
                return ServiceResult.Validation(
                    "相似建案資料不可為空"
                );
            }

            var requestedMaterialIds =
                materialIds
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

            if (!requestedMaterialIds.Any())
            {
                return ServiceResult.Validation(
                    "請至少選擇一個標準材料"
                );
            }

            var settings =
                await _settingsService
                    .GetCurrentAsync();

            var materials =
                await _context.MaterialMasters
                    .AsNoTracking()
                    .Where(x =>
                        requestedMaterialIds.Contains(
                            x.MaterialId
                        )
                    )
                    .ToListAsync();

            var materialDictionary =
                materials.ToDictionary(
                    x => x.MaterialId,
                    x => x
                );

            var missingMaterialId =
                requestedMaterialIds
                    .FirstOrDefault(x =>
                        !materialDictionary.ContainsKey(x)
                    );

            if (missingMaterialId > 0)
            {
                return ServiceResult.NotFound(
                    $"找不到指定的標準材料：{missingMaterialId}"
                );
            }

            if (!similarityCandidates.Any())
            {
                var noCandidateResults =
                    requestedMaterialIds
                        .Select(materialId =>
                        {
                            var result =
                                CreateBaseResult(
                                    resolvedTarget,
                                    materialDictionary[materialId],
                                    settings
                                );

                            result.CandidateProjectCount = 0;
                            result.HasEnoughSamples = false;
                            result.StatusMessage =
                                "沒有可用的相似建案";

                            return result;
                        })
                        .ToList();

                return ServiceResult.Ok(
                    noCandidateResults
                );
            }

            var candidateIds =
                similarityCandidates
                    .Select(x => x.ProjectId)
                    .Distinct()
                    .ToList();

            var usageData =
                await _context.HistoricalTransactions
                    .AsNoTracking()
                    .Where(x =>
                        x.ProjectMasterId.HasValue &&
                        candidateIds.Contains(
                            x.ProjectMasterId.Value
                        ) &&
                        requestedMaterialIds.Contains(
                            x.MaterialId
                        ) &&
                        !x.IsDeleted &&
                        !x.IsOutlier
                    )
                    .GroupBy(x => new
                    {
                        x.MaterialId,
                        ProjectId =
                            x.ProjectMasterId!.Value
                    })
                    .Select(g => new
                    {
                        g.Key.MaterialId,
                        g.Key.ProjectId,

                        ActualQuantity =
                            g.Sum(x =>
                                x.ActualQuantity
                            )
                    })
                    .ToListAsync();

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

            var usageLookup =
                usageData.ToLookup(
                    x => x.MaterialId
                );

            var results =
                new List<RangeEstimationDto>();

            foreach (
                var materialId
                in requestedMaterialIds)
            {
                var material =
                    materialDictionary[materialId];

                var result =
                    CreateBaseResult(
                        resolvedTarget,
                        material,
                        settings
                    );

                result.CandidateProjectCount =
                    similarityCandidates.Count;

                var materialUsage =
                    usageLookup[materialId]
                        .ToList();

                if (!materialUsage.Any())
                {
                    result.HasEnoughSamples =
                        false;

                    result.StatusMessage =
                        "相似建案中沒有此材料的有效歷史資料";

                    results.Add(
                        result
                    );

                    continue;
                }

                var allReferences =
                    new List<RangeEstimationReferenceDto>();

                foreach (
                    var usage
                    in materialUsage)
                {
                    if (
                        !projectMetadataDictionary
                            .TryGetValue(
                                usage.ProjectId,
                                out var project
                            )
                    )
                    {
                        continue;
                    }

                    if (
                        !similarityDictionary
                            .TryGetValue(
                                usage.ProjectId,
                                out var similarity
                            )
                    )
                    {
                        continue;
                    }

                    if (
                        !project.TotalFloorArea.HasValue ||
                        project.TotalFloorArea.Value <= 0
                    )
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

                    decimal usagePerArea =
                        usage.ActualQuantity /
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

                if (
                    references.Count <
                    settings.MinimumSampleCount
                )
                {
                    result.HasEnoughSamples =
                        false;

                    result.StatusMessage =
                        $"樣本不足：目前只有 {references.Count} 個有效參考建案，最低需要 {settings.MinimumSampleCount} 個。";

                    RoundReferences(
                        result.References
                    );

                    results.Add(
                        result
                    );

                    continue;
                }

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

                results.Add(
                    result
                );
            }

            return ServiceResult.Ok(
                results
            );
        }

        private async Task<ServiceResult>
            EstimateResolvedMaterialRangeAsync(
                ResolvedEstimationTarget resolvedTarget,
                int materialId,
                List<ProjectSimilarityDto>? precomputedSimilarityCandidates = null)
        {
            var similarityCandidates =
                precomputedSimilarityCandidates
                ??
                await _similarityService
                    .GetScoredCandidateProjectsAsync(
                        resolvedTarget
                    );

            var batchResult =
                await EstimateMaterialRangesAsync(
                    resolvedTarget,
                    new[]
                    {
                        materialId
                    },
                    similarityCandidates
                );

            if (!batchResult.Success)
            {
                return batchResult;
            }

            if (
                batchResult.Data
                is not List<RangeEstimationDto> results ||
                results.Count != 1
            )
            {
                return ServiceResult.Validation(
                    "單一材料估算結果格式錯誤"
                );
            }

            return ServiceResult.Ok(
                results[0]
            );
        }

        private static RangeEstimationDto
            CreateBaseResult(
                ResolvedEstimationTarget resolvedTarget,
                MaterialMaster material,
                EstimationSetting settings)
        {
            return new RangeEstimationDto
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
        }

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