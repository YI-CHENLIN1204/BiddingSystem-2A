using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class MaterialCategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public MaterialCategoryService(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        // ==========================================
        // 取得所有材料分類
        // ==========================================
        public ServiceResult GetCategories()
        {
            var categories =
                _context.MaterialCategories
                    .AsNoTracking()
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.CategoryName)
                    .Select(x => new
                    {
                        categoryId =
                            x.CategoryId,

                        categoryName =
                            x.CategoryName,

                        isActive =
                            x.IsActive,

                        sortOrder =
                            x.SortOrder,

                        materialCount =
                            x.Materials.Count()
                    })
                    .ToList();

            return ServiceResult.Ok(
                categories
            );
        }


        // ==========================================
        // 新增材料分類
        // ==========================================
        public ServiceResult CreateCategory(
            string categoryName,
            string actionUser)
        {
            categoryName =
                NormalizeText(categoryName);

            if (string.IsNullOrWhiteSpace(
                    categoryName))
            {
                return ServiceResult.Validation(
                    "請輸入材料分類名稱"
                );
            }

            bool exists =
                _context.MaterialCategories
                    .Any(x =>
                        EF.Functions.ILike(
                            x.CategoryName,
                            categoryName
                        )
                    );

            if (exists)
            {
                return ServiceResult.Conflict(
                    "相同名稱的材料分類已經存在"
                );
            }

            int nextSortOrder =
                _context.MaterialCategories
                    .Select(x =>
                        (int?)x.SortOrder)
                    .Max() ?? 0;

            using var transaction =
                _context.Database
                    .BeginTransaction();

            try
            {
                var category =
                    new MaterialCategory
                    {
                        CategoryName =
                            categoryName,

                        IsActive =
                            true,

                        SortOrder =
                            nextSortOrder + 1
                    };

                _context.MaterialCategories.Add(
                    category
                );

                // 先取得資料庫產生的 CategoryId
                _context.SaveChanges();

                _auditLogService.AddLog(
                    module:
                        "材料分類",

                    actionType:
                        "CreateCategory",

                    entityType:
                        "MaterialCategory",

                    entityId:
                        category.CategoryId,

                    description:
                        $"新增材料分類「{category.CategoryName}」",

                    actionUser:
                        actionUser,

                    newValue:
                        new
                        {
                            categoryId =
                                category.CategoryId,

                            categoryName =
                                category.CategoryName,

                            isActive =
                                category.IsActive,

                            sortOrder =
                                category.SortOrder
                        }
                );

                _context.SaveChanges();

                transaction.Commit();

                return ServiceResult.Ok(new
                {
                    message =
                        $"已新增材料分類「{category.CategoryName}」",

                    categoryId =
                        category.CategoryId,

                    categoryName =
                        category.CategoryName,

                    isActive =
                        category.IsActive,

                    sortOrder =
                        category.SortOrder
                });
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // ==========================================
        // 修改材料分類名稱
        // ==========================================
        public ServiceResult UpdateCategory(
            int categoryId,
            string categoryName,
            string actionUser)
        {
            categoryName =
                NormalizeText(categoryName);

            if (string.IsNullOrWhiteSpace(
                    categoryName))
            {
                return ServiceResult.Validation(
                    "請輸入材料分類名稱"
                );
            }

            var category =
                _context.MaterialCategories
                    .FirstOrDefault(x =>
                        x.CategoryId ==
                        categoryId);

            if (category == null)
            {
                return ServiceResult.NotFound(
                    "找不到材料分類"
                );
            }

            bool duplicate =
                _context.MaterialCategories
                    .Any(x =>
                        x.CategoryId !=
                            categoryId
                        &&
                        EF.Functions.ILike(
                            x.CategoryName,
                            categoryName
                        )
                    );

            if (duplicate)
            {
                return ServiceResult.Conflict(
                    "相同名稱的材料分類已經存在"
                );
            }

            var oldValue = new
            {
                categoryName =
                    category.CategoryName,

                isActive =
                    category.IsActive,

                sortOrder =
                    category.SortOrder
            };

            using var transaction =
                _context.Database
                    .BeginTransaction();

            try
            {
                category.CategoryName =
                    categoryName;

                _auditLogService.AddLog(
                    module:
                        "材料分類",

                    actionType:
                        "UpdateCategory",

                    entityType:
                        "MaterialCategory",

                    entityId:
                        category.CategoryId,

                    description:
                        $"修改材料分類名稱為「{category.CategoryName}」",

                    actionUser:
                        actionUser,

                    oldValue:
                        oldValue,

                    newValue:
                        new
                        {
                            categoryName =
                                category.CategoryName,

                            isActive =
                                category.IsActive,

                            sortOrder =
                                category.SortOrder
                        }
                );

                _context.SaveChanges();

                transaction.Commit();

                return ServiceResult.Ok(new
                {
                    message =
                        $"材料分類已修改為「{category.CategoryName}」",

                    categoryId =
                        category.CategoryId,

                    categoryName =
                        category.CategoryName
                });
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // ==========================================
        // 啟用 / 停用材料分類
        //
        // 不刪除分類，也不破壞既有材料關聯
        // ==========================================
        public ServiceResult SetCategoryActive(
            int categoryId,
            bool isActive,
            string actionUser)
        {
            var category =
                _context.MaterialCategories
                    .FirstOrDefault(x =>
                        x.CategoryId ==
                        categoryId);

            if (category == null)
            {
                return ServiceResult.NotFound(
                    "找不到材料分類"
                );
            }

            bool oldIsActive =
                category.IsActive;

            category.IsActive =
                isActive;

            _auditLogService.AddLog(
                module:
                    "材料分類",

                actionType:
                    isActive
                        ? "EnableCategory"
                        : "DisableCategory",

                entityType:
                    "MaterialCategory",

                entityId:
                    category.CategoryId,

                description:
                    isActive
                        ? $"啟用材料分類「{category.CategoryName}」"
                        : $"停用材料分類「{category.CategoryName}」",

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
                            category.IsActive
                    }
            );

            _context.SaveChanges();

            return ServiceResult.Ok(new
            {
                message =
                    isActive
                        ? $"已啟用材料分類「{category.CategoryName}」"
                        : $"已停用材料分類「{category.CategoryName}」",

                categoryId =
                    category.CategoryId,

                categoryName =
                    category.CategoryName,

                isActive =
                    category.IsActive
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