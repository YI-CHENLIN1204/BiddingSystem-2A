using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class UnitCategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public UnitCategoryService(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        // ==========================================
        // 取得可使用的單位類別
        // ==========================================
        public ServiceResult GetCategories()
        {
            var categories =
                _context.UnitCategories
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive)
                    .OrderBy(x =>
                        x.SortOrder)
                    .ThenBy(x =>
                        x.CategoryName)
                    .Select(x => new
                    {
                        unitCategoryId =
                            x.UnitCategoryId,

                        categoryName =
                            x.CategoryName,

                        allowConversion =
                            x.AllowConversion,

                        baseUnitName =
                            x.Units
                                .Where(u =>
                                    u.IsBaseUnit)
                                .Select(u =>
                                    u.UnitName)
                                .FirstOrDefault()
                    })
                    .ToList();

            return ServiceResult.Ok(
                categories
            );
        }


        // ==========================================
        // 新增單位類別
        //
        // 可換算：
        //   必須同時建立基準單位
        //
        // 不可換算：
        //   不建立基準單位
        // ==========================================
        public ServiceResult CreateCategory(
            string categoryName,
            bool allowConversion,
            string? baseUnitName,
            string actionUser)
        {
            categoryName =
                NormalizeText(
                    categoryName
                );

            baseUnitName =
                NormalizeText(
                    baseUnitName
                );


            if (string.IsNullOrWhiteSpace(
                    categoryName))
            {
                return ServiceResult.Validation(
                    "請輸入單位類別名稱"
                );
            }


            bool categoryExists =
                _context.UnitCategories
                    .Any(x =>
                        EF.Functions.ILike(
                            x.CategoryName,
                            categoryName
                        )
                    );

            if (categoryExists)
            {
                return ServiceResult.Conflict(
                    "此單位類別已經存在"
                );
            }


            if (allowConversion &&
                string.IsNullOrWhiteSpace(
                    baseUnitName))
            {
                return ServiceResult.Validation(
                    "允許單位換算的類別必須設定基準單位"
                );
            }


            if (allowConversion)
            {
                bool unitExists =
                    _context.UnitMasters
                        .Any(x =>
                            EF.Functions.ILike(
                                x.UnitName,
                                baseUnitName
                            )
                        );

                if (unitExists)
                {
                    return ServiceResult.Conflict(
                        $"單位「{baseUnitName}」已經存在"
                    );
                }
            }


            using var transaction =
                _context.Database
                    .BeginTransaction();

            try
            {
                int nextSortOrder =
                    (_context.UnitCategories
                        .Select(x =>
                            (int?)x.SortOrder)
                        .Max() ?? 0) + 1;


                var category =
                    new UnitCategory
                    {
                        CategoryName =
                            categoryName,

                        AllowConversion =
                            allowConversion,

                        IsActive =
                            true,

                        SortOrder =
                            nextSortOrder
                    };


                _context.UnitCategories.Add(
                    category
                );

                // 先取得 DB 產生的 UnitCategoryId
                _context.SaveChanges();


                UnitMaster? baseUnit =
                    null;


                if (allowConversion)
                {
                    baseUnit =
                        new UnitMaster
                        {
                            UnitName =
                                baseUnitName,

                            UnitCategoryId =
                                category.UnitCategoryId,

                            ToBaseFactor =
                                1m,

                            IsBaseUnit =
                                true,

                            IsActive =
                                true,

                            SortOrder =
                                1
                        };


                    _context.UnitMasters.Add(
                        baseUnit
                    );
                }


                _auditLogService.AddLog(
                    module:
                        "單位管理",

                    actionType:
                        "CreateUnitCategory",

                    entityType:
                        "UnitCategory",

                    entityId:
                        category.UnitCategoryId,

                    description:
                        allowConversion
                            ? $"新增單位類別「{category.CategoryName}」，基準單位為「{baseUnitName}」"
                            : $"新增單位類別「{category.CategoryName}」",

                    actionUser:
                        actionUser,

                    newValue:
                        new
                        {
                            unitCategoryId =
                                category.UnitCategoryId,

                            categoryName =
                                category.CategoryName,

                            allowConversion =
                                category.AllowConversion,

                            baseUnitName =
                                allowConversion
                                    ? baseUnitName
                                    : null,

                            isActive =
                                category.IsActive,

                            sortOrder =
                                category.SortOrder
                        }
                );


                // 同一次 SaveChanges 寫入基準單位（若有）與 Audit Log
                _context.SaveChanges();

                transaction.Commit();


                return ServiceResult.Ok(new
                {
                    message =
                        allowConversion
                            ? $"已新增單位類別「{category.CategoryName}」，基準單位為「{baseUnit!.UnitName}」"
                            : $"已新增單位類別「{category.CategoryName}」",

                    unitCategoryId =
                        category.UnitCategoryId,

                    categoryName =
                        category.CategoryName,

                    allowConversion =
                        category.AllowConversion,

                    baseUnitId =
                        baseUnit?.UnitId,

                    baseUnitName =
                        baseUnit?.UnitName
                });
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
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
