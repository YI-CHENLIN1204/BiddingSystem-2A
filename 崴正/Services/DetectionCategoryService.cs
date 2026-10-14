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


        public ServiceResult GetReadiness()
        {
            var categories =
                _context.DetectionCategories
                    .AsNoTracking()
                    .Include(x => x.UnitMaster)
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ToList();

            var mappings =
                _context.DetectionCategoryMaterialMaps
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .ToList();

            int minimumSampleCount =
                _context.EstimationSettings
                    .AsNoTracking()
                    .Where(x =>
                        x.SettingsId == 1)
                    .Select(x =>
                        x.MinimumSampleCount)
                    .FirstOrDefault();

            if (minimumSampleCount <= 0)
            {
                minimumSampleCount = 5;
            }

            var mappedMaterialIds =
                mappings
                    .Select(x => x.MaterialId)
                    .Distinct()
                    .ToList();

            var history =
                _context.HistoricalTransactions
                    .AsNoTracking()
                    .Where(x =>
                        mappedMaterialIds.Contains(
                            x.MaterialId
                        ) &&
                        !x.IsDeleted &&
                        !x.IsOutlier &&
                        x.ProjectMasterId.HasValue &&
                        x.ProjectMaster != null &&
                        !x.ProjectMaster.IsDeleted
                    )
                    .Select(x => new
                    {
                        x.MaterialId,
                        ProjectId =
                            x.ProjectMasterId!.Value
                    })
                    .ToList();

            var items =
                categories
                    .Select(category =>
                    {
                        var categoryMaterialIds =
                            mappings
                                .Where(x =>
                                    x.DetectionCategoryId ==
                                    category.DetectionCategoryId
                                )
                                .Select(x => x.MaterialId)
                                .Distinct()
                                .ToList();

                        int historicalTransactionCount =
                            history.Count(x =>
                                categoryMaterialIds.Contains(
                                    x.MaterialId
                                )
                            );

                        int historicalProjectCount =
                            history
                                .Where(x =>
                                    categoryMaterialIds.Contains(
                                        x.MaterialId
                                    ))
                                .Select(x => x.ProjectId)
                                .Distinct()
                                .Count();

                        bool hasMaterialMapping =
                            categoryMaterialIds.Count > 0;

                        bool hasEnoughHistory =
                            historicalProjectCount >=
                            minimumSampleCount;

                        return new
                        {
                            detectionCategoryId =
                                category.DetectionCategoryId,

                            majorCategoryName =
                                category.MajorCategoryName,

                            detectionCategoryName =
                                category.DetectionCategoryName,

                            unitName =
                                category.UnitMaster.UnitName,

                            mappedMaterialCount =
                                categoryMaterialIds.Count,

                            historicalTransactionCount,

                            historicalProjectCount,

                            minimumSampleCount,

                            missingProjectCount =
                                Math.Max(
                                    0,
                                    minimumSampleCount -
                                    historicalProjectCount
                                ),

                            status =
                                !hasMaterialMapping
                                    ? "MaterialMappingRequired"
                                    : historicalProjectCount == 0
                                        ? "HistoricalDataRequired"
                                        : !hasEnoughHistory
                                            ? "InsufficientHistoricalProjects"
                                            : "ReadyForEstimation",

                            message =
                                !hasMaterialMapping
                                    ? "尚未對應標準材料。"
                                    : historicalProjectCount == 0
                                        ? $"已建立材料對應，但尚無可用歷史建案；至少需要 {minimumSampleCount} 案。"
                                        : !hasEnoughHistory
                                            ? $"目前只有 {historicalProjectCount} 個有效歷史建案，至少需要 {minimumSampleCount} 案，尚差 {minimumSampleCount - historicalProjectCount} 案。"
                                            : $"已有 {historicalProjectCount} 個有效歷史建案，已達最低樣本數 {minimumSampleCount}。"
                        };
                    })
                    .ToList();

            return ServiceResult.Ok(new
            {
                categoryCount =
                    items.Count,

                minimumSampleCount,

                readyCount =
                    items.Count(x =>
                        x.status ==
                            "ReadyForEstimation"),

                items
            });
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


        public ServiceResult AssignMaterial(
            int detectionCategoryId,
            int materialId,
            string actionUser)
        {
            var category =
                _context.DetectionCategories
                    .Include(x => x.UnitMaster)
                    .FirstOrDefault(x =>
                        x.DetectionCategoryId ==
                            detectionCategoryId);

            if (category == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的偵錯細項");
            }

            if (!category.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的偵錯細項目前已停用");
            }

            var material =
                _context.MaterialMasters
                    .Include(x => x.UnitMaster)
                    .FirstOrDefault(x =>
                        x.MaterialId == materialId);

            if (material == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定的標準材料");
            }

            if (!material.IsActive)
            {
                return ServiceResult.Validation(
                    "指定的標準材料目前已停用");
            }

            if (!_unitConversionService.CanConvert(
                    material.UnitMaster.UnitName,
                    category.UnitId))
            {
                return ServiceResult.Validation(
                    $"標準材料「{material.MaterialName}」的單位「{material.UnitMaster.UnitName}」無法換算為偵錯細項標準單位「{category.UnitMaster.UnitName}」。");
            }

            var mapping =
                _context.DetectionCategoryMaterialMaps
                    .FirstOrDefault(x =>
                        x.MaterialId == materialId);

            int? oldDetectionCategoryId =
                mapping?.DetectionCategoryId;

            if (mapping == null)
            {
                mapping =
                    new DetectionCategoryMaterialMap
                    {
                        MaterialId = materialId,
                        DetectionCategoryId =
                            detectionCategoryId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                _context
                    .DetectionCategoryMaterialMaps
                    .Add(mapping);
            }
            else
            {
                mapping.DetectionCategoryId =
                    detectionCategoryId;
                mapping.IsActive = true;
                mapping.UpdatedAt =
                    DateTime.UtcNow;
            }

            _auditLogService.AddLog(
                module: "2A偵錯細項",
                actionType: "AssignDetectionMaterial",
                entityType:
                    "DetectionCategoryMaterialMap",
                entityId: materialId,
                description:
                    $"標準材料「{material.MaterialName}」對應至偵錯細項「{category.DetectionCategoryName}」",
                actionUser: actionUser,
                oldValue: new
                {
                    detectionCategoryId =
                        oldDetectionCategoryId
                },
                newValue: new
                {
                    detectionCategoryId,
                    materialId,
                    category =
                        category.DetectionCategoryName,
                    standardUnit =
                        category.UnitMaster.UnitName
                });

            _context.SaveChanges();

            return ServiceResult.Ok(new
            {
                message =
                    "標準材料偵錯細項對應已更新",
                detectionCategoryId,
                materialId
            });
        }


        public ServiceResult UnassignMaterial(
            int materialId,
            string actionUser)
        {
            var mapping =
                _context.DetectionCategoryMaterialMaps
                    .Include(x => x.Material)
                    .Include(x => x.DetectionCategory)
                    .FirstOrDefault(x =>
                        x.MaterialId == materialId);

            if (mapping == null)
            {
                return ServiceResult.NotFound(
                    "此標準材料尚未建立偵錯細項對應");
            }

            bool oldIsActive =
                mapping.IsActive;

            mapping.IsActive = false;
            mapping.UpdatedAt =
                DateTime.UtcNow;

            _auditLogService.AddLog(
                module: "2A偵錯細項",
                actionType:
                    "UnassignDetectionMaterial",
                entityType:
                    "DetectionCategoryMaterialMap",
                entityId: materialId,
                description:
                    $"取消標準材料「{mapping.Material.MaterialName}」的偵錯細項對應",
                actionUser: actionUser,
                oldValue: new
                {
                    mapping.DetectionCategoryId,
                    detectionCategoryName =
                        mapping.DetectionCategory
                            .DetectionCategoryName,
                    isActive = oldIsActive
                },
                newValue: new
                {
                    isActive = false
                });

            _context.SaveChanges();

            return ServiceResult.Ok(new
            {
                message =
                    "偵錯細項對應已取消",
                materialId
            });
        }


        private static string NormalizeText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }
    }
}
