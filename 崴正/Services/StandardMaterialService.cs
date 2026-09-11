using Microsoft.EntityFrameworkCore;
using 崴正.Data;

namespace 崴正.Services
{
    public class StandardMaterialService
    {
        private readonly ApplicationDbContext _context;
        private readonly MaterialMappingService _materialMappingService;

        public StandardMaterialService(
            ApplicationDbContext context,
            MaterialMappingService materialMappingService)
        {
            _context = context;

            _materialMappingService =
                materialMappingService;
        }

        // ==========================================
        // 新增標準材料
        // ==========================================
        public ServiceResult CreateMaterial(
            string materialName,
            int categoryId,
            string baseUnit)
        {
            var category =
                _context.MaterialCategories
                    .AsNoTracking()
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

            return _materialMappingService
                .CreateMaterial(
                    materialName,
                    category.CategoryName,
                    baseUnit
                );
        }


        // ==========================================
        // 查詢標準材料
        // ==========================================
        public ServiceResult GetMaterials(
            string? search,
            int? categoryId,
            string? baseUnit,
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


            if (!string.IsNullOrWhiteSpace(
                    baseUnit))
            {
                string selectedUnit =
                    baseUnit.Trim();

                query = query.Where(x =>
                    x.BaseUnit ==
                    selectedUnit
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

                        baseUnit =
                            x.BaseUnit,

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


            var baseUnits =
                _context.MaterialMasters
                    .AsNoTracking()
                    .Where(x =>
                        x.BaseUnit != "")
                    .Select(x =>
                        x.BaseUnit)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();


            return ServiceResult.Ok(new
            {
                categories,
                baseUnits
            });
        }


        // ==========================================
        // 修改標準材料
        // ==========================================
        public ServiceResult UpdateMaterial(
            int materialId,
            string materialName,
            int categoryId,
            string baseUnit)
        {
            materialName =
                NormalizeText(materialName);

            baseUnit =
                NormalizeText(baseUnit);


            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return ServiceResult.Validation(
                    "請輸入標準材料名稱"
                );
            }


            if (string.IsNullOrWhiteSpace(
                    baseUnit))
            {
                return ServiceResult.Validation(
                    "請選擇標準單位"
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
                    x.BaseUnit ==
                        baseUnit
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


            material.BaseUnit =
                baseUnit;


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

                baseUnit =
                    material.BaseUnit,

                isActive =
                    material.IsActive
            });
        }


        // ==========================================
        // 啟用 / 停用標準材料
        // ==========================================
        public ServiceResult SetMaterialActive(
            int materialId,
            bool isActive)
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


            material.IsActive =
                isActive;


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