using Microsoft.EntityFrameworkCore;
using 崴正.Models;
using 崴正.Data;

namespace 崴正.Services
{
    public class StandardMaterialService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public StandardMaterialService(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;

            _auditLogService = auditLogService;
        }

        // ==========================================
        // 新增標準材料
        // ==========================================
        public ServiceResult CreateMaterial(
            string materialName,
            int categoryId,
            int unitId,
            string actionUser)
        {
            materialName =
                NormalizeText(
                    materialName
                );


            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return ServiceResult.Validation(
                    "請輸入標準材料名稱"
                );
            }


            var category =
                _context.MaterialCategories
                    .FirstOrDefault(x =>
                        x.CategoryId ==
                            categoryId
                    );


            if (category == null)
            {
                return ServiceResult.Validation(
                    "找不到指定的材料分類"
                );
            }


            if (!category.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的材料分類目前已停用"
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
                return ServiceResult.Validation(
                    "找不到指定的標準單位"
                );
            }


            if (!unit.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的標準單位目前已停用"
                );
            }


            bool exists =
                _context.MaterialMasters.Any(x =>
                    EF.Functions.ILike(
                        x.MaterialName,
                        materialName
                    )
                    &&
                    x.CategoryId ==
                        categoryId
                    &&
                    x.UnitId ==
                        unitId
                );


            if (exists)
            {
                return ServiceResult.Conflict(
                    "相同名稱、分類與單位的標準材料已經存在"
                );
            }


            using var transaction =
                _context.Database.BeginTransaction();


            try
            {
                var material =
                    new MaterialMaster
                    {
                        MaterialName =
                            materialName,

                        CategoryId =
                            category.CategoryId,

                        UnitId =
                            unit.UnitId,

                        IsActive =
                            true
                    };


                _context.MaterialMasters.Add(
                    material
                );


                // 先取得 DB 產生的 MaterialId
                _context.SaveChanges();


                _auditLogService.AddLog(
                    module:
                        "標準材料",

                    actionType:
                        "CreateMaterial",

                    entityType:
                        "MaterialMaster",

                    entityId:
                        material.MaterialId,

                    description:
                        $"新增標準材料「{material.MaterialName}」",

                    actionUser:
                        actionUser,

                    newValue:
                        new
                        {
                            materialId =
                                material.MaterialId,

                            materialName =
                                material.MaterialName,

                            categoryId =
                                material.CategoryId,

                            categoryName =
                                category.CategoryName,

                            unitId =
                                material.UnitId,

                            unitName =
                                unit.UnitName,

                            isActive =
                                material.IsActive
                        }
                );


                _context.SaveChanges();

                transaction.Commit();


                return ServiceResult.Ok(new
                {
                    message =
                        $"已新增標準材料「{material.MaterialName}」",

                    materialId =
                        material.MaterialId,

                    materialName =
                        material.MaterialName,

                    categoryId =
                        material.CategoryId,

                    categoryName =
                        category.CategoryName,

                    unitId =
                        material.UnitId,

                    unitName =
                        unit.UnitName,

                    isActive =
                        material.IsActive
                });
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        // ==========================================
        // 查詢標準材料
        // ==========================================
        public ServiceResult GetMaterials(
            string? search,
            int? categoryId,
            int? unitId,
            bool? isActive)
        {
            var query =
                _context.MaterialMasters
                    .AsNoTracking()
                    .AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
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
                    EF.Functions.ILike(
                        x.MaterialCategory.CategoryName,
                        pattern
                    )
                    )
                );
            }


            if (categoryId.HasValue)
            {
                query = query.Where(x =>
                    x.CategoryId ==
                    categoryId.Value
                );
            }


            if (unitId.HasValue)
            {
                query = query.Where(x =>
                    x.UnitId ==
                    unitId.Value
                );
            }


            if (isActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive ==
                    isActive.Value
                );
            }


            var materials =
                query
                    .OrderBy(x =>
                        x.MaterialCategory.SortOrder
                    )
                    .ThenBy(x =>
                        x.MaterialName
                    )
                    .Select(x => new
                    {
                        materialId =
                            x.MaterialId,

                        materialName =
                            x.MaterialName,

                        categoryId =
                            x.CategoryId,

                        categoryName =
                            x.MaterialCategory.CategoryName,

                        unitId =
                            x.UnitId,

                        unitName =
                            x.UnitMaster.UnitName,

                        isActive =
                            x.IsActive,

                        dictionaryCount =
                            _context.ItemDictionaries.Count(d =>
                                d.Material_Id ==
                                x.MaterialId
                            ),

                        historicalCount =
                            _context.HistoricalTransactions.Count(h =>
                                h.MaterialId ==
                                x.MaterialId &&
                                !h.IsDeleted
                            ),

                        finalBidItemCount =
                            _context.FinalBidItems.Count(f =>
                                f.MaterialId ==
                                x.MaterialId
                            )
                    })
                    .ToList();


            return ServiceResult.Ok(
                materials
            );
        }


        // ==========================================
        // 標準材料篩選選項
        // ==========================================
        public ServiceResult GetFilterOptions()
        {
            var categories =
                _context.MaterialCategories
                    .AsNoTracking()
                    .OrderBy(x =>
                        x.SortOrder)
                    .ThenBy(x =>
                        x.CategoryName)
                    .Select(x => new
                    {
                        categoryId =
                            x.CategoryId,

                        categoryName =
                            x.CategoryName,

                        isActive =
                            x.IsActive
                    })
                    .ToList();


            var units =
                _context.UnitMasters
                    .AsNoTracking()
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

                        isActive =
                            x.IsActive
                    })
                    .ToList();


            return ServiceResult.Ok(new
            {
                categories,
                units
            });
        }


        // ==========================================
        // 修改標準材料
        // ==========================================
        public ServiceResult UpdateMaterial(
            int materialId,
            string materialName,
            int categoryId,
            int unitId,
            string actionUser)
        {
            materialName =
                NormalizeText(materialName);

            var unit =
                _context.UnitMasters
                    .FirstOrDefault(x =>
                        x.UnitId ==
                        unitId
                    );

            if (unit == null)
            {
                return ServiceResult.Validation(
                    "找不到指定的標準單位"
                );
            }


            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return ServiceResult.Validation(
                    "請輸入標準材料名稱"
                );
            }


            var material =
                _context.MaterialMasters
                    .FirstOrDefault(x =>
                        x.MaterialId ==
                        materialId
                    );


            if (material == null)
            {
                return ServiceResult.NotFound(
                    "找不到標準材料"
                );
            }

            var oldValue = new
            {
                materialName =
                    material.MaterialName,

                categoryId =
                    material.CategoryId,

                unitId =
                    material.UnitId,

                isActive =
                    material.IsActive
            };


            var category =
                _context.MaterialCategories
                    .FirstOrDefault(x =>
                        x.CategoryId ==
                        categoryId
                    );


            if (category == null)
            {
                return ServiceResult.Validation(
                    "找不到指定的材料分類"
                );
            }


            if (!category.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的材料分類目前已停用"
                );
            }


            bool duplicate =
                _context.MaterialMasters.Any(x =>
                    x.MaterialId !=
                        materialId
                    &&
                    EF.Functions.ILike(
                        x.MaterialName,
                        materialName
                    )
                    &&
                    x.CategoryId ==
                        categoryId
                    &&
                    x.UnitId == unitId
                );


            if (duplicate)
            {
                return ServiceResult.Conflict(
                    "相同名稱、分類與單位的標準材料已經存在"
                );
            }


            material.MaterialName =
                materialName;

            material.CategoryId =
                category.CategoryId;


            material.UnitId =
                unit.UnitId;


            _auditLogService.AddLog(
                module:
                    "標準材料",

                actionType:
                    "UpdateMaterial",

                entityType:
                    "MaterialMaster",

                entityId:
                    material.MaterialId,

                description:
                    $"修改標準材料「{material.MaterialName}」",

                actionUser:
                    actionUser,

                oldValue:
                    oldValue,

                newValue:
                    new
                    {
                        materialName =
                            material.MaterialName,

                        categoryId =
                            material.CategoryId,

                        categoryName =
                            category.CategoryName,

                        unitId =
                            material.UnitId,

                        unitName =
                            unit.UnitName,

                        isActive =
                            material.IsActive
                    }
            );


            _context.SaveChanges();


            return ServiceResult.Ok(new
            {
                message =
                    $"已更新標準材料「{material.MaterialName}」",

                materialId =
                    material.MaterialId,

                materialName =
                    material.MaterialName,

                categoryId =
                    material.CategoryId,

                categoryName =
                    category.CategoryName,

                unitId =
                    unit.UnitId,

                unitName =
                    unit.UnitName,

                isActive =
                    material.IsActive
            });
        }


        // ==========================================
        // 啟用 / 停用標準材料
        // ==========================================
        public ServiceResult SetMaterialActive(
            int materialId,
            bool isActive,
            string actionUser)
        {
            var material =
                _context.MaterialMasters
                    .FirstOrDefault(x =>
                        x.MaterialId ==
                        materialId
                    );


            if (material == null)
            {
                return ServiceResult.NotFound(
                    "找不到標準材料"
                );
            }

            bool oldIsActive = 
                material.IsActive;


            material.IsActive =
                isActive;


            _auditLogService.AddLog(
                module:
                    "標準材料",

                actionType:
                    isActive
                        ? "EnableMaterial"
                        : "DisableMaterial",

                entityType:
                    "MaterialMaster",

                entityId:
                    material.MaterialId,

                description:
                    isActive
                        ? $"啟用標準材料「{material.MaterialName}」"
                        : $"停用標準材料「{material.MaterialName}」",

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
                            material.IsActive
                    }
            );


            _context.SaveChanges();


            return ServiceResult.Ok(new
            {
                message =
                    isActive
                        ? $"已啟用標準材料「{material.MaterialName}」"
                        : $"已停用標準材料「{material.MaterialName}」",

                materialId =
                    material.MaterialId,

                materialName =
                    material.MaterialName,

                isActive =
                    material.IsActive
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