using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class UnitMasterService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public UnitMasterService(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        // ==========================================
        // 新增標準單位
        //
        // 可換算類別：
        //   toBaseFactor 必填
        //
        // 不可換算類別：
        //   toBaseFactor 不使用，存 NULL
        // ==========================================
        public ServiceResult CreateUnit(
            string unitName,
            int unitCategoryId,
            decimal? toBaseFactor,
            string actionUser)
        {
            unitName =
                NormalizeText(unitName);


            if (string.IsNullOrWhiteSpace(
                    unitName))
            {
                return ServiceResult.Validation(
                    "請輸入單位名稱"
                );
            }


            var category =
                _context.UnitCategories
                    .AsNoTracking()
                    .FirstOrDefault(x =>
                        x.UnitCategoryId ==
                            unitCategoryId
                    );


            if (category == null)
            {
                return ServiceResult.Validation(
                    "找不到指定的單位類別"
                );
            }


            if (!category.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的單位類別目前已停用"
                );
            }


            bool duplicate =
                _context.UnitMasters
                    .Any(x =>
                        EF.Functions.ILike(
                            x.UnitName,
                            unitName
                        )
                    );


            if (duplicate)
            {
                return ServiceResult.Conflict(
                    $"單位「{unitName}」已經存在"
                );
            }


            // ==========================================
            // 可換算類別一定要有倍率
            // ==========================================
            if (category.AllowConversion)
            {
                if (!toBaseFactor.HasValue ||
                    toBaseFactor.Value <= 0)
                {
                    return ServiceResult.Validation(
                        "此單位類別允許換算，請輸入相對基準單位的換算倍率"
                    );
                }
            }
            else
            {
                // 不可換算類別不使用倍率
                toBaseFactor = null;
            }


            int nextSortOrder =
                (_context.UnitMasters
                    .Where(x =>
                        x.UnitCategoryId ==
                            unitCategoryId)
                    .Select(x =>
                        (int?)x.SortOrder)
                    .Max() ?? 0) + 1;


            var unit =
                new UnitMaster
                {
                    UnitName =
                        unitName,

                    UnitCategoryId =
                        category.UnitCategoryId,

                    ToBaseFactor =
                        toBaseFactor,

                    IsBaseUnit =
                        false,

                    IsActive =
                        true,

                    SortOrder =
                        nextSortOrder
                };


            using var transaction =
                _context.Database
                    .BeginTransaction();

            try
            {
                _context.UnitMasters.Add(
                    unit
                );

                // 先取得 DB 產生的 UnitId
                _context.SaveChanges();


                _auditLogService.AddLog(
                    module:
                        "單位管理",

                    actionType:
                        "CreateUnit",

                    entityType:
                        "UnitMaster",

                    entityId:
                        unit.UnitId,

                    description:
                        $"新增單位「{unit.UnitName}」",

                    actionUser:
                        actionUser,

                    newValue:
                        new
                        {
                            unitId =
                                unit.UnitId,

                            unitName =
                                unit.UnitName,

                            unitCategoryId =
                                unit.UnitCategoryId,

                            categoryName =
                                category.CategoryName,

                            allowConversion =
                                category.AllowConversion,

                            toBaseFactor =
                                unit.ToBaseFactor,

                            isBaseUnit =
                                unit.IsBaseUnit,

                            isActive =
                                unit.IsActive,

                            sortOrder =
                                unit.SortOrder
                        }
                );


                _context.SaveChanges();

                transaction.Commit();


                return ServiceResult.Ok(new
                {
                    message =
                        $"已新增單位「{unit.UnitName}」",

                    unitId =
                        unit.UnitId,

                    unitName =
                        unit.UnitName,

                    unitCategoryId =
                        unit.UnitCategoryId,

                    categoryName =
                        category.CategoryName,

                    allowConversion =
                        category.AllowConversion,

                    toBaseFactor =
                        unit.ToBaseFactor,

                    isBaseUnit =
                        unit.IsBaseUnit,

                    isActive =
                        unit.IsActive
                });
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public ServiceResult GetUnits(
            string? search,
            int? unitCategoryId,
            bool? isActive)
        {
            var query =
                _context.UnitMasters
                    .AsNoTracking()
                    .AsQueryable();


            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                string pattern =
                    $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.UnitName,
                        pattern
                    )
                    ||
                    EF.Functions.ILike(
                        x.UnitCategory.CategoryName,
                        pattern
                    )
                );
            }


            if (unitCategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.UnitCategoryId ==
                    unitCategoryId.Value
                );
            }


            if (isActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive ==
                    isActive.Value
                );
            }


            var units =
                query
                    .OrderBy(x =>
                        x.UnitCategory.SortOrder)
                    .ThenBy(x =>
                        x.SortOrder)
                    .ThenBy(x =>
                        x.UnitName)
                    .Select(x => new
                    {
                        unitId =
                            x.UnitId,

                        unitName =
                            x.UnitName,

                        unitCategoryId =
                            x.UnitCategoryId,

                        categoryName =
                            x.UnitCategory.CategoryName,

                        allowConversion =
                            x.UnitCategory.AllowConversion,

                        toBaseFactor =
                            x.ToBaseFactor,

                        isBaseUnit =
                            x.IsBaseUnit,

                        isActive =
                            x.IsActive
                    })
                    .ToList();


            return ServiceResult.Ok(
                units
            );
        }

        public ServiceResult UpdateUnit(
            int unitId,
            string unitName,
            int unitCategoryId,
            decimal? toBaseFactor,
            string actionUser)
        {
            unitName =
                NormalizeText(
                    unitName
                );


            if (string.IsNullOrWhiteSpace(
                    unitName))
            {
                return ServiceResult.Validation(
                    "請輸入單位名稱"
                );
            }


            var unit =
                _context.UnitMasters
                    .FirstOrDefault(x =>
                        x.UnitId ==
                        unitId
                    );


            if (unit == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的單位"
                );
            }


            // 基準單位建立後固定
            if (unit.IsBaseUnit)
            {
                return ServiceResult.Validation(
                    "基準單位不可修改"
                );
            }


            var oldValue = new
            {
                unitName =
                    unit.UnitName,

                unitCategoryId =
                    unit.UnitCategoryId,

                toBaseFactor =
                    unit.ToBaseFactor,

                isBaseUnit =
                    unit.IsBaseUnit,

                isActive =
                    unit.IsActive
            };


            var category =
                _context.UnitCategories
                    .AsNoTracking()
                    .FirstOrDefault(x =>
                        x.UnitCategoryId ==
                        unitCategoryId
                    );


            if (category == null)
            {
                return ServiceResult.Validation(
                    "找不到指定的單位類別"
                );
            }


            if (!category.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的單位類別目前已停用"
                );
            }


            bool duplicate =
                _context.UnitMasters
                    .Any(x =>
                        x.UnitId !=
                            unitId
                        &&
                        EF.Functions.ILike(
                            x.UnitName,
                            unitName
                        )
                    );


            if (duplicate)
            {
                return ServiceResult.Conflict(
                    $"單位「{unitName}」已經存在"
                );
            }


            if (category.AllowConversion)
            {
                if (!toBaseFactor.HasValue ||
                    toBaseFactor.Value <= 0)
                {
                    return ServiceResult.Validation(
                        "此單位類別允許換算，請輸入相對基準單位的換算倍率"
                    );
                }
            }
            else
            {
                toBaseFactor =
                    null;
            }


            unit.UnitName =
                unitName;

            unit.UnitCategoryId =
                category.UnitCategoryId;

            unit.ToBaseFactor =
                toBaseFactor;


            _auditLogService.AddLog(
                module:
                    "單位管理",

                actionType:
                    "UpdateUnit",

                entityType:
                    "UnitMaster",

                entityId:
                    unit.UnitId,

                description:
                    $"修改單位「{unit.UnitName}」",

                actionUser:
                    actionUser,

                oldValue:
                    oldValue,

                newValue:
                    new
                    {
                        unitName =
                            unit.UnitName,

                        unitCategoryId =
                            unit.UnitCategoryId,

                        categoryName =
                            category.CategoryName,

                        allowConversion =
                            category.AllowConversion,

                        toBaseFactor =
                            unit.ToBaseFactor,

                        isBaseUnit =
                            unit.IsBaseUnit,

                        isActive =
                            unit.IsActive
                    }
            );


            _context.SaveChanges();


            return ServiceResult.Ok(new
            {
                message =
                    $"已更新單位「{unit.UnitName}」",

                unitId =
                    unit.UnitId,

                unitName =
                    unit.UnitName,

                unitCategoryId =
                    unit.UnitCategoryId,

                categoryName =
                    category.CategoryName,

                allowConversion =
                    category.AllowConversion,

                toBaseFactor =
                    unit.ToBaseFactor,

                isBaseUnit =
                    unit.IsBaseUnit,

                isActive =
                    unit.IsActive
            });
        }

        public ServiceResult SetUnitActive(
            int unitId,
            bool isActive,
            string actionUser)
        {
            var unit =
                _context.UnitMasters
                    .FirstOrDefault(x =>
                        x.UnitId ==
                        unitId
                    );


            if (unit == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的單位"
                );
            }


            if (unit.IsBaseUnit &&
                !isActive)
            {
                return ServiceResult.Validation(
                    "基準單位不可停用"
                );
            }


            bool oldIsActive =
                unit.IsActive;


            unit.IsActive =
                isActive;


            _auditLogService.AddLog(
                module:
                    "單位管理",

                actionType:
                    isActive
                        ? "EnableUnit"
                        : "DisableUnit",

                entityType:
                    "UnitMaster",

                entityId:
                    unit.UnitId,

                description:
                    isActive
                        ? $"啟用單位「{unit.UnitName}」"
                        : $"停用單位「{unit.UnitName}」",

                actionUser:
                    actionUser,

                oldValue:
                    new
                    {
                        isActive =
                            oldIsActive
                    },

                newValue:
                    new
                    {
                        isActive =
                            unit.IsActive
                    }
            );


            _context.SaveChanges();


            return ServiceResult.Ok(new
            {
                message =
                    isActive
                        ? $"已啟用單位「{unit.UnitName}」"
                        : $"已停用單位「{unit.UnitName}」",

                unitId =
                    unit.UnitId,

                unitName =
                    unit.UnitName,

                isActive =
                    unit.IsActive
            });
        }


        private static string NormalizeText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? string.Empty
                : value.Trim();
        }
    }
}
