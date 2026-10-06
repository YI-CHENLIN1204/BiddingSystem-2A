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

        public ServiceResult
            CompareGranularity(
                GranularityPreviewRequest request)
        {
            if (
                request == null ||
                request.Items == null ||
                request.Items.Count == 0
            )
            {
                return ServiceResult.Validation(
                    "請至少提供一筆歷史工項"
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    request.TargetChapter
                ) ||
                string.IsNullOrWhiteSpace(
                    request.TargetItemName
                )
            )
            {
                return ServiceResult.Validation(
                    "請提供目標工程大項與原始工項名稱"
                );
            }

            int minimumSampleCount =
                Math.Max(
                    1,
                    request.MinimumSampleCount
                );

            var target =
                Classify(
                    new OneDSavedResultItem
                    {
                        Chapter =
                            request.TargetChapter,

                        Name =
                            request.TargetItemName
                    }
                );

            if (
                target.ClassificationStatus !=
                    "Classified" ||
                string.IsNullOrWhiteSpace(
                    target.DetectionCategory
                )
            )
            {
                return ServiceResult.Validation(
                    "目標工項目前無法歸入可比較的偵錯細項"
                );
            }

            string normalizedTargetName =
                Normalize(
                    request.TargetItemName
                );

            var specificationProjectIds =
                request.Items
                    .Where(x =>
                        Normalize(x.Name) ==
                        normalizedTargetName
                    )
                    .Select(x =>
                        x.ProjectId
                    )
                    .Where(x =>
                        x > 0
                    )
                    .Distinct()
                    .OrderBy(x =>
                        x
                    )
                    .ToList();

            var detectionCategoryProjectIds =
                request.Items
                    .Where(x =>
                    {
                        var classified =
                            Classify(
                                new OneDSavedResultItem
                                {
                                    Chapter =
                                        x.Chapter,

                                    Name =
                                        x.Name,

                                    Unit =
                                        x.Unit,

                                    Quantity =
                                        x.Quantity
                                }
                            );

                        return
                            classified
                                .ClassificationStatus ==
                                "Classified" &&
                            classified
                                .MajorCategory ==
                                target.MajorCategory &&
                            classified
                                .DetectionCategory ==
                                target.DetectionCategory;
                    })
                    .Select(x =>
                        x.ProjectId
                    )
                    .Where(x =>
                        x > 0
                    )
                    .Distinct()
                    .OrderBy(x =>
                        x
                    )
                    .ToList();

            bool specificationHasEnoughSamples =
                specificationProjectIds.Count >=
                minimumSampleCount;

            bool detectionCategoryHasEnoughSamples =
                detectionCategoryProjectIds.Count >=
                minimumSampleCount;

            string recommendation =
                !specificationHasEnoughSamples &&
                detectionCategoryHasEnoughSamples
                    ? $"規格層級僅有 {specificationProjectIds.Count} 個有效建案，低於最低樣本數 {minimumSampleCount}；改以「{target.DetectionCategory}」層級可取得 {detectionCategoryProjectIds.Count} 個建案，較適合作為目前異常偵測基準。"
                    : specificationHasEnoughSamples
                        ? $"目前「{request.TargetItemName}」已有 {specificationProjectIds.Count} 個有效建案，可進一步評估是否下切至規格層級。"
                        : $"規格層級與「{target.DetectionCategory}」層級目前都未達最低樣本數 {minimumSampleCount}，應標示資料不足，不強制判定異常。";

            return ServiceResult.Ok(
                new GranularityPreviewResponse
                {
                    TargetItemName =
                        request.TargetItemName,

                    MajorCategory =
                        target.MajorCategory,

                    DetectionCategory =
                        target.DetectionCategory,

                    MinimumSampleCount =
                        minimumSampleCount,

                    SpecificationProjectCount =
                        specificationProjectIds.Count,

                    DetectionCategoryProjectCount =
                        detectionCategoryProjectIds.Count,

                    SpecificationHasEnoughSamples =
                        specificationHasEnoughSamples,

                    DetectionCategoryHasEnoughSamples =
                        detectionCategoryHasEnoughSamples,

                    SpecificationProjectIds =
                        specificationProjectIds,

                    DetectionCategoryProjectIds =
                        detectionCategoryProjectIds,

                    Recommendation =
                        recommendation
                }
            );
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
