using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class TwoAQuantityAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly DetectionCategoryRangeService
            _rangeService;


        public TwoAQuantityAnalysisService(
            ApplicationDbContext context,
            DetectionCategoryRangeService
                rangeService)
        {
            _context = context;
            _rangeService = rangeService;
        }


        public async Task<ServiceResult> AnalyzeAsync(
            int oneDProjectId,
            TwoAProjectPreprocessResponse preprocess,
            TwoAQuantityAnalysisRequest request)
        {
            if (request == null ||
                request.Target == null)
            {
                return ServiceResult.Validation(
                    "請提供建案條件");
            }

            if (request.Target.TotalFloorArea <= 0)
            {
                return ServiceResult.Validation(
                    "總樓地板面積必須大於 0");
            }

            var configuredCategories =
                await _context.DetectionCategories
                    .AsNoTracking()
                    .Include(x => x.UnitMaster)
                    .Where(x => x.IsActive)
                    .ToListAsync();

            var result =
                new TwoAQuantityAnalysisResultDto
                {
                    OneDProjectId =
                        oneDProjectId,

                    TotalGroupCount =
                        preprocess.Groups.Count
                };

            foreach (var group
                in preprocess.Groups)
            {
                var item =
                    new TwoAQuantityAnalysisItemDto
                    {
                        MajorCategoryName =
                            group.MajorCategory,

                        DetectionCategoryName =
                            group.DetectionCategory,

                        ObservedQuantity =
                            group.TotalQuantity,

                        UnitName =
                            group.StandardUnit
                    };

                if (group.AggregationStatus !=
                        "Ready" ||
                    !group.TotalQuantity.HasValue ||
                    string.IsNullOrWhiteSpace(
                        group.StandardUnit))
                {
                    item.AnalysisStatus =
                        "ObservedQuantityUnavailable";

                    item.TrafficLight =
                        "Gray";

                    item.Message =
                        $"1D 工項尚未完成可比較的單位標準化：{group.AggregationStatus}。";

                    result.SkippedGroupCount++;
                    result.Items.Add(item);
                    continue;
                }

                var category =
                    configuredCategories
                        .FirstOrDefault(x =>
                            string.Equals(
                                x.MajorCategoryName,
                                group.MajorCategory,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                            &&
                            string.Equals(
                                x.DetectionCategoryName,
                                group.DetectionCategory,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                        );

                if (category == null)
                {
                    item.AnalysisStatus =
                        "DetectionCategoryNotConfigured";

                    item.TrafficLight =
                        "Gray";

                    item.Message =
                        "此偵錯細項尚未建立正式主檔與標準材料對應。";

                    result.SkippedGroupCount++;
                    result.Items.Add(item);
                    continue;
                }

                item.DetectionCategoryId =
                    category.DetectionCategoryId;

                if (!string.Equals(
                        group.StandardUnit,
                        category.UnitMaster.UnitName,
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    item.AnalysisStatus =
                        "UnitMismatch";

                    item.TrafficLight =
                        "Gray";

                    item.Message =
                        $"目前建案已標準化為「{group.StandardUnit}」，但偵錯細項標準單位為「{category.UnitMaster.UnitName}」，暫不比較。";

                    result.SkippedGroupCount++;
                    result.Items.Add(item);
                    continue;
                }

                var rangeResult =
                    await _rangeService
                        .EstimateAsync(
                            category
                                .DetectionCategoryId,
                            new
                            DetectionCategoryRangeRequest
                            {
                                Target =
                                    request.Target,

                                TopCount =
                                    request.TopCount
                            }
                        );

                if (!rangeResult.Success)
                {
                    return rangeResult;
                }

                var range =
                    rangeResult.Data
                    as DetectionCategoryRangeResultDto;

                if (range == null)
                {
                    return ServiceResult.Validation(
                        "偵錯細項合理範圍結果格式錯誤"
                    );
                }

                item.Range =
                    range;

                item.ObservedUsagePerArea =
                    Math.Round(
                        group.TotalQuantity.Value /
                        request.Target.TotalFloorArea,
                        6
                    );

                if (!range.HasEnoughSamples ||
                    !range.LowerQuantity.HasValue ||
                    !range.UpperQuantity.HasValue)
                {
                    item.AnalysisStatus =
                        "InsufficientData";

                    item.TrafficLight =
                        "Gray";

                    item.Message =
                        range.StatusMessage;

                    result.InsufficientDataCount++;
                    result.Items.Add(item);
                    continue;
                }

                if (group.TotalQuantity.Value <
                    range.LowerQuantity.Value)
                {
                    item.AnalysisStatus =
                        "Low";

                    item.TrafficLight =
                        "Red";

                    item.Message =
                        "實際用量低於歷史相似建案合理範圍。";
                }
                else if (group.TotalQuantity.Value >
                    range.UpperQuantity.Value)
                {
                    item.AnalysisStatus =
                        "High";

                    item.TrafficLight =
                        "Red";

                    item.Message =
                        "實際用量高於歷史相似建案合理範圍。";
                }
                else
                {
                    item.AnalysisStatus =
                        "Normal";

                    item.TrafficLight =
                        "Green";

                    item.Message =
                        "實際用量位於歷史相似建案合理範圍內。";
                }

                result.AnalyzedGroupCount++;
                result.Items.Add(item);
            }

            return ServiceResult.Ok(
                result
            );
        }
    }
}
