using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class EstimationSettingsService 
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public EstimationSettingsService(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        public async Task<EstimationSetting>
            GetCurrentAsync()
        {
            var settings =
                await _context.EstimationSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SettingsId == 1);

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "找不到估算系統參數設定，請確認 estimation_settings 已建立並包含 settings_id = 1。"
                );
            }

            Validate(settings);

            return settings;
        }

        public async Task<ServiceResult>
            UpdateAsync(
                EstimationSettingsUpdateRequest request,
                string? updatedBy)
        {
            var settings =
                await _context.EstimationSettings
                    .FirstOrDefaultAsync(x =>
                        x.SettingsId == 1
                    );

            if (settings == null)
            {
                return ServiceResult.NotFound(
                    "找不到估算系統參數設定"
                );
            }


            var oldValue = new
            {
                settings.TopK,
                settings.MinimumSampleCount,
                settings.LowerQuantile,
                settings.MedianQuantile,
                settings.UpperQuantile,
                settings.SimilarityExponent,
                settings.TimeHalfLifeYears,
                settings.MinimumSimilarityScore,
                settings.StructureWeight,
                settings.AreaWeight,
                settings.SiteAreaWeight,
                settings.FloorWeight,
                settings.YearWeight,
                settings.CityWeight,
                settings.AreaDifferenceCutoffRatio,
                settings.SiteAreaDifferenceCutoffRatio,
                settings.GroundFloorPenaltyRatio,
                settings.BasementFloorPenaltyRatio,
                settings.YearDifferenceCutoffYears,
                settings.UpdatedAt,
                settings.UpdatedBy
            };


            settings.TopK =
                request.TopK;

            settings.MinimumSampleCount =
                request.MinimumSampleCount;

            settings.LowerQuantile =
                request.LowerQuantile;

            settings.MedianQuantile =
                request.MedianQuantile;

            settings.UpperQuantile =
                request.UpperQuantile;

            settings.SimilarityExponent =
                request.SimilarityExponent;

            settings.TimeHalfLifeYears =
                request.TimeHalfLifeYears;

            settings.MinimumSimilarityScore =
                request.MinimumSimilarityScore;

            settings.StructureWeight =
                request.StructureWeight;

            settings.AreaWeight =
                request.AreaWeight;

            settings.SiteAreaWeight =
                request.SiteAreaWeight;

            settings.FloorWeight =
                request.FloorWeight;

            settings.YearWeight =
                request.YearWeight;

            settings.CityWeight =
                request.CityWeight;

            settings.AreaDifferenceCutoffRatio =
                request.AreaDifferenceCutoffRatio;

            settings.SiteAreaDifferenceCutoffRatio =
                request.SiteAreaDifferenceCutoffRatio;

            settings.GroundFloorPenaltyRatio =
                request.GroundFloorPenaltyRatio;

            settings.BasementFloorPenaltyRatio =
                request.BasementFloorPenaltyRatio;

            settings.YearDifferenceCutoffYears =
                request.YearDifferenceCutoffYears;

            settings.UpdatedAt =
                DateTime.UtcNow;

            settings.UpdatedBy =
                string.IsNullOrWhiteSpace(updatedBy)
                    ? "SYSTEM"
                    : updatedBy.Trim();


            try
            {
                Validate(settings);
            }
            catch (InvalidOperationException ex)
            {
                return ServiceResult.Validation(
                    ex.Message
                );
            }


            _auditLogService.AddLog(
                module:
                    "估算參數",

                actionType:
                    "UpdateEstimationSettings",

                entityType:
                    "EstimationSetting",

                entityId:
                    settings.SettingsId,

                description:
                    "更新估算與異常分析模型參數",

                actionUser:
                    settings.UpdatedBy
                        ?? "SYSTEM",

                oldValue:
                    oldValue,

                newValue:
                    new
                    {
                        settings.TopK,
                        settings.MinimumSampleCount,
                        settings.LowerQuantile,
                        settings.MedianQuantile,
                        settings.UpperQuantile,
                        settings.SimilarityExponent,
                        settings.TimeHalfLifeYears,
                        settings.MinimumSimilarityScore,
                        settings.StructureWeight,
                        settings.AreaWeight,
                        settings.FloorWeight,
                        settings.YearWeight,
                        settings.CityWeight,
                        settings.AreaDifferenceCutoffRatio,
                        settings.GroundFloorPenaltyRatio,
                        settings.BasementFloorPenaltyRatio,
                        settings.YearDifferenceCutoffYears,
                        settings.UpdatedAt,
                        settings.UpdatedBy
                    }
            );


            await _context.SaveChangesAsync();

            return ServiceResult.Ok(
                settings
            );
        }

        private static void Validate(
            EstimationSetting settings)
        {
            if (settings.TopK < 1)
            {
                throw new InvalidOperationException(
                    "Top K 必須大於 0。"
                );
            }

            if (settings.MinimumSampleCount < 1 ||
                settings.MinimumSampleCount >
                settings.TopK)
            {
                throw new InvalidOperationException(
                    "最低樣本數必須介於 1 與 Top K 之間。"
                );
            }

            if (
                settings.LowerQuantile < 0 ||
                settings.LowerQuantile >=
                settings.MedianQuantile ||
                settings.MedianQuantile >=
                settings.UpperQuantile ||
                settings.UpperQuantile > 1
            )
            {
                throw new InvalidOperationException(
                    "合理區間分位數設定不正確。"
                );
            }

            if (settings.SimilarityExponent <= 0)
            {
                throw new InvalidOperationException(
                    "Similarity Exponent 必須大於 0。"
                );
            }

            if (settings.TimeHalfLifeYears <= 0)
            {
                throw new InvalidOperationException(
                    "時間半衰期必須大於 0。"
                );
            }
            if (
                settings.MinimumSimilarityScore < 0 ||
                settings.MinimumSimilarityScore > 100
            )
            {
                throw new InvalidOperationException(
                    "最低相似度必須介於 0 到 100 之間。"
                );
            }


            if (
                settings.StructureWeight < 0 ||
                settings.AreaWeight < 0 ||
                settings.SiteAreaWeight < 0 ||
                settings.FloorWeight < 0 ||
                settings.YearWeight < 0 ||
                settings.CityWeight < 0
            )
            {
                throw new InvalidOperationException(
                    "相似度權重不可小於 0。"
                );
            }


            decimal totalWeight =
                settings.StructureWeight +
                settings.AreaWeight +
                settings.SiteAreaWeight +
                settings.FloorWeight +
                settings.YearWeight +
                settings.CityWeight;

            if (totalWeight <= 0)
            {
                throw new InvalidOperationException(
                    "相似度權重總和必須大於 0。"
                );
            }


            if (
                settings.AreaDifferenceCutoffRatio <= 0 ||
                settings.AreaDifferenceCutoffRatio > 1
            )
            {
                throw new InvalidOperationException(
                    "面積差距截止比例必須介於 0 到 1 之間。"
                );
            }


            if (
                settings.SiteAreaDifferenceCutoffRatio <= 0 ||
                settings.SiteAreaDifferenceCutoffRatio > 1
            )
            {
                throw new InvalidOperationException(
                    "基地面積差距截止比例必須介於 0 到 1 之間。"
                );
            }


            if (
                settings.GroundFloorPenaltyRatio < 0 ||
                settings.GroundFloorPenaltyRatio > 1
            )
            {
                throw new InvalidOperationException(
                    "地上樓層懲罰比例必須介於 0 到 1 之間。"
                );
            }


            if (
                settings.BasementFloorPenaltyRatio < 0 ||
                settings.BasementFloorPenaltyRatio > 1
            )
            {
                throw new InvalidOperationException(
                    "地下樓層懲罰比例必須介於 0 到 1 之間。"
                );
            }


            if (settings.YearDifferenceCutoffYears <= 0)
            {
                throw new InvalidOperationException(
                    "年份差距截止值必須大於 0。"
                );
            }
        }
    }
}