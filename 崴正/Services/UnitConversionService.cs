using Microsoft.EntityFrameworkCore;
using 崴正.Data;

namespace 崴正.Services
{
    public class UnitConversionResult
    {
        public bool Success { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string SourceUnit { get; set; }
            = string.Empty;

        public string TargetUnit { get; set; }
            = string.Empty;

        public decimal ConversionFactor { get; set; }

        public string? ErrorMessage { get; set; }
    }


    public class UnitConversionService
    {
        private readonly ApplicationDbContext _context;

        private Dictionary<string, UnitDefinition>?
            _unitsByName;

        private Dictionary<int, UnitDefinition>?
            _unitsById;

        private Dictionary<int, UnitDefinition>?
            _baseUnitsByCategoryId;


        private sealed class UnitDefinition
        {
            public int UnitId { get; init; }

            public string UnitName { get; init; }
                = string.Empty;

            public int UnitCategoryId { get; init; }

            public bool AllowConversion { get; init; }

            public decimal? ToBaseFactor { get; init; }

            public bool IsBaseUnit { get; init; }
        }


        public UnitConversionService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public string NormalizeUnit(
            string? unit)
        {
            return string.IsNullOrWhiteSpace(unit)
                ? string.Empty
                : unit.Trim()
                    .ToLowerInvariant()
                    .Replace(" ", "");
        }


        public bool CanConvert(
            string? sourceUnit,
            int targetUnitId)
        {
            return TryGetConversionFactor(
                sourceUnit,
                targetUnitId,
                out _,
                out _
            );
        }


        public bool TryGetConversionFactor(
            string? sourceUnit,
            int targetUnitId,
            out decimal factor,
            out string targetUnitName)
        {
            EnsureUnitsLoaded();

            factor = 0m;
            targetUnitName =
                string.Empty;


            string source =
                NormalizeUnit(
                    sourceUnit
                );


            if (string.IsNullOrWhiteSpace(
                    source))
            {
                return false;
            }


            if (!_unitsById!.TryGetValue(
                    targetUnitId,
                    out var targetDefinition))
            {
                return false;
            }


            targetUnitName =
                targetDefinition.UnitName;


            if (!_unitsByName!.TryGetValue(
                    source,
                    out var sourceDefinition))
            {
                return false;
            }


            if (sourceDefinition.UnitId ==
                targetDefinition.UnitId)
            {
                factor = 1m;

                return true;
            }


            if (sourceDefinition.UnitCategoryId !=
                targetDefinition.UnitCategoryId)
            {
                return false;
            }


            // 不允許換算的類別：
            // 只有完全相同單位才成立
            if (!targetDefinition.AllowConversion)
            {
                return false;
            }


            if (sourceDefinition.ToBaseFactor is null or <= 0 ||
                targetDefinition.ToBaseFactor is null or <= 0)
            {
                return false;
            }


            factor =
                sourceDefinition.ToBaseFactor.Value /
                targetDefinition.ToBaseFactor.Value;


            return factor > 0;
        }


        public UnitConversionResult Convert(
            decimal quantity,
            decimal unitPrice,
            string? sourceUnit,
            int targetUnitId)
        {
            if (!TryGetConversionFactor(
                    sourceUnit,
                    targetUnitId,
                    out decimal factor,
                    out string targetUnitName))
            {
                return new UnitConversionResult
                {
                    Success = false,

                    SourceUnit =
                        sourceUnit ??
                        string.Empty,

                    TargetUnit =
                        targetUnitName,

                    ErrorMessage =
                        $"單位「{sourceUnit ?? "空白"}」無法轉換為標準材料單位「{targetUnitName}」"
                };
            }


            return new UnitConversionResult
            {
                Success = true,

                Quantity =
                    quantity * factor,

                UnitPrice =
                    unitPrice / factor,

                SourceUnit =
                    sourceUnit ??
                    string.Empty,

                TargetUnit =
                    targetUnitName,

                ConversionFactor =
                    factor
            };
        }


        public UnitConversionResult
            ConvertQuantityToBaseUnit(
                decimal quantity,
                string? sourceUnit)
        {
            EnsureUnitsLoaded();

            string normalizedSource =
                NormalizeUnit(
                    sourceUnit
                );

            if (string.IsNullOrWhiteSpace(
                    normalizedSource) ||
                !_unitsByName!.TryGetValue(
                    normalizedSource,
                    out var sourceDefinition))
            {
                return new UnitConversionResult
                {
                    Success = false,
                    SourceUnit =
                        sourceUnit ??
                        string.Empty,
                    ErrorMessage =
                        $"單位「{sourceUnit ?? "空白"}」尚未建立於單位主檔。"
                };
            }

            if (!_baseUnitsByCategoryId!
                .TryGetValue(
                    sourceDefinition
                        .UnitCategoryId,
                    out var baseDefinition))
            {
                return new UnitConversionResult
                {
                    Success = false,
                    SourceUnit =
                        sourceUnit ??
                        string.Empty,
                    ErrorMessage =
                        $"單位「{sourceUnit}」所屬類別尚未設定基準單位。"
                };
            }

            return Convert(
                quantity,
                0m,
                sourceUnit,
                baseDefinition.UnitId
            );
        }


        public string GetUnitName(
            int unitId)
        {
            EnsureUnitsLoaded();

            return _unitsById!
                .TryGetValue(
                    unitId,
                    out var definition)
                ? definition.UnitName
                : string.Empty;
        }


        private void EnsureUnitsLoaded()
        {
            if (_unitsByName != null &&
                _unitsById != null &&
                _baseUnitsByCategoryId != null)
            {
                return;
            }


            var units =
                _context.UnitMasters
                    .AsNoTracking()
                    .Select(x => new UnitDefinition
                    {
                        UnitId =
                            x.UnitId,

                        UnitName =
                            x.UnitName,

                        UnitCategoryId =
                            x.UnitCategoryId,

                        AllowConversion =
                            x.UnitCategory
                                .AllowConversion,

                        ToBaseFactor =
                            x.ToBaseFactor,

                        IsBaseUnit =
                            x.IsBaseUnit
                    })
                    .ToList();


            _unitsById =
                units.ToDictionary(
                    x => x.UnitId
                );


            _unitsByName =
                units.ToDictionary(
                    x =>
                        NormalizeUnit(
                            x.UnitName
                        ),
                    StringComparer.OrdinalIgnoreCase
                );


            _baseUnitsByCategoryId =
                units
                    .Where(x =>
                        x.IsBaseUnit)
                    .GroupBy(x =>
                        x.UnitCategoryId)
                    .ToDictionary(
                        x => x.Key,
                        x => x
                            .OrderBy(unit =>
                                unit.UnitId)
                            .First()
                    );
        }
    }
}