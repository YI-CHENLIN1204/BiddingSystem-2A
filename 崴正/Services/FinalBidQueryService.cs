using Microsoft.EntityFrameworkCore;
using 崴正.Data;

namespace 崴正.Services
{
    public class FinalBidQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitConversionService _unitConversionService;

        public FinalBidQueryService(
            ApplicationDbContext context,
            UnitConversionService unitConversionService)
        {
            _context = context;
            _unitConversionService = unitConversionService;
        }

        public ServiceResult GetMappingItems(
            int? projectId,
            string status,
            string? search,
            string? issueCode,
            string? majorCategory,
            string? category,
            string? unit,
            int page,
            int pageSize)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 50;
            }

            if (pageSize > 200)
            {
                pageSize = 200;
            }

            var query = _context.FinalBidItems
                .AsNoTracking()
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(x =>
                    x.ProjectMasterId == projectId.Value);
            }

            status = string.IsNullOrWhiteSpace(status)
                ? "Pending"
                : status.Trim();

            if (!IsAllowedValue(
                    status,
                    "Pending",
                    "Auto",
                    "Manual",
                    "Ignored",
                    "All"))
            {
                return ServiceResult.Validation(
                    "狀態只允許 Pending、Auto、Manual、Ignored、All"
                );
            }


            if (string.Equals(
                    status,
                    "Ignored",
                    StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.IsIgnored);
            }
            else if (!string.Equals(
                        status,
                        "All",
                        StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    !x.IsIgnored &&
                    x.MappingStatus == status);
            }

            SafeMaterialDictionary? dictionary = null;
            SafeMaterialDictionary Dictionary() => dictionary ??= new SafeMaterialDictionary(SafeMaterialDictionary.Read(_context));

            if (!string.IsNullOrWhiteSpace(issueCode) &&
                !string.Equals(
                    issueCode,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                switch (issueCode)
                {
                    case "INVALID_QUANTITY":
                        query = query.Where(x =>
                            !x.Qty.HasValue ||
                            x.Qty.Value <= 0);
                        break;

                    case "INVALID_UNIT_PRICE":
                        query = query.Where(x =>
                            !x.UnitPrice.HasValue ||
                            x.UnitPrice.Value <= 0);
                        break;

                    case "UNIT_MISMATCH":
                    {
                        var ids = UnitMismatches(query);
                        query = query.Where(x => ids.Contains(x.FinalBidItemId));
                        break;
                    }
                    case "DRAFT_AUTO_REVIEW":
                    {
                        var matcher = Dictionary();
                        var ids = query.Where(x => !x.IsIgnored && x.MappingStatus == "Auto" &&
                                x.ProjectMaster.HistoryStatus == "Draft")
                            .Select(x => new { x.FinalBidItemId, x.ItemName, x.Spec, x.Unit, x.Qty, x.MaterialId })
                            .ToList()
                            .Where(x => matcher.Decide(x.ItemName, x.Spec, x.Unit, x.Qty,
                                _unitConversionService.CanConvert).Match?.MaterialId != x.MaterialId)
                            .Select(x => x.FinalBidItemId).ToArray();
                        query = query.Where(x => ids.Contains(x.FinalBidItemId));
                        break;
                    }

                    default:
                        return ServiceResult.Validation(
                            "不支援的待處理原因"
                        );
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    majorCategory) &&
                !string.Equals(
                    majorCategory,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedMajorCategory =
                    majorCategory.Trim();

                if (string.Equals(
                        selectedMajorCategory,
                        "Unclassified",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var knownMajorCategories =
                        TwoAScopePolicy
                            .GetKnownMajorCategories()
                            .ToArray();

                    query = query.Where(x =>
                        (
                            x.MajorCategoryOverride == null ||
                            x.MajorCategoryOverride == ""
                        ) &&
                        (
                            x.Category1 == null ||
                            !knownMajorCategories.Contains(
                                x.Category1)
                        ) &&
                        (
                            x.Category2 == null ||
                            !knownMajorCategories.Contains(
                                x.Category2)
                        ) &&
                        (
                            x.Category3 == null ||
                            !knownMajorCategories.Contains(
                                x.Category3)
                        )
                    );
                }
                else
                {
                    if (!TwoAScopePolicy
                        .IsKnownMajorCategory(
                            selectedMajorCategory))
                    {
                        return ServiceResult.Validation(
                            "不支援的工程大項篩選條件"
                        );
                    }

                    query = query.Where(x =>
                        x.MajorCategoryOverride ==
                            selectedMajorCategory ||
                        (
                            (
                                x.MajorCategoryOverride == null ||
                                x.MajorCategoryOverride == ""
                            ) &&
                            (
                                x.Category1 ==
                                    selectedMajorCategory ||
                                x.Category2 ==
                                    selectedMajorCategory ||
                                x.Category3 ==
                                    selectedMajorCategory
                            )
                        )
                    );
                }
            }


            if (!string.IsNullOrWhiteSpace(category) &&
                !string.Equals(
                    category,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedCategory =
                    category.Trim();

                query = query.Where(x =>
                    x.Material != null &&
                    x.Material.MaterialCategory.CategoryName ==
                        selectedCategory
                );
            }

            if (!string.IsNullOrWhiteSpace(unit) &&
                !string.Equals(
                    unit,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
            {
                string selectedUnit =
                    unit.Trim();

                query = query.Where(x =>
                    x.Unit != null &&
                    x.Unit == selectedUnit);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern =
                    $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.ItemName,
                        pattern
                    )
                    ||
                    (
                        x.Spec != null &&
                        EF.Functions.ILike(
                            x.Spec,
                            pattern
                        )
                    )
                    ||
                    (
                        x.Material != null &&
                        EF.Functions.ILike(
                            x.Material.MaterialName,
                            pattern
                        )
                    )
                    ||
                    EF.Functions.ILike(
                        x.ProjectMaster.ProjectName,
                        pattern
                    )
                );
            }

            int totalCount =
                query.Count();

            var rawItems = query
                .OrderBy(x =>
                    x.MappingStatus == "Pending"
                        ? 0
                        : x.MappingStatus == "Manual"
                            ? 1
                            : 2
                )
                .ThenByDescending(x =>
                    x.FinalBidItemId
                )
                .Skip(
                    (page - 1) * pageSize
                )
                .Take(pageSize)
                .Select(x => new
                {
                    finalBidItemId =
                        x.FinalBidItemId,

                    projectId =
                        x.ProjectMasterId,

                    projectName =
                        x.ProjectMaster.ProjectName,

                    isDraft = x.ProjectMaster.HistoryStatus == "Draft",

                    sourceFileName =
                        x.SourceFileName,

                    sheetName =
                        x.SheetName,

                    rowNumber =
                        x.RowNumber,

                    itemCode =
                        x.ItemCode,

                    category1 =
                        x.Category1,

                    category2 =
                        x.Category2,

                    category3 =
                        x.Category3,

                    majorCategoryOverride =
                        x.MajorCategoryOverride,

                    itemName =
                        x.ItemName,

                    spec =
                        x.Spec,

                    unit =
                        x.Unit,

                    qty =
                        x.Qty,

                    unitPrice =
                        x.UnitPrice,

                    amount =
                        x.Amount,

                    materialId =
                        x.MaterialId,

                    materialName =
                        x.Material != null
                            ? x.Material.MaterialName
                            : null,

                    materialCategory =
                        x.Material != null
                            ? x.Material.MaterialCategory.CategoryName
                            : null,

                    materialUnitId =
                        x.Material != null
                            ? (int?)x.Material.UnitId
                            : null,

                    materialUnit =
                        x.Material != null
                            ? x.Material.UnitMaster.UnitName
                            : null,

                    mappingStatus =
                        x.MappingStatus,

                    isIgnored =
                        x.IsIgnored,

                    mappingIssueCode =
                        x.MappingIssueCode,

                    mappingIssueMessage =
                        x.MappingIssueMessage,

                    dataIssueCode =
                        x.DataIssueCode,

                    dataIssueMessage =
                        x.DataIssueMessage,

                    matchedKeyword =
                        x.MatchedKeyword,

                    updatedAt =
                        x.UpdatedAt
                })
                .ToList();

            var items = rawItems
                .Select(x =>
                {
                    var decision = x.isDraft && !x.isIgnored && x.mappingStatus == "Auto"
                        ? Dictionary().Decide(x.itemName, x.spec, x.unit, x.qty, _unitConversionService.CanConvert)
                        : null;
                    bool needsReview = decision is not null && decision.Match?.MaterialId != x.materialId;
                    return new
                    {
                    x.finalBidItemId,
                    x.projectId,
                    x.projectName,
                    x.sourceFileName,
                    x.sheetName,
                    x.rowNumber,
                    x.itemCode,
                    x.category1,
                    x.category2,
                    x.category3,
                    x.majorCategoryOverride,

                    effectiveMajorCategory =
                        TwoAScopePolicy
                            .ResolveEffectiveMajorCategory(
                                x.majorCategoryOverride,
                                x.category1,
                                x.category2,
                                x.category3
                            ),

                    contributesStructuralPrice =
                        !x.isIgnored &&
                        string.Equals(
                            TwoAScopePolicy
                                .ResolveEffectiveMajorCategory(
                                    x.majorCategoryOverride,
                                    x.category1,
                                    x.category2,
                                    x.category3
                                ),
                            "結構體工程",
                            StringComparison.OrdinalIgnoreCase
                        ) &&
                        (
                            (
                                x.amount.HasValue &&
                                x.amount.Value > 0
                            ) ||
                            (
                                x.qty.HasValue &&
                                x.qty.Value > 0 &&
                                x.unitPrice.HasValue &&
                                x.unitPrice.Value > 0
                            )
                        ),

                    contributesQuantityHistory =
                        !needsReview && !x.isIgnored &&
                        string.Equals(
                            TwoAScopePolicy
                                .ResolveEffectiveMajorCategory(
                                    x.majorCategoryOverride,
                                    x.category1,
                                    x.category2,
                                    x.category3
                                ),
                            "結構體工程",
                            StringComparison.OrdinalIgnoreCase
                        ) &&
                        (
                            string.Equals(
                                x.materialName,
                                "鋼筋",
                                StringComparison.OrdinalIgnoreCase
                            ) ||
                            string.Equals(
                                x.materialName,
                                "混凝土",
                                StringComparison.OrdinalIgnoreCase
                            )
                        ) &&
                        x.qty.HasValue &&
                        x.qty.Value > 0 &&
                        x.materialUnitId.HasValue &&
                        _unitConversionService.CanConvert(
                            x.unit,
                            x.materialUnitId.Value
                        ),

                    quantityHistoryCategory =
                        !needsReview && !x.isIgnored &&
                        string.Equals(
                            TwoAScopePolicy
                                .ResolveEffectiveMajorCategory(
                                    x.majorCategoryOverride,
                                    x.category1,
                                    x.category2,
                                    x.category3
                                ),
                            "結構體工程",
                            StringComparison.OrdinalIgnoreCase
                        ) &&
                        (
                            string.Equals(
                                x.materialName,
                                "鋼筋",
                                StringComparison.OrdinalIgnoreCase
                            ) ||
                            string.Equals(
                                x.materialName,
                                "混凝土",
                                StringComparison.OrdinalIgnoreCase
                            )
                        ) &&
                        x.qty.HasValue &&
                        x.qty.Value > 0 &&
                        x.materialUnitId.HasValue &&
                        _unitConversionService.CanConvert(
                            x.unit,
                            x.materialUnitId.Value
                        )
                            ? x.materialName
                            : null,

                    x.itemName,
                    x.spec,
                    x.unit,
                    x.qty,
                    x.unitPrice,
                    x.amount,
                    x.materialId,
                    x.materialName,
                    x.materialCategory,
                    x.isIgnored,
                    x.materialUnitId,
                    x.materialUnit,
                    x.mappingStatus,
                    x.mappingIssueCode,
                    mappingIssueMessage = needsReview
                        ? decision!.Message ?? "目前核定材料與舊自動對應不同，請逐筆覆核。"
                        : x.mappingIssueMessage,
                    draftAutoNeedsReview = needsReview,
                    reviewCandidateMaterialId = x.isDraft && !x.isIgnored &&
                        (needsReview || (x.mappingStatus == "Pending" &&
                            x.mappingIssueCode is "UNVERIFIED_SPEC" or "NO_DICTIONARY_MATCH" or "MAPPING_REVIEW_REQUIRED"))
                        ? Dictionary().SuggestForReview(x.itemName, x.spec, x.unit, x.qty,
                            _unitConversionService.CanConvert) : null,
                    x.dataIssueCode,
                    x.dataIssueMessage,
                    x.matchedKeyword,
                    x.updatedAt,

                    invalidQuantity =
                        !x.qty.HasValue ||
                        x.qty.Value <= 0,

                    invalidUnitPrice =
                        !x.unitPrice.HasValue ||
                        x.unitPrice.Value <= 0,

                    unitMismatch = HasUnitMismatch(x.mappingIssueCode, x.unit, x.materialUnitId)
                    };
                })
                .ToList();

            return ServiceResult.Ok(new
            {
                totalCount,
                page,
                pageSize,

                filters = new
                {
                    status,
                    search,
                    issueCode,
                    majorCategory,
                    category,
                    unit
                },

                items
            });
        }

        public ServiceResult GetMappingSummary(
            int? projectId)
        {
            var allQuery =
                _context.FinalBidItems
                    .AsNoTracking()
                    .AsQueryable();

            if (projectId.HasValue)
            {
                allQuery = allQuery.Where(x =>
                    x.ProjectMasterId ==
                    projectId.Value);
            }

            int totalCount =
                allQuery.Count();

            int ignoredCount =
                allQuery.Count(x =>
                    x.IsIgnored);

            var activeQuery =
                allQuery.Where(x =>
                    !x.IsIgnored);

            int autoCount =
                activeQuery.Count(x =>
                    x.MappingStatus == "Auto");

            int manualCount =
                activeQuery.Count(x =>
                    x.MappingStatus == "Manual");

            int pendingCount =
                activeQuery.Count(x =>
                    x.MappingStatus == "Pending");

            int invalidQuantityCount =
                activeQuery.Count(x =>
                    !x.Qty.HasValue ||
                    x.Qty.Value <= 0);

            int invalidUnitPriceCount =
                activeQuery.Count(x =>
                    !x.UnitPrice.HasValue ||
                    x.UnitPrice.Value <= 0);

            int unitMismatchCount = UnitMismatches(activeQuery).Length;

            return ServiceResult.Ok(new
            {
                totalCount,
                autoCount,
                manualCount,
                pendingCount,
                invalidQuantityCount,
                invalidUnitPriceCount,
                unitMismatchCount,
                ignoredCount
            });
        }

        private bool HasUnitMismatch(string? mappingIssueCode, string? unit, int? materialUnitId) =>
            mappingIssueCode == "MAPPING_UNIT_MISMATCH" ||
            (materialUnitId.HasValue && !_unitConversionService.CanConvert(unit, materialUnitId.Value));

        private long[] UnitMismatches(IQueryable<崴正.Models.FinalBidItem> query) =>
            query.Where(x => x.MappingIssueCode == "MAPPING_UNIT_MISMATCH" || x.MaterialId.HasValue)
                .Select(x => new { x.FinalBidItemId, x.MappingIssueCode, x.Unit,
                    MaterialUnitId = x.Material != null ? (int?)x.Material.UnitId : null })
                .ToList()
                .Where(x => HasUnitMismatch(x.MappingIssueCode, x.Unit, x.MaterialUnitId))
                .Select(x => x.FinalBidItemId).ToArray();

        public ServiceResult GetMappingFilterOptions(
            int? projectId)
        {
            var itemQuery =
                _context.FinalBidItems
                    .AsNoTracking()
                    .AsQueryable();

            if (projectId.HasValue)
            {
                itemQuery = itemQuery.Where(x =>
                    x.ProjectMasterId ==
                    projectId.Value);
            }

            var units =
                itemQuery
                    .Where(x =>
                        x.Unit != null &&
                        x.Unit != "")
                    .Select(x =>
                        x.Unit!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var categories =
                    _context.MaterialCategories
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive)
                        .OrderBy(x =>
                            x.SortOrder)
                        .ThenBy(x =>
                            x.CategoryName)
                        .Select(x =>
                            x.CategoryName)
                        .ToList();

            return ServiceResult.Ok(new
            {
                units,
                categories
            });
        }

        public ServiceResult GetMaterialOptions(
            string? search)
        {
            var query =
                _context.MaterialMasters
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                string pattern =
                    $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.MaterialName,
                        pattern
                    )
                    ||
                    EF.Functions.ILike(
                        x.MaterialCategory.CategoryName,
                        pattern
                    )
                );
            }

            var materials =
                query
                    .OrderBy(x =>
                        x.MaterialName)
                    .Select(x => new
                    {
                        materialId =
                            x.MaterialId,

                        materialName =
                            x.MaterialName,

                        categoryId =
                            x.CategoryId,

                        category =
                            x.MaterialCategory.CategoryName,

                        unitId =
                            x.UnitId,

                        unitName =
                            x.UnitMaster.UnitName
                    })
                    .Take(200)
                    .ToList();

            return ServiceResult.Ok(
                materials
            );
        }

        private static bool IsAllowedValue(
            string value,
            params string[] allowedValues)
        {
            return allowedValues.Any(x =>
                string.Equals(
                    x,
                    value,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }
    }

}
