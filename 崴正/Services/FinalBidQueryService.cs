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
                        var candidates = query
                            .Where(x => x.MaterialId.HasValue)
                            .Select(x => new
                            {
                                x.FinalBidItemId,
                                x.Unit,

                                MaterialUnit =
                                    x.Material != null
                                        ? x.Material.BaseUnit
                                        : null
                            })
                            .ToList();

                        var unitMismatchIds = candidates
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.MaterialUnit
                                ) &&
                                !_unitConversionService.CanConvert(
                                    x.Unit,
                                    x.MaterialUnit
                                )
                            )
                            .Select(x =>
                                x.FinalBidItemId
                            )
                            .ToList();

                        query = query.Where(x =>
                            unitMismatchIds.Contains(
                                x.FinalBidItemId
                            )
                        );

                        break;
                    }

                    default:
                        return ServiceResult.Validation(
                            "不支援的待處理原因"
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
                    x.Material.Category != null &&
                    x.Material.Category ==
                        selectedCategory);
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

                    sourceFileName =
                        x.SourceFileName,

                    sheetName =
                        x.SheetName,

                    rowNumber =
                        x.RowNumber,

                    itemCode =
                        x.ItemCode,

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

                    materialId =
                        x.MaterialId,

                    materialName =
                        x.Material != null
                            ? x.Material.MaterialName
                            : null,

                    materialCategory =
                        x.Material != null
                            ? x.Material.Category
                            : null,

                    materialUnit =
                        x.Material != null
                            ? x.Material.BaseUnit
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
                .Select(x => new
                {
                    x.finalBidItemId,
                    x.projectId,
                    x.projectName,
                    x.sourceFileName,
                    x.sheetName,
                    x.rowNumber,
                    x.itemCode,
                    x.itemName,
                    x.spec,
                    x.unit,
                    x.qty,
                    x.unitPrice,
                    x.materialId,
                    x.materialName,
                    x.materialCategory,
                    x.isIgnored,
                    x.materialUnit,
                    x.mappingStatus,
                    x.mappingIssueCode,
                    x.mappingIssueMessage,
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

                    unitMismatch =
                        x.materialId.HasValue &&
                        !string.IsNullOrWhiteSpace(
                            x.materialUnit
                        ) &&
                        !_unitConversionService.CanConvert(
                            x.unit,
                            x.materialUnit
                        )
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

            var unitCandidates =
                activeQuery
                    .Where(x =>
                        x.MaterialId.HasValue)
                    .Select(x => new
                    {
                        x.Unit,

                        MaterialUnit =
                            x.Material != null
                                ? x.Material.BaseUnit
                                : null
                    })
                    .ToList();

                    int unitMismatchCount =
                        unitCandidates.Count(x =>
                            !string.IsNullOrWhiteSpace(
                                x.MaterialUnit
                            ) &&
                            !_unitConversionService.CanConvert(
                                x.Unit,
                                x.MaterialUnit
                            )
                        );

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
                _context.MaterialMasters
                    .AsNoTracking()
                    .Where(x =>
                        x.Category != null &&
                        x.Category != "")
                    .Select(x =>
                        x.Category!)
                    .Distinct()
                    .OrderBy(x => x)
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
                    (
                        x.Category != null &&
                        EF.Functions.ILike(
                            x.Category,
                            pattern
                        )
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

                        category =
                            x.Category,

                        baseUnit =
                            x.BaseUnit
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
