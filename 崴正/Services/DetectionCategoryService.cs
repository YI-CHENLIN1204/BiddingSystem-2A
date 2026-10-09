using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class DetectionCategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitConversionService _unitConversionService;
        private readonly AuditLogService _auditLogService;

        public DetectionCategoryService(
            ApplicationDbContext context,
            UnitConversionService unitConversionService,
            AuditLogService auditLogService)
        {
            _context = context;
            _unitConversionService = unitConversionService;
            _auditLogService = auditLogService;
        }


        public ServiceResult GetCategories()
        {
            var categories =
                _context.DetectionCategories
                    .AsNoTracking()
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.MajorCategoryName)
                    .ThenBy(x => x.DetectionCategoryName)
                    .Select(x => new
                    {
                        detectionCategoryId = x.DetectionCategoryId,
                        majorCategoryName = x.MajorCategoryName,
                        detectionCategoryName = x.DetectionCategoryName,
                        unitId = x.UnitId,
                        unitName = x.UnitMaster.UnitName,
                        isActive = x.IsActive,
                        sortOrder = x.SortOrder,
                        materialCount = x.MaterialMappings.Count(m => m.IsActive)
                    })
                    .ToList();

            return ServiceResult.Ok(categories);
        }


        public ServiceResult CreateCategory(
            string majorCategoryName,
            string detectionCategoryName,
            int unitId,
            string actionUser)
        {
            majorCategoryName =
                NormalizeText(majorCategoryName);

            detectionCategoryName =
                NormalizeText(detectionCategoryName);

            if (string.IsNullOrWhiteSpace(majorCategoryName))
            {
                return ServiceResult.Validation(
                    "請輸入 1D 工程大項名稱");
            }

            if (string.IsNullOrWhiteSpace(detectionCategoryName))
            {
                return ServiceResult.Validation(
                    "請輸入 2A 偵錯細項名稱");
            }

            var unit =
                _context.UnitMasters
                    .AsNoTracking()
                    .FirstOrDefault(x =>
                        x.UnitId == unitId);

            if (unit == null)
            {
                return ServiceResult.Validation(
                    "找不到指定的標準單位");
            }

            if (!unit.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的標準單位目前已停用");
            }

            bool duplicate =
                _context.DetectionCategories
                    .Any(x =>
                        EF.Functions.ILike(
                            x.MajorCategoryName,
                            majorCategoryName)
                        &&
                        EF.Functions.ILike(
                            x.DetectionCategoryName,
                            detectionCategoryName));

            if (duplicate)
            {
                return ServiceResult.Conflict(
                    "相同工程大項與偵錯細項已存在");
            }

            int nextSortOrder =
                _context.DetectionCategories
                    .Select(x => (int?)x.SortOrder)
                    .Max() ?? 0;

            var category =
                new DetectionCategory
                {
                    MajorCategoryName =
                        majorCategoryName,
                    DetectionCategoryName =
                        detectionCategoryName,
                    UnitId = unitId,
                    IsActive = true,
                    SortOrder = nextSortOrder + 1
                };

            _context.DetectionCategories.Add(category);
            _context.SaveChanges();

            _auditLogService.AddLog(
                module: "2A偵錯細項",
                actionType: "CreateDetectionCategory",
                entityType: "DetectionCategory",
                entityId: category.DetectionCategoryId,
                description:
                    $"新增偵錯細項「{majorCategoryName} / {detectionCategoryName}」",
                actionUser: actionUser,
                newValue: new
                {
                    category.DetectionCategoryId,
                    category.MajorCategoryName,
                    category.DetectionCategoryName,
                    category.UnitId,
                    unit.UnitName,
                    category.IsActive,
                    category.SortOrder
                });

            _context.SaveChanges();

            return ServiceResult.Ok(new
            {
                message = "偵錯細項已建立",
                detectionCategoryId =
                    category.DetectionCategoryId
            });
        }


        public ServiceResult GetMappings(
            int? detectionCategoryId = null)
        {
            var query =
                _context.DetectionCategoryMaterialMaps
                    .AsNoTracking()
                    .AsQueryable();

            if (detectionCategoryId.HasValue)
            {
                query = query.Where(x =>
                    x.DetectionCategoryId ==
                    detectionCategoryId.Value);
            }

            var mappings =
                query
                    .OrderBy(x => x.DetectionCategory.SortOrder)
                    .ThenBy(x => x.Material.MaterialName)
                    .Select(x => new
                    {
                        detectionCategoryId = x.DetectionCategoryId,
                        detectionCategoryName =
                            x.DetectionCategory.DetectionCategoryName,
                        majorCategoryName =
                            x.DetectionCategory.MajorCategoryName,
                        materialId = x.MaterialId,
                        materialName = x.Material.MaterialName,
                        materialUnitId = x.Material.UnitId,
                        materialUnitName = x.Material.UnitMaster.UnitName,
                        detectionUnitId = x.DetectionCategory.UnitId,
                        detectionUnitName =
                            x.DetectionCategory.UnitMaster.UnitName,
                        isActive = x.IsActive
                    })
                    .ToList();

            return ServiceResult.Ok(mappings);
        }
    }
}
