using 崴正.Models;

namespace 崴正.Services
{
    public class TwoAPreprocessService
    {
        private readonly UnitConversionService
            _unitConversionService;


        public TwoAPreprocessService(
            UnitConversionService
                unitConversionService)
        {
            _unitConversionService =
                unitConversionService;
        }


        private static readonly
            Dictionary<string, string>
                ExcludedChapterReasons =
            new(
                StringComparer.OrdinalIgnoreCase
            )
            {
                ["假設工程"] =
                    "受施工規劃、工地配置與現場條件影響較大，不納入 2A 自動估算。",

                ["土方及基礎工程"] =
                    "受地質、開挖深度與基礎設計影響較大，僅靠建案基本特徵不適合直接估算。",

                ["防水工程"] =
                    "受實際防水範圍、節點與設計圖影響較大，不納入 2A 自動估算。",

                ["門窗工程"] =
                    "數量與規格直接由設計圖及開口配置決定，不納入 2A 自動估算。",

                ["景觀工程"] =
                    "受景觀設計內容影響較大，不納入 2A 自動估算。",

                ["外部裝修工程"] =
                    "受立面設計與材料選用影響較大，不納入 2A 自動估算。",

                ["雜項工程"] =
                    "工項內容依個案差異大，不建立通用歷史估算基準。"
            };


        public TwoAProjectPreprocessResponse
            Build(
                int projectId,
                IReadOnlyCollection<
                    OneDSavedResultItem> sourceItems)
        {
            var items =
                sourceItems
                    .Select(
                        BuildItem
                    )
                    .ToList();

            foreach (var item in
                items.Where(x =>
                    x.Status == "Included" &&
                    !string.IsNullOrWhiteSpace(
                        x.DetectionCategory
                    )))
            {
                ApplyUnitNormalization(
                    item
                );
            }

            var majorCategories =
                items
                    .GroupBy(
                        x => x.MajorCategory
                    )
                    .Select(group =>
                    {
                        bool isEstimable =
                            IsEstimableChapter(
                                group.Key
                            );

                        return
                            new
                            TwoAMajorCategorySummaryDto
                            {
                                MajorCategory =
                                    group.Key,

                                ItemCount =
                                    group.Count(),

                                IsEstimable =
                                    isEstimable,

                                Reason =
                                    isEstimable
                                        ? "納入 2A：可依歷史案件建立材料用量比較。"
                                        : GetExcludedReason(
                                            group.Key
                                        ),

                                DetectionCategories =
                                    group
                                        .Where(x =>
                                            !string
                                                .IsNullOrWhiteSpace(
                                                    x.DetectionCategory
                                                ))
                                        .Select(x =>
                                            x.DetectionCategory!)
                                        .Distinct(
                                            StringComparer
                                                .OrdinalIgnoreCase
                                        )
                                        .OrderBy(x => x)
                                        .ToList()
                            };
                    })
                    .OrderBy(x =>
                        x.MajorCategory)
                    .ToList();

            var groups =
                items
                    .Where(x =>
                        x.Status ==
                            "Included" &&
                        !string.IsNullOrWhiteSpace(
                            x.DetectionCategory
                        ))
                    .GroupBy(x => (
                        MajorCategory:
                            x.MajorCategory,
                        DetectionCategory:
                            x.DetectionCategory!
                    ))
                    .Select(BuildGroup)
                    .OrderBy(x =>
                        x.MajorCategory)
                    .ThenBy(x =>
                        x.DetectionCategory)
                    .ToList();

            return
                new TwoAProjectPreprocessResponse
                {
                    ProjectId =
                        projectId,

                    SourceItemCount =
                        items.Count,

                    MajorCategoryCount =
                        majorCategories.Count,

                    IncludedItemCount =
                        items.Count(x =>
                            x.Status ==
                                "Included"),

                    ExcludedItemCount =
                        items.Count(x =>
                            x.Status ==
                                "Excluded"),

                    PendingItemCount =
                        items.Count(x =>
                            x.Status ==
                                "Pending"),

                    MajorCategories =
                        majorCategories,

                    Groups =
                        groups,

                    Items =
                        items
                };
        }


        private static
            TwoAPreprocessItemDto
            BuildItem(
                OneDSavedResultItem item)
        {
            string chapter =
                item.Chapter?.Trim()
                ?? string.Empty;

            if (!IsEstimableChapter(
                    chapter))
            {
                return
                    CreateItem(
                        item,
                        null,
                        "Excluded",
                        GetExcludedReason(
                            chapter
                        )
                    );
            }

            if (chapter.Contains(
                    "結構體",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                if (ContainsAny(
                        item.Name,
                        "續接器",
                        "套筒",
                        "接頭"))
                {
                    return
                        CreateItem(
                            item,
                            null,
                            "Pending",
                            "屬鋼筋相關配件，但不可直接併入鋼筋重量，需另行確認。"
                        );
                }

                if (ContainsAny(
                        item.Name,
                        "鋼筋",
                        "SD420W",
                        "SD420",
                        "SD280",
                        "竹節鋼筋",
                        "高拉力鋼筋"))
                {
                    return
                        CreateItem(
                            item,
                            "鋼筋",
                            "Included",
                            "結構體工程中的鋼筋工項，納入鋼筋用量比較。"
                        );
                }

                if (ContainsAny(
                        item.Name,
                        "混凝土",
                        "預拌混凝土",
                        "預拌"))
                {
                    return
                        CreateItem(
                            item,
                            "混凝土",
                            "Included",
                            "結構體工程中的混凝土工項，納入混凝土用量比較。"
                        );
                }

                if (ContainsAny(
                        item.Name,
                        "模板",
                        "模版",
                        "系統模",
                        "清水模"))
                {
                    return
                        CreateItem(
                            item,
                            "模板",
                            "Included",
                            "結構體工程中的模板工項，納入模板用量比較。"
                        );
                }

                return
                    CreateItem(
                        item,
                        null,
                        "Pending",
                        "此工項位於可估算的結構體工程，但尚未建立 2A 偵錯細項規則。"
                    );
            }

            if (chapter.Contains(
                    "鋼骨",
                    StringComparison
                        .OrdinalIgnoreCase) ||
                chapter.Contains(
                    "鋼構",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                if (ContainsAny(
                        item.Name,
                        "鋼骨",
                        "鋼構",
                        "鋼結構",
                        "H型鋼",
                        "H 型鋼",
                        "型鋼",
                        "鋼樑",
                        "鋼梁",
                        "鋼柱"))
                {
                    return
                        CreateItem(
                            item,
                            "鋼骨",
                            "Included",
                            "鋼骨鋼構工程中的主要鋼構工項，納入鋼骨用量比較。"
                        );
                }

                return
                    CreateItem(
                        item,
                        null,
                        "Pending",
                        "此工項位於可估算的鋼骨鋼構工程，但尚未建立 2A 偵錯細項規則。"
                    );
            }

            return
                CreateItem(
                    item,
                    null,
                    "Excluded",
                    GetExcludedReason(
                        chapter
                    )
                );
        }


        private
            TwoADetectionGroupDto
            BuildGroup(
                IGrouping<
                    (
                        string MajorCategory,
                        string DetectionCategory
                    ),
                    TwoAPreprocessItemDto>
                        group)
        {
            var sourceUnits =
                group
                    .Select(x =>
                        NormalizeUnit(
                            x.Unit
                        ))
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x))
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .OrderBy(x => x)
                    .ToList();

            bool missingQuantityOrUnit =
                group.Any(x =>
                    !x.Quantity.HasValue ||
                    string.IsNullOrWhiteSpace(
                        x.Unit
                    ));

            bool normalizationFailed =
                group.Any(x =>
                    x.UnitNormalizationStatus ==
                        "UnitNotConfigured" ||
                    x.UnitNormalizationStatus ==
                        "ConversionUnavailable");

            var normalizedUnits =
                group
                    .Select(x =>
                        x.NormalizedUnit)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x))
                    .Select(x => x!)
                    .Distinct(
                        StringComparer
                            .OrdinalIgnoreCase
                    )
                    .ToList();

            bool ready =
                !missingQuantityOrUnit &&
                !normalizationFailed &&
                normalizedUnits.Count == 1 &&
                group.All(x =>
                    x.NormalizedQuantity
                        .HasValue);

            bool normalizationApplied =
                group.Any(x =>
                    x.UnitConversionFactor
                        .HasValue &&
                    x.UnitConversionFactor.Value !=
                        1m);

            string aggregationStatus =
                missingQuantityOrUnit
                    ? "MissingQuantityOrUnit"
                    : normalizationFailed
                        ? "UnitNormalizationFailed"
                        : normalizedUnits.Count != 1
                            ? "IncompatibleUnits"
                            : "Ready";

            return
                new TwoADetectionGroupDto
                {
                    MajorCategory =
                        group.Key
                            .MajorCategory,

                    DetectionCategory =
                        group.Key
                            .DetectionCategory,

                    ItemCount =
                        group.Count(),

                    Units =
                        sourceUnits,

                    AggregationStatus =
                        aggregationStatus,

                    StandardUnit =
                        ready
                            ? normalizedUnits[0]
                            : null,

                    NormalizationApplied =
                        normalizationApplied,

                    TotalQuantity =
                        ready
                            ? group.Sum(x =>
                                x.NormalizedQuantity!
                                    .Value)
                            : null
                };
        }


        private void ApplyUnitNormalization(
            TwoAPreprocessItemDto item)
        {
            if (!item.Quantity.HasValue ||
                string.IsNullOrWhiteSpace(
                    item.Unit))
            {
                item.UnitNormalizationStatus =
                    "MissingQuantityOrUnit";

                return;
            }

            var result =
                _unitConversionService
                    .ConvertQuantityToBaseUnit(
                        item.Quantity.Value,
                        item.Unit
                    );

            if (!result.Success)
            {
                item.UnitNormalizationStatus =
                    result.ErrorMessage?
                        .Contains(
                            "尚未建立於單位主檔",
                            StringComparison
                                .OrdinalIgnoreCase
                        ) == true
                        ? "UnitNotConfigured"
                        : "ConversionUnavailable";

                return;
            }

            item.NormalizedQuantity =
                result.Quantity;

            item.NormalizedUnit =
                result.TargetUnit;

            item.UnitConversionFactor =
                result.ConversionFactor;

            item.UnitNormalizationStatus =
                "Ready";
        }


        private static
            TwoAPreprocessItemDto
            CreateItem(
                OneDSavedResultItem item,
                string? detectionCategory,
                string status,
                string reason)
        {
            return
                new TwoAPreprocessItemDto
                {
                    MajorCategory =
                        item.Chapter?.Trim()
                        ?? string.Empty,

                    DetectionCategory =
                        detectionCategory,

                    Code =
                        item.Code,

                    Name =
                        item.Name,

                    Unit =
                        item.Unit,

                    Quantity =
                        item.Quantity,

                    Confidence =
                        item.Confidence,

                    Status =
                        status,

                    Reason =
                        reason
                };
        }


        private static bool
            IsEstimableChapter(
                string chapter)
        {
            return
                TwoAScopePolicy
                    .IsEstimableMajorCategory(
                        chapter
                    );
        }


        private static string
            GetExcludedReason(
                string chapter)
        {
            return
                ExcludedChapterReasons
                    .TryGetValue(
                        chapter,
                        out var reason)
                    ? reason
                    : "目前沒有足夠依據建立可採用的歷史估算基準，因此不納入 2A 自動估算。";
        }


        private static bool
            ContainsAny(
                string? value,
                params string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }

            return
                keywords.Any(keyword =>
                    value.Contains(
                        keyword,
                        StringComparison
                            .OrdinalIgnoreCase
                    ));
        }


        private static string
            NormalizeUnit(
                string? unit)
        {
            return
                string.IsNullOrWhiteSpace(
                    unit)
                    ? string.Empty
                    : unit.Trim()
                        .ToUpperInvariant();
        }
    }
}
