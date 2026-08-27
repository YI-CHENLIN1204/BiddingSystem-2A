namespace 崴正.Services
{
    public class UnitConversionResult
    {
        public bool Success { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string SourceUnit { get; set; } =
            string.Empty;

        public string TargetUnit { get; set; } =
            string.Empty;

        public decimal ConversionFactor { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class UnitConversionService
    {
        private sealed class UnitDefinition
        {
            public string Dimension { get; init; } =
                string.Empty;

            public decimal ToBaseFactor { get; init; }
        }

        private static readonly Dictionary<
            string,
            UnitDefinition
        > UnitDefinitions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // ==========================================
                // 長度
                // 基準：m
                // ==========================================

                ["m"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 1m
                },

                ["公尺"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 1m
                },

                ["米"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 1m
                },

                ["cm"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 0.01m
                },

                ["公分"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 0.01m
                },

                ["厘米"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 0.01m
                },

                ["mm"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 0.001m
                },

                ["毫米"] = new UnitDefinition
                {
                    Dimension = "length",
                    ToBaseFactor = 0.001m
                },


                // ==========================================
                // 面積
                // 基準：m2
                // ==========================================

                ["m2"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 1m
                },

                ["㎡"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 1m
                },

                ["m²"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 1m
                },

                ["平方公尺"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 1m
                },

                ["平方米"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 1m
                },

                ["cm2"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 0.0001m
                },

                ["cm²"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 0.0001m
                },

                ["平方公分"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 0.0001m
                },

                ["mm2"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 0.000001m
                },

                ["mm²"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 0.000001m
                },

                ["平方毫米"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 0.000001m
                },

                ["坪"] = new UnitDefinition
                {
                    Dimension = "area",
                    ToBaseFactor = 3.305785124m
                },


                // ==========================================
                // 體積
                // 基準：m3
                // ==========================================

                ["m3"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 1m
                },

                ["m³"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 1m
                },

                ["立方公尺"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 1m
                },

                ["立方米"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 1m
                },

                ["l"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 0.001m
                },

                ["公升"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 0.001m
                },

                ["升"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 0.001m
                },

                ["ml"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 0.000001m
                },

                ["毫升"] = new UnitDefinition
                {
                    Dimension = "volume",
                    ToBaseFactor = 0.000001m
                },


                // ==========================================
                // 重量
                // 基準：kg
                // ==========================================

                ["kg"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1m
                },

                ["公斤"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1m
                },

                ["千克"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1m
                },

                ["g"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 0.001m
                },

                ["公克"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 0.001m
                },

                ["克"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 0.001m
                },

                ["t"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1000m
                },

                ["ton"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1000m
                },

                ["噸"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1000m
                },

                ["公噸"] = new UnitDefinition
                {
                    Dimension = "mass",
                    ToBaseFactor = 1000m
                }
            };


        public string NormalizeUnit(
            string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                return string.Empty;
            }

            return unit
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "");
        }


        public bool CanConvert(
            string? sourceUnit,
            string? targetUnit)
        {
            return TryGetConversionFactor(
                sourceUnit,
                targetUnit,
                out _
            );
        }


        public bool TryGetConversionFactor(
            string? sourceUnit,
            string? targetUnit,
            out decimal factor)
        {
            factor = 0m;

            string source =
                NormalizeUnit(sourceUnit);

            string target =
                NormalizeUnit(targetUnit);

            if (string.IsNullOrWhiteSpace(source) ||
                string.IsNullOrWhiteSpace(target))
            {
                return false;
            }

            // ==========================================
            // 完全相同的未知單位仍然可以使用
            //
            // 例如：
            // 式 → 式
            // 組 → 組
            // 支 → 支
            // ==========================================

            if (source == target)
            {
                factor = 1m;
                return true;
            }

            if (!UnitDefinitions.TryGetValue(
                    source,
                    out var sourceDefinition))
            {
                return false;
            }

            if (!UnitDefinitions.TryGetValue(
                    target,
                    out var targetDefinition))
            {
                return false;
            }

            if (sourceDefinition.Dimension !=
                targetDefinition.Dimension)
            {
                return false;
            }

            factor =
                sourceDefinition.ToBaseFactor /
                targetDefinition.ToBaseFactor;

            return true;
        }


        public UnitConversionResult Convert(
            decimal quantity,
            decimal unitPrice,
            string? sourceUnit,
            string? targetUnit)
        {
            if (!TryGetConversionFactor(
                    sourceUnit,
                    targetUnit,
                    out decimal factor))
            {
                return new UnitConversionResult
                {
                    Success = false,

                    SourceUnit =
                        sourceUnit ?? string.Empty,

                    TargetUnit =
                        targetUnit ?? string.Empty,

                    ErrorMessage =
                        $"單位「{sourceUnit ?? "空白"}」無法轉換為「{targetUnit ?? "空白"}」"
                };
            }

            decimal convertedQuantity =
                quantity * factor;

            decimal convertedUnitPrice =
                unitPrice / factor;

            return new UnitConversionResult
            {
                Success = true,

                Quantity =
                    convertedQuantity,

                UnitPrice =
                    convertedUnitPrice,

                SourceUnit =
                    sourceUnit ?? string.Empty,

                TargetUnit =
                    targetUnit ?? string.Empty,

                ConversionFactor =
                    factor
            };
        }
    }
}