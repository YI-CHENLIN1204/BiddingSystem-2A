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
        private readonly UnitConversionService
            _unitConversionService;


        public TwoAQuantityAnalysisService(
            ApplicationDbContext context,
            DetectionCategoryRangeService
                rangeService,
            UnitConversionService
                unitConversionService)
        {
            _context = context;
            _rangeService = rangeService;
            _unitConversionService =
                unitConversionService;
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
                            group.DetectionCategory
                    };

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

                item.UnitName =
                    category.UnitMaster.UnitName;

                var sourceItems =
                    preprocess.Items
                        .Where(x =>
                            x.Status ==
                                "Included"
                            &&
                            string.Equals(
                                x.MajorCategory,
                                group.MajorCategory,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                            &&
                            string.Equals(
                                x.DetectionCategory,
                                group.DetectionCategory,
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                        )
                        .ToList();

                if (!sourceItems.Any())
                {
                    item.AnalysisStatus =
                        "ObservedQuantityUnavailable";

                    item.TrafficLight =
                        "Gray";

                    item.Message =
                        "找不到此偵錯細項可供分析的 1D 工項。";

                    result.SkippedGroupCount++;
                    result.Items.Add(item);
                    continue;
                }

                decimal observedQuantity = 0m;
                bool conversionFailed = false;
                string conversionMessage =
                    string.Empty;

                foreach (var sourceItem
                    in sourceItems)
                {
                    if (!sourceItem.Quantity
                            .HasValue ||
                        string.IsNullOrWhiteSpace(
                            sourceItem.Unit))
                    {
                        conversionFailed = true;
                        conversionMessage =
                            $"工項「{sourceItem.Name}」缺少數量或單位。";
                        break;
                    }

                    var conversion =
                        _unitConversionService
                            .Convert(
                                sourceItem
                                    .Quantity.Value,
                                0m,
                                sourceItem.Unit,
                                category.UnitId
                            );

                    if (!conversion.Success)
                    {
                        conversionFailed = true;
                        conversionMessage =
                            $"工項「{sourceItem.Name}」無法由「{sourceItem.Unit}」換算為「{category.UnitMaster.UnitName}」。";
                        break;
                    }

                    observedQuantity +=
                        conversion.Quantity;
                }

                if (conversionFailed)
                {
                    item.AnalysisStatus =
                        "ObservedQuantityUnavailable";

                    item.TrafficLight =
                        "Gray";

                    item.Message =
                        conversionMessage;

                    result.SkippedGroupCount++;
                    result.Items.Add(item);
                    continue;
                }

                item.ObservedQuantity =
                    observedQuantity;

                item.ObservedUsagePerArea =
                    Math.Round(
                        observedQuantity /
                        request.Target.TotalFloorArea,
                        6
                    );

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

                if (observedQuantity <
                    range.LowerQuantity.Value)
                {
                    item.AnalysisStatus =
                        "Low";

                    item.TrafficLight =
                        "Red";

                    item.Message =
                        "實際用量低於歷史相似建案合理範圍。";
                }
                else if (observedQuantity >
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
