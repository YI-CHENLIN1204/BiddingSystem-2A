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
