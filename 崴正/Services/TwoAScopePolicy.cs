namespace 崴正.Services
{
    public static class TwoAScopePolicy
    {
        private static readonly HashSet<string>
            EstimableMajorCategories =
                new(
                    new[]
                    {
                        "結構體工程",
                        "鋼骨鋼構工程"
                    },
                    StringComparer.OrdinalIgnoreCase
                );


        private static readonly HashSet<string>
            KnownMajorCategories =
                new(
                    new[]
                    {
                        "假設工程",
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


        public static bool
            IsEstimableMajorCategory(
                string? majorCategory)
        {
            if (string.IsNullOrWhiteSpace(
                    majorCategory))
            {
                return false;
            }

            return
                EstimableMajorCategories
                    .Contains(
                        majorCategory.Trim()
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

                string normalized =
                    category.Trim();

                if (KnownMajorCategories
                    .Contains(normalized))
                {
                    return normalized;
                }
            }

            return null;
        }
    }
}
