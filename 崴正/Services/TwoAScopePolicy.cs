namespace 崴正.Services
{
    public static class TwoAScopePolicy
    {
        // 目前暫定的「細項數量偵錯」範圍。
        // 廠商確認前先聚焦在結構體工程中的鋼筋、混凝土。
        private static readonly HashSet<string>
            QuantityEstimableMajorCategories =
                new(
                    new[]
                    {
                        "結構體工程"
                    },
                    StringComparer.OrdinalIgnoreCase
                );


        private static readonly HashSet<string>
            QuantityDetectionCategories =
                new(
                    new[]
                    {
                        "鋼筋",
                        "混凝土"
                    },
                    StringComparer.OrdinalIgnoreCase
                );


        // 目前暫定的「大項估算 / 價格異常」單位。
        private static readonly HashSet<string>
            QuantityHistoryMaterialNames =
                new(
                    new[]
                    {
                        "鋼筋",
                        "混凝土"
                    },
                    StringComparer.OrdinalIgnoreCase
                );


        private static readonly HashSet<string>
            PriceEstimationMajorCategories =
                new(
                    new[]
                    {
                        "結構體工程"
                    },
                    StringComparer.OrdinalIgnoreCase
                );


        private static readonly HashSet<string>
            KnownMajorCategories =
                new(
                    new[]
                    {
                        "假設工程",
                        "安全觀測工程",
                        "輕隔間工程",
                        "配合使照工程",
                        "拆除工程",
                        "土方及基礎工程",
                        "鋼骨鋼構工程",
                        "結構體工程",
                        "防水工程",
                        "內外牆裝修工程",
                        "地坪裝修工程",
                        "天花板裝修工程",
                        "外部裝修工程",
                        "門窗工程",
                        "景觀工程",
                        "雜項工程"
                    },
                    StringComparer.OrdinalIgnoreCase
                );


        // Explicit source-heading aliases observed in real final tender spreadsheets.
        // Keep unknown headings unresolved rather than guessing an estimable scope.
        private static readonly Dictionary<string, string> MajorCategoryAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["土方工程"] = "土方及基礎工程",
                ["基礎工程(擋土柱及安全支撐工程)"] = "土方及基礎工程",
                ["平頂裝修工程"] = "天花板裝修工程",
                ["牆面裝修工程"] = "內外牆裝修工程"
            };

        private static string? NormalizeMajorCategory(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return null;

            var normalized = category.Trim();
            if (KnownMajorCategories.Contains(normalized))
                return normalized;

            return MajorCategoryAliases.TryGetValue(normalized, out var canonical)
                ? canonical
                : null;
        }

        // Explicit non-material charge: an accidentally matched rebar material
        // must never turn this transportation subsidy into a quantity baseline.
        public static bool IsExplicitNonMaterialCharge(string? itemName)
        {
            return !string.IsNullOrWhiteSpace(itemName) &&
                itemName.Contains("鋼筋不足噸數運費補貼", StringComparison.OrdinalIgnoreCase);
        }

        public static IReadOnlyCollection<string>
            GetKnownMajorCategories()
        {
            return KnownMajorCategories
                .OrderBy(x => x)
                .ToArray();
        }


        public static IReadOnlyCollection<string>
            GetEstimableMajorCategories()
        {
            return GetQuantityEstimableMajorCategories();
        }


        public static IReadOnlyCollection<string>
            GetQuantityEstimableMajorCategories()
        {
            return QuantityEstimableMajorCategories
                .OrderBy(x => x)
                .ToArray();
        }


        public static IReadOnlyCollection<string>
            GetQuantityDetectionCategories()
        {
            return QuantityDetectionCategories
                .OrderBy(x => x)
                .ToArray();
        }


        public static IReadOnlyCollection<string>
            GetPriceEstimationMajorCategories()
        {
            return PriceEstimationMajorCategories
                .OrderBy(x => x)
                .ToArray();
        }


        public static bool
            IsKnownMajorCategory(
                string? majorCategory)
        {
            if (string.IsNullOrWhiteSpace(
                    majorCategory))
            {
                return false;
            }

            return KnownMajorCategories
                .Contains(
                    majorCategory.Trim()
                );
        }


        public static bool
            IsEstimableMajorCategory(
                string? majorCategory)
        {
            return IsQuantityEstimableMajorCategory(
                majorCategory
            );
        }


        public static bool
            IsQuantityEstimableMajorCategory(
                string? majorCategory)
        {
            if (string.IsNullOrWhiteSpace(
                    majorCategory))
            {
                return false;
            }

            return QuantityEstimableMajorCategories
                .Contains(
                    majorCategory.Trim()
                );
        }


        public static bool
            IsQuantityDetectionCategory(
                string? detectionCategory)
        {
            if (string.IsNullOrWhiteSpace(
                    detectionCategory))
            {
                return false;
            }

            return QuantityDetectionCategories
                .Contains(
                    detectionCategory.Trim()
                );
        }


        public static bool
            IsQuantityHistoryMaterial(
                string? materialName)
        {
            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return false;
            }

            return QuantityHistoryMaterialNames
                .Contains(
                    materialName.Trim()
                );
        }


        public static bool
            IsPriceEstimationMajorCategory(
                string? majorCategory)
        {
            if (string.IsNullOrWhiteSpace(
                    majorCategory))
            {
                return false;
            }

            return PriceEstimationMajorCategories
                .Contains(
                    majorCategory.Trim()
                );
        }


        public static string?
            ResolveEffectiveMajorCategory(
                string? overrideCategory,
                params string?[] sourceCategories)
        {
            if (!string.IsNullOrWhiteSpace(
                    overrideCategory))
            {
                var resolvedOverride = NormalizeMajorCategory(overrideCategory);
                if (resolvedOverride != null)
                    return resolvedOverride;
            }

            return ResolveMajorCategory(
                sourceCategories
            );
        }


        public static string?
            ResolveMajorCategory(
                params string?[] categories)
        {
            foreach (var category
                in categories)
            {
                if (string.IsNullOrWhiteSpace(
                        category))
                {
                    continue;
                }

                var normalized = NormalizeMajorCategory(category);
                if (normalized != null)
                    return normalized;
            }

            return null;
        }
    }
}
