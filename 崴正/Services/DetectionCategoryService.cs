using 崴正.Models;

namespace 崴正.Services
{
    /// <summary>
    /// 2A 偵錯細項分類器。
    ///
    /// 1D 已完成整個建案的工程大項分類；
    /// 2A 不重做工程大項，而是在各大項內建立異常偵測所需的細項分類。
    ///
    /// 目前此服務仍屬 POC：只做分類與彙整預覽，不寫入正式異常結果。
    /// </summary>
    public class DetectionCategoryService
    {
        private static readonly string[]
            RebarAccessoryKeywords =
            {
                "續接器",
                "套筒",
                "接頭"
            };

        private static readonly string[]
            RebarKeywords =
            {
                "鋼筋",
                "SD420W",
                "SD420",
                "SD280",
                "竹節鋼筋",
                "高拉力鋼筋"
            };

        private static readonly string[]
            ConcreteKeywords =
            {
                "混凝土",
                "預拌混凝土",
                "預拌"
            };

        private static readonly string[]
            FormworkKeywords =
            {
                "模板",
                "模版",
                "系統模",
                "清水模"
            };

        private static readonly string[]
            StructuralSteelKeywords =
            {
                "鋼骨",
                "鋼構",
                "鋼結構",
                "H型鋼",
                "H 型鋼",
                "型鋼",
                "鋼樑",
                "鋼梁",
                "鋼柱"
            };

        public DetectionCategoryPreviewResponse
            BuildPreview(
                IEnumerable<OneDSavedResultItem> sourceItems)
        {
            var items =
                sourceItems
                    .Select(Classify)
                    .ToList();

            var groups =
                BuildDetectionGroups(
                    items
                );

            var majorCategories =
                items
                    .GroupBy(x =>
                        x.MajorCategory
                    )
                    .Select(g =>
                    {
                        var detectionCategories =
                            g.Where(x =>
                                    x.ClassificationStatus ==
                                        "Classified" &&
                                    !string.IsNullOrWhiteSpace(
                                        x.DetectionCategory
                                    )
                                )
                                .Select(x =>
                                    x.DetectionCategory!
                                )
                                .Distinct(
                                    StringComparer
                                        .OrdinalIgnoreCase
                                )
                                .OrderBy(x => x)
                                .ToList();

                        return new MajorCategorySummaryDto
                        {
                            MajorCategory =
                                g.Key,

                            ItemCount =
                                g.Count(),

                            ClassifiedItemCount =
                                g.Count(x =>
                                    x.ClassificationStatus ==
                                    "Classified"
                                ),

                            PendingItemCount =
                                g.Count(x =>
                                    x.ClassificationStatus !=
                                    "Classified"
                                ),

                            HasDetectionRules =
                                detectionCategories.Count > 0,

                            DetectionCategories =
                                detectionCategories
                        };
                    })
                    .OrderBy(x =>
                        x.MajorCategory
                    )
                    .ToList();

            return new DetectionCategoryPreviewResponse
            {
                TotalItems =
                    items.Count,

                MajorCategoryCount =
                    majorCategories.Count,

                ClassifiedItems =
                    items.Count(x =>
                        x.ClassificationStatus ==
                        "Classified"
                    ),

                PendingItems =
                    items.Count(x =>
                        x.ClassificationStatus !=
                        "Classified"
                    ),

                MajorCategories =
                    majorCategories,

                Groups =
                    groups,

                Items =
                    items
            };
        }

        private static List<DetectionCategoryGroupDto>
            BuildDetectionGroups(
                IEnumerable<DetectionCategoryPreviewItem> items)
        {
            return items
                .Where(x =>
                    x.ClassificationStatus ==
                    "Classified" &&
                    !string.IsNullOrWhiteSpace(
                        x.DetectionCategory
                    )
                )
                .GroupBy(x => new
                {
                    x.MajorCategory,
                    x.DetectionCategory
                })
                .Select(g =>
                {
                    var units =
                        g.Select(x =>
                                x.Unit?.Trim()
                            )
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x)
                            )
                            .Select(x => x!)
                            .Distinct(
                                StringComparer
                                    .OrdinalIgnoreCase
                            )
                            .OrderBy(x => x)
                            .ToList();

                    var quantities =
                        g.Where(x =>
                                x.Quantity.HasValue
                            )
                            .Select(x =>
                                x.Quantity!.Value
                            )
                            .ToList();

                    bool hasMixedUnits =
                        units.Count > 1;

                    bool canAggregate =
                        units.Count == 1 &&
                        quantities.Count ==
                        g.Count();

                    return new DetectionCategoryGroupDto
                    {
                        MajorCategory =
                            g.Key.MajorCategory,

                        DetectionCategory =
                            g.Key.DetectionCategory!,

                        ItemCount =
                            g.Count(),

                        Units =
                            units,

                        TotalQuantity =
                            canAggregate
                                ? quantities.Sum()
                                : null,

                        AggregateUnit =
                            canAggregate
                                ? units[0]
                                : null,

                        AggregationStatus =
                            canAggregate
                                ? "Ready"
                                : hasMixedUnits
                                    ? "NeedsUnitNormalization"
                                    : "MissingQuantityOrUnit",

                        AggregationMessage =
                            canAggregate
                                ? "同一偵錯細項之工項單位一致，可直接彙整。"
                                : hasMixedUnits
                                    ? "同一偵錯細項出現不同單位，需先依 2A 單位主檔完成標準化後再彙整。"
                                    : "部分工項缺少數量或單位，暫不納入正式數量彙整。"
                    };
                })
                .OrderBy(x =>
                    x.MajorCategory
                )
                .ThenBy(x =>
                    x.DetectionCategory
                )
                .ToList();
        }

        public DetectionCategoryPreviewItem
            Classify(
                OneDSavedResultItem item)
        {
            string chapter =
                Normalize(
                    item.Chapter
                );

            string name =
                Normalize(
                    item.Name
                );

            var result =
                new DetectionCategoryPreviewItem
                {
                    SourceCategory =
                        item.Category,

                    MajorCategory =
                        string.IsNullOrWhiteSpace(
                            item.Chapter
                        )
                            ? "未分類"
                            : item.Chapter!.Trim(),

                    OriginalItemName =
                        item.Name?.Trim()
                        ?? string.Empty,

                    ItemCode =
                        item.Code,

                    Unit =
                        item.Unit,

                    Quantity =
                        item.Quantity,

                    OneDConfidence =
                        item.Confidence
                };

            if (
                string.IsNullOrWhiteSpace(
                    chapter
                )
            )
            {
                return Pending(
                    result,
                    "1D 尚未提供工程大項"
                );
            }

            // 1D 已完成「結構體工程」大項分類，
            // 2A 只在此大項內往下建立偵錯細項。
            if (
                chapter.Contains(
                    "結構體",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    ContainsAny(
                        name,
                        RebarAccessoryKeywords
                    )
                )
                {
                    return Pending(
                        result,
                        "1D 已分類為結構體工程；此工項為鋼筋相關配件，暫不併入鋼筋重量"
                    );
                }

                var rebar =
                    FirstMatchedKeyword(
                        name,
                        RebarKeywords
                    );

                if (rebar != null)
                {
                    return Classified(
                        result,
                        "鋼筋",
                        $"1D 大項「結構體工程」＋關鍵字「{rebar}」"
                    );
                }

                var concrete =
                    FirstMatchedKeyword(
                        name,
                        ConcreteKeywords
                    );

                if (concrete != null)
                {
                    return Classified(
                        result,
                        "混凝土",
                        $"1D 大項「結構體工程」＋關鍵字「{concrete}」"
                    );
                }

                var formwork =
                    FirstMatchedKeyword(
                        name,
                        FormworkKeywords
                    );

                if (formwork != null)
                {
                    return Classified(
                        result,
                        "模板",
                        $"1D 大項「結構體工程」＋關鍵字「{formwork}」"
                    );
                }

                return Pending(
                    result,
                    "1D 已分類為結構體工程；2A 尚未建立此工項的偵錯細項規則"
                );
            }

            // 1D 已完成「鋼骨鋼構工程」大項分類，
            // 2A 再建立可供數量異常分析的「鋼骨」細項。
            if (
                chapter.Contains(
                    "鋼骨",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                chapter.Contains(
                    "鋼構",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var steel =
                    FirstMatchedKeyword(
                        name,
                        StructuralSteelKeywords
                    );

                if (steel != null)
                {
                    return Classified(
                        result,
                        "鋼骨",
                        $"1D 大項「{result.MajorCategory}」＋關鍵字「{steel}」"
                    );
                }

                return Pending(
                    result,
                    $"1D 已分類為「{result.MajorCategory}」；2A 尚未建立此工項的偵錯細項規則"
                );
            }

            // 其他工程大項仍完整保留。
            // 此 POC 不硬把尚未確認的工程內容塞進錯誤材料類別。
            return Pending(
                result,
                $"1D 已完成大項「{result.MajorCategory}」分類；2A 尚未建立此大項的偵錯細項規則"
            );
        }

        private static DetectionCategoryPreviewItem
            Classified(
                DetectionCategoryPreviewItem result,
                string category,
                string rule)
        {
            result.DetectionCategory =
                category;

            result.ClassificationStatus =
                "Classified";

            result.MatchedRule =
                rule;

            return result;
        }

        private static DetectionCategoryPreviewItem
            Pending(
                DetectionCategoryPreviewItem result,
                string reason)
        {
            result.DetectionCategory =
                null;

            result.ClassificationStatus =
                "Pending";

            result.MatchedRule =
                reason;

            return result;
        }

        private static bool ContainsAny(
            string text,
            IEnumerable<string> keywords)
        {
            return FirstMatchedKeyword(
                text,
                keywords
            ) != null;
        }

        private static string?
            FirstMatchedKeyword(
                string text,
                IEnumerable<string> keywords)
        {
            return keywords
                .FirstOrDefault(keyword =>
                    text.Contains(
                        Normalize(keyword),
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

        private static string Normalize(
            string? value)
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return string.Empty;
            }

            return value
                .Trim()
                .Replace("　", " ")
                .Replace("＃", "#")
                .Replace("－", "-")
                .ToUpperInvariant();
        }
    }
}
