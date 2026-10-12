using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class MajorCategoryPriceRangeService
    {
        private const decimal SquareMetersPerPing =
            3.305785m;

        private readonly ApplicationDbContext _context;
        private readonly ProjectSimilarityService
            _similarityService;
        private readonly EstimationSettingsService
            _settingsService;


        public MajorCategoryPriceRangeService(
            ApplicationDbContext context,
            ProjectSimilarityService similarityService,
            EstimationSettingsService settingsService)
        {
            _context = context;
            _similarityService =
                similarityService;
            _settingsService =
                settingsService;
        }


        public async Task<ServiceResult>
            GetReadinessAsync()
        {
            var settings =
                await _settingsService
                    .GetCurrentAsync();

            var projects =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .Where(x =>
                        !x.IsDeleted &&
                        !string.IsNullOrWhiteSpace(
                            x.ProjectType) &&
                        !string.IsNullOrWhiteSpace(
                            x.StructureType) &&
                        !string.IsNullOrWhiteSpace(
                            x.City) &&
                        x.TotalFloorArea.HasValue &&
                        x.TotalFloorArea.Value > 0 &&
                        x.SiteArea.HasValue &&
                        x.SiteArea.Value > 0 &&
                        x.FloorCount.HasValue &&
                        x.FloorCount.Value > 0 &&
                        x.BasementFloorCount.HasValue &&
                        x.BasementFloorCount.Value >= 0 &&
                        x.StartDate.HasValue)
                    .Select(x => new
                    {
                        x.ProjectId,
                        x.ProjectName
                    })
                    .ToListAsync();

            var projectIds =
                projects
                    .Select(x => x.ProjectId)
                    .ToList();

            var rows =
                await _context.FinalBidItems
                    .AsNoTracking()
                    .Where(x =>
                        projectIds.Contains(
                            x.ProjectMasterId) &&
                        !x.IsIgnored &&
                        (
                            x.Category1 ==
                                "結構體工程" ||
                            x.Category2 ==
                                "結構體工程" ||
                            x.Category3 ==
                                "結構體工程"
                        ))
                    .Select(x => new
                    {
                        x.ProjectMasterId,
                        x.Amount,
                        x.Qty,
                        x.UnitPrice
                    })
                    .ToListAsync();

            var amountByProject =
                rows
                    .GroupBy(x =>
                        x.ProjectMasterId)
                    .Select(group => new
                    {
                        ProjectId =
                            group.Key,

                        Amount =
                            group.Sum(x =>
                                ResolveLineAmount(
                                    x.Amount,
                                    x.Qty,
                                    x.UnitPrice
                                ))
                    })
                    .Where(x =>
                        x.Amount > 0)
                    .ToList();

            int availableProjectCount =
                amountByProject.Count;

            int minimumSampleCount =
                settings.MinimumSampleCount;

            int missingProjectCount =
                Math.Max(
                    0,
                    minimumSampleCount -
                    availableProjectCount
                );

            bool ready =
                availableProjectCount >=
                minimumSampleCount;

            return ServiceResult.Ok(
                new
                {
                    majorCategory =
                        "結構體工程",

                    eligibleMetadataProjectCount =
                        projects.Count,

                    availableProjectCount,

                    minimumSampleCount,

                    missingProjectCount,

                    status =
                        ready
                            ? "ReadyForEstimation"
                            : availableProjectCount == 0
                                ? "HistoricalDataRequired"
                                : "InsufficientHistoricalProjects",

                    message =
                        ready
                            ? $"已有 {availableProjectCount} 個具完整條件與結構體價格的歷史建案，可進行大項估算。"
                            : availableProjectCount == 0
                                ? $"尚無可用的結構體工程價格歷史建案，至少需要 {minimumSampleCount} 案。"
                                : $"目前只有 {availableProjectCount} 個結構體工程價格歷史建案，至少需要 {minimumSampleCount} 案，尚差 {missingProjectCount} 案。"
                });
        }


        public async Task<ServiceResult> EstimateAsync(
            MajorCategoryPriceRangeRequest request)
        {
            if (request == null ||
                request.Target == null)
            {
                return ServiceResult.Validation(
                    "請提供建案條件");
            }

            string majorCategory =
                request.MajorCategory?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    majorCategory))
            {
                return ServiceResult.Validation(
                    "請提供工程大項名稱");
            }

            if (!TwoAScopePolicy
                .IsPriceEstimationMajorCategory(
                    majorCategory))
            {
                string supported =
                    string.Join(
                        "、",
                        TwoAScopePolicy
                            .GetPriceEstimationMajorCategories()
                    );

                return ServiceResult.Validation(
                    $"目前大項估算暫定只支援：{supported}。"
                );
            }

            if (request.Target.TotalFloorArea <= 0)
            {
                return ServiceResult.Validation(
                    "總樓地板面積必須大於 0");
            }

            if (request.ObservedAmount.HasValue &&
                request.ObservedAmount.Value < 0)
            {
                return ServiceResult.Validation(
                    "工程大項金額不可小於 0");
            }

            if (request.TopCount.HasValue &&
                request.TopCount.Value <= 0)
            {
                return ServiceResult.Validation(
                    "TopCount 必須大於 0");
            }

            var settings =
                await _settingsService
                    .GetCurrentAsync();

            int takeCount =
                request.TopCount ??
                settings.TopK;

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

            var result =
                new MajorCategoryPriceRangeResultDto
                {
                    MajorCategory =
                        majorCategory,

                    CandidateProjectCount =
                        similarityCandidates.Count,

                    ObservedAmount =
                        request.ObservedAmount
                };

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
                await _context.FinalBidItems
                    .AsNoTracking()
                    .Where(x =>
                        candidateIds.Contains(
                            x.ProjectMasterId
                        ) &&
                        !x.IsIgnored &&
                        !x.ProjectMaster.IsDeleted &&
                        (
                            x.Category1 ==
                                majorCategory ||
                            x.Category2 ==
                                majorCategory ||
                            x.Category3 ==
                                majorCategory
                        )
                    )
                    .Select(x => new
                    {
                        x.ProjectMasterId,
                        x.Amount,
                        x.Qty,
                        x.UnitPrice
                    })
                    .ToListAsync();

            var amountByProject =
                rows
                    .GroupBy(x =>
                        x.ProjectMasterId)
                    .Select(group => new
                    {
                        ProjectId =
                            group.Key,

                        TotalAmount =
                            group.Sum(x =>
                                ResolveLineAmount(
                                    x.Amount,
                                    x.Qty,
                                    x.UnitPrice
                                ))
                    })
                    .Where(x =>
                        x.TotalAmount > 0)
                    .ToDictionary(
                        x => x.ProjectId,
                        x => x.TotalAmount
                    );

            result.AvailableProjectCount =
                amountByProject.Count;

            if (!amountByProject.Any())
            {
                result.StatusMessage =
                    "相似建案中沒有可用的工程大項金額資料";

                return ServiceResult.Ok(
                    result
                );
            }

            var amountProjectIds =
                amountByProject.Keys
                    .ToList();

            var projects =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .Where(x =>
                        amountProjectIds.Contains(
                            x.ProjectId
                        ) &&
                        !x.IsDeleted &&
                        x.TotalFloorArea.HasValue &&
                        x.TotalFloorArea.Value > 0 &&
                        x.StartDate.HasValue
                    )
                    .Select(x => new
                    {
                        x.ProjectId,
                        x.ProjectName,
                        x.TotalFloorArea,
                        x.StartDate
                    })
                    .ToListAsync();

            var projectDictionary =
                projects.ToDictionary(
                    x => x.ProjectId,
                    x => x
                );

            var similarityDictionary =
                similarityCandidates
                    .ToDictionary(
                        x => x.ProjectId,
                        x => x
                    );

            var references =
                new List<
                    MajorCategoryPriceReferenceDto>();

            foreach (var entry
                in amountByProject)
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

                if (similarity.SimilarityScore <= 0)
                {
                    continue;
                }

                decimal floorAreaM2 =
                    project.TotalFloorArea!.Value;

                decimal floorAreaPing =
                    floorAreaM2 /
                    SquareMetersPerPing;

                if (floorAreaPing <= 0)
                {
                    continue;
                }

                decimal pricePerPing =
                    entry.Value /
                    floorAreaPing;

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
                            project.StartDate!.Value
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

                references.Add(
                    new MajorCategoryPriceReferenceDto
                    {
                        ProjectId =
                            project.ProjectId,

                        ProjectName =
                            project.ProjectName,

                        ProjectDate =
                            project.StartDate.Value,

                        FloorAreaM2 =
                            floorAreaM2,

                        FloorAreaPing =
                            floorAreaPing,

                        TotalAmount =
                            entry.Value,

                        PricePerPing =
                            pricePerPing,

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

            references =
                references
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

            if (request.ObservedAmount.HasValue)
            {
                decimal targetFloorAreaPing =
                    request.Target.TotalFloorArea /
                    SquareMetersPerPing;

                result.ObservedPricePerPing =
                    targetFloorAreaPing > 0
                        ? Math.Round(
                            request.ObservedAmount.Value /
                            targetFloorAreaPing,
                            2
                        )
                        : null;
            }

            if (references.Count <
                settings.MinimumSampleCount)
            {
                result.AnalysisStatus =
                    "InsufficientData";

                result.TrafficLight =
                    "Gray";

                result.StatusMessage =
                    $"樣本不足：目前只有 {references.Count} 個有效參考建案，最低需要 {settings.MinimumSampleCount} 個。";

                RoundReferences(
                    result.References
                );

                return ServiceResult.Ok(
                    result
                );
            }

            decimal lowerPricePerPing =
                CalculateWeightedQuantile(
                    references,
                    settings.LowerQuantile
                );

            decimal medianPricePerPing =
                CalculateWeightedQuantile(
                    references,
                    settings.MedianQuantile
                );

            decimal upperPricePerPing =
                CalculateWeightedQuantile(
                    references,
                    settings.UpperQuantile
                );

            decimal targetPing =
                request.Target.TotalFloorArea /
                SquareMetersPerPing;

            result.LowerPricePerPing =
                Math.Round(
                    lowerPricePerPing,
                    2
                );

            result.MedianPricePerPing =
                Math.Round(
                    medianPricePerPing,
                    2
                );

            result.UpperPricePerPing =
                Math.Round(
                    upperPricePerPing,
                    2
                );

            result.LowerAmount =
                Math.Round(
                    lowerPricePerPing *
                    targetPing,
                    2
                );

            result.MedianAmount =
                Math.Round(
                    medianPricePerPing *
                    targetPing,
                    2
                );

            result.UpperAmount =
                Math.Round(
                    upperPricePerPing *
                    targetPing,
                    2
                );

            result.HasEnoughSamples =
                true;

            if (!request.ObservedAmount.HasValue)
            {
                result.AnalysisStatus =
                    "RangeOnly";

                result.TrafficLight =
                    "Gray";

                result.StatusMessage =
                    "工程大項每坪單價合理區間估算完成";

                RoundReferences(
                    result.References
                );

                return ServiceResult.Ok(
                    result
                );
            }

            if (request.ObservedAmount.Value <
                result.LowerAmount.Value)
            {
                result.AnalysisStatus =
                    "Low";

                result.TrafficLight =
                    "Red";

                result.StatusMessage =
                    "工程大項金額低於歷史相似建案合理範圍。";
            }
            else if (request.ObservedAmount.Value >
                result.UpperAmount.Value)
            {
                result.AnalysisStatus =
                    "High";

                result.TrafficLight =
                    "Red";

                result.StatusMessage =
                    "工程大項金額高於歷史相似建案合理範圍。";
            }
            else
            {
                result.AnalysisStatus =
                    "Normal";

                result.TrafficLight =
                    "Green";

                result.StatusMessage =
                    "工程大項金額位於歷史相似建案合理範圍內。";
            }

            RoundReferences(
                result.References
            );

            return ServiceResult.Ok(
                result
            );
        }


        private static decimal ResolveLineAmount(
            decimal? amount,
            decimal? quantity,
            decimal? unitPrice)
        {
            if (amount.HasValue &&
                amount.Value > 0)
            {
                return amount.Value;
            }

            if (quantity.HasValue &&
                quantity.Value > 0 &&
                unitPrice.HasValue &&
                unitPrice.Value > 0)
            {
                return
                    quantity.Value *
                    unitPrice.Value;
            }

            return 0m;
        }


        private static decimal
            CalculateWeightedQuantile(
                List<
                    MajorCategoryPriceReferenceDto>
                        references,
                decimal quantile)
        {
            var ordered =
                references
                    .Where(x =>
                        x.FinalWeight > 0)
                    .OrderBy(x =>
                        x.PricePerPing)
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
                    return item.PricePerPing;
                }
            }

            return ordered
                .Last()
                .PricePerPing;
        }


        private static void RoundReferences(
            List<
                MajorCategoryPriceReferenceDto>
                    references)
        {
            foreach (var reference
                in references)
            {
                reference.FloorAreaM2 =
                    Math.Round(
                        reference.FloorAreaM2,
                        2
                    );

                reference.FloorAreaPing =
                    Math.Round(
                        reference.FloorAreaPing,
                        2
                    );

                reference.TotalAmount =
                    Math.Round(
                        reference.TotalAmount,
                        2
                    );

                reference.PricePerPing =
                    Math.Round(
                        reference.PricePerPing,
                        2
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
