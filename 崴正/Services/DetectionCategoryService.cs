using 崴正.Models;

namespace 崴正.Services
{
    /// <summary>
    /// 2A 偵錯細項分類器。
    ///
    /// 1D 已先把工項限縮至工程大項（例如結構體工程、鋼骨鋼構工程），
    /// 2A 再依異常偵測需求把工程大項內的工項整理成可統計的材料細項。
    ///
    /// 目前只做「分類預覽」，不寫入資料庫、不改動既有異常偵測流程。
    /// 等崴正確認細項粒度後，再把規則主檔化並接到正式分析流程。
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
                items
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
                        new DetectionCategoryGroupDto
                        {
                            MajorCategory =
                                g.Key.MajorCategory,

                            DetectionCategory =
                                g.Key.DetectionCategory!,

                            ItemCount =
                                g.Count(),

                            Units =
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
                                    .ToList()
                        }
                    )
                    .OrderBy(x =>
                        x.MajorCategory)
                    .ThenBy(x =>
                        x.DetectionCategory)
                    .ToList();

            return new DetectionCategoryPreviewResponse
            {
                TotalItems =
                    items.Count,

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

                Groups =
                    groups,

                Items =
                    items
            };
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

            // ------------------------------------------
            // 結構體工程
            // ------------------------------------------
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
                        "鋼筋相關配件暫不併入鋼筋重量"
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
                        $"結構體工程 + 關鍵字「{rebar}」"
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
                        $"結構體工程 + 關鍵字「{concrete}」"
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
                        $"結構體工程 + 關鍵字「{formwork}」"
                    );
                }

                return Pending(
                    result,
                    "已限縮為結構體工程，但尚未符合鋼筋、混凝土或模板規則"
                );
            }

            // ------------------------------------------
            // 鋼骨鋼構工程
            // ------------------------------------------
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
                        $"鋼骨鋼構工程 + 關鍵字「{steel}」"
                    );
                }

                return Pending(
                    result,
                    "已限縮為鋼骨鋼構工程，但尚未符合鋼骨材料規則"
                );
            }

            return Pending(
                result,
                $"目前尚未建立「{result.MajorCategory}」的 2A 偵錯細項規則"
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
