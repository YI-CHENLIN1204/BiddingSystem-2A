using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class EstimationSettingsService 
    {
        private readonly ApplicationDbContext _context;

        public EstimationSettingsService(
            ApplicationDbContext context)
        {
            _context = context;
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
        }
    }
}