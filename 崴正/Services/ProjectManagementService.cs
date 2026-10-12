using System.Globalization;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class ProjectManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public ProjectManagementService(
            ApplicationDbContext context,
            AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }


        public async Task<List<ProjectManagementDto>>
            GetProjectsAsync(
                string? search = null)
        {
            var query =
                _context.ProjectMasters
                    .AsNoTracking()
                    .Where(x =>
                        !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern =
                    $"%{search.Trim()}%";

                query = query.Where(x =>
                    EF.Functions.ILike(
                        x.ProjectName,
                        pattern
                    )
                    ||
                    (
                        x.City != null &&
                        EF.Functions.ILike(
                            x.City,
                            pattern
                        )
                    )
                );
            }

            var projects =
                await query
                    .OrderByDescending(x =>
                        x.StartDate)
                    .ThenByDescending(x =>
                        x.ProjectId)
                    .ToListAsync();

            var projectIds =
                projects
                    .Select(x =>
                        x.ProjectId)
                    .ToList();

            var finalBidCounts =
                await _context.FinalBidItems
                    .AsNoTracking()
                    .Where(x =>
                        projectIds.Contains(
                            x.ProjectMasterId
                        ))
                    .GroupBy(x =>
                        x.ProjectMasterId)
                    .Select(g => new
                    {
                        ProjectId =
                            g.Key,

                        Count =
                            g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.ProjectId,
                        x => x.Count
                    );

            var historyCounts =
                await _context.HistoricalTransactions
                    .AsNoTracking()
                    .Where(x =>
                        x.ProjectMasterId.HasValue &&
                        projectIds.Contains(
                            x.ProjectMasterId.Value
                        ) &&
                        !x.IsDeleted)
                    .GroupBy(x =>
                        x.ProjectMasterId!.Value)
                    .Select(g => new
                    {
                        ProjectId =
                            g.Key,

                        Count =
                            g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.ProjectId,
                        x => x.Count
                    );

            return projects
                .Select(project =>
                {
                    int finalBidItemCount =
                        finalBidCounts
                            .TryGetValue(
                                project.ProjectId,
                                out var bidCount)
                            ? bidCount
                            : 0;

                    int historicalCount =
                        historyCounts
                            .TryGetValue(
                                project.ProjectId,
                                out var historyCount)
                            ? historyCount
                            : 0;

                    return new ProjectManagementDto
                    {
                        ProjectId =
                            project.ProjectId,

                        ProjectName =
                            project.ProjectName,

                        ProjectType =
                            project.ProjectType,

                        StructureType =
                            project.StructureType,

                        City =
                            project.City,

                        TotalFloorArea =
                            project.TotalFloorArea,

                        SiteArea =
                            project.SiteArea,

                        FloorCount =
                            project.FloorCount,

                        BasementFloorCount =
                            project.BasementFloorCount,

                        StartDate =
                            project.StartDate,

                        HasFinalBid =
                            finalBidItemCount > 0,

                        FinalBidItemCount =
                            finalBidItemCount,

                        HistoricalTransactionCount =
                            historicalCount
                    };
                })
                .ToList();
        }


        public async Task<ServiceResult>
            GetProjectAsync(
                int projectId)
        {
            var project =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId ==
                        projectId &&
                        !x.IsDeleted
                    );

            if (project == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定建案"
                );
            }

            int finalBidItemCount =
                await _context.FinalBidItems
                    .AsNoTracking()
                    .CountAsync(x =>
                        x.ProjectMasterId ==
                        projectId
                    );

            int historyCount =
                await _context.HistoricalTransactions
                    .AsNoTracking()
                    .CountAsync(x =>
                        x.ProjectMasterId ==
                            projectId &&
                        !x.IsDeleted
                    );

            return ServiceResult.Ok(
                new ProjectManagementDto
                {
                    ProjectId =
                        project.ProjectId,

                    ProjectName =
                        project.ProjectName,

                    ProjectType =
                        project.ProjectType,

                    StructureType =
                        project.StructureType,

                    City =
                        project.City,

                    TotalFloorArea =
                        project.TotalFloorArea,

                    FloorCount =
                        project.FloorCount,

                    BasementFloorCount =
                        project.BasementFloorCount,

                    StartDate =
                        project.StartDate,

                    HasFinalBid =
                        finalBidItemCount > 0,

                    FinalBidItemCount =
                        finalBidItemCount,

                    HistoricalTransactionCount =
                        historyCount
                }
            );
        }


        public async Task<ServiceResult>
            UpdateProjectAsync(
                int projectId,
                ProjectSaveRequest request,
                string? actionUser)
        {
            var validation =
                ValidateRequest(
                    request,
                    out var projectDate
                );

            if (validation != null)
            {
                return validation;
            }

            var project =
                await _context.ProjectMasters
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId ==
                            projectId &&
                        !x.IsDeleted
                    );

            if (project == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定建案"
                );
            }

            string projectName =
                request.ProjectName.Trim();

            bool duplicate =
                await _context.ProjectMasters
                    .AnyAsync(x =>
                        !x.IsDeleted &&
                        x.ProjectId !=
                            projectId &&
                        x.ProjectName ==
                            projectName
                    );

            if (duplicate)
            {
                return ServiceResult.Conflict(
                    "已存在相同名稱的建案"
                );
            }

            var oldValue = new
            {
                project.ProjectName,
                project.ProjectType,
                project.StructureType,
                project.City,
                project.TotalFloorArea,
                project.SiteArea,
                project.FloorCount,
                project.BasementFloorCount,
                project.StartDate,
                project.IsDeleted
            };

            project.ProjectName =
                projectName;

            project.ProjectType =
                request.ProjectType.Trim();

            project.StructureType =
                request.StructureType.Trim();

            project.City =
                request.City.Trim();

            project.TotalFloorArea =
                request.TotalFloorArea;

            project.SiteArea =
                request.SiteArea;

            project.FloorCount =
                request.FloorCount;

            project.BasementFloorCount =
                request.BasementFloorCount;

            project.StartDate =
                projectDate;

            _auditLogService.AddLog(
                module:
                    "建案管理",

                actionType:
                    "UpdateProject",

                entityType:
                    "ProjectMaster",

                entityId:
                    project.ProjectId,

                description:
                    $"修改建案「{project.ProjectName}」",

                actionUser:
                    actionUser ?? "System",

                projectId:
                    project.ProjectId,

                oldValue:
                    oldValue,

                newValue:
                    new
                    {
                        project.ProjectName,
                        project.ProjectType,
                        project.StructureType,
                        project.City,
                        project.TotalFloorArea,
                        project.SiteArea,
                        project.FloorCount,
                        project.BasementFloorCount,
                        project.StartDate,
                        project.IsDeleted
                    }
            );

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                message =
                    "建案資料已更新"
            });
        }


        public async Task<ServiceResult>
            DeleteProjectAsync(
                int projectId,
                string? actionUser)
        {
            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var project =
                    await _context.ProjectMasters
                        .FirstOrDefaultAsync(x =>
                            x.ProjectId ==
                                projectId &&
                            !x.IsDeleted
                        );

                if (project == null)
                {
                    return ServiceResult.NotFound(
                        "找不到指定建案"
                    );
                }

                project.IsDeleted =
                    true;

                var histories =
                    await _context
                        .HistoricalTransactions
                        .Where(x =>
                            x.ProjectMasterId ==
                                projectId &&
                            !x.IsDeleted)
                        .ToListAsync();

                foreach (var history in histories)
                {
                    history.IsDeleted =
                        true;
                }

                _auditLogService.AddLog(
                    module:
                        "建案管理",

                    actionType:
                        "DeleteProject",

                    entityType:
                        "ProjectMaster",

                    entityId:
                        project.ProjectId,

                    description:
                        $"刪除建案「{project.ProjectName}」",

                    actionUser:
                        actionUser ?? "System",

                    projectId:
                        project.ProjectId,

                    oldValue:
                        new
                        {
                            isDeleted =
                                false,

                            activeHistoricalTransactionCount =
                                histories.Count
                        },

                    newValue:
                        new
                        {
                            isDeleted =
                                true,

                            deletedHistoricalTransactionCount =
                                histories.Count
                        }
                );

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return ServiceResult.Ok(new
                {
                    message =
                        $"已刪除建案「{project.ProjectName}」"
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult>
            GetFinalBidItemsAsync(
                int projectId,
                string? search,
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

            bool projectExists =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.ProjectId == projectId &&
                        !x.IsDeleted
                    );

            if (!projectExists)
            {
                return ServiceResult.NotFound(
                    "找不到指定建案"
                );
            }

            var query =
                _context.FinalBidItems
                    .AsNoTracking()
                    .Where(x =>
                        x.ProjectMasterId ==
                        projectId
                    );

            if (!string.IsNullOrWhiteSpace(
                    search))
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
                );
            }

            int totalCount =
                await query.CountAsync();

            var items =
                await query
                    .OrderBy(x =>
                        x.RowNumber)
                    .ThenBy(x =>
                        x.FinalBidItemId)
                    .Skip(
                        (page - 1) *
                        pageSize
                    )
                    .Take(pageSize)
                    .Select(x => new
                    {
                        finalBidItemId =
                            x.FinalBidItemId,

                        itemName =
                            x.ItemName,

                        unit =
                            x.Unit,

                        qty =
                            x.Qty,

                        unitPrice =
                            x.UnitPrice,

                        amount =
                            x.Amount,

                        mappingStatus =
                            x.MappingStatus,

                        isIgnored =
                            x.IsIgnored,

                        materialId =
                            x.MaterialId,

                        materialName =
                            x.Material != null
                                ? x.Material.MaterialName
                                : null,

                        materialUnit =
                            x.Material != null
                                ? x.Material.UnitMaster.UnitName
                                : null,

                        sheetName =
                            x.SheetName,

                        rowNumber =
                            x.RowNumber
                    })
                    .ToListAsync();

            return ServiceResult.Ok(new
            {
                totalCount,
                page,
                pageSize,
                items
            });
        }

        public async Task<ServiceResult>
            GetHistoricalDataAsync(
                int projectId)
        {
            bool projectExists =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.ProjectId == projectId &&
                        !x.IsDeleted
                    );

            if (!projectExists)
            {
                return ServiceResult.NotFound(
                    "找不到指定建案"
                );
            }

            var history =
                await (
                    from h in
                        _context.HistoricalTransactions
                            .AsNoTracking()

                    join m in
                        _context.MaterialMasters
                            .AsNoTracking()

                    on h.MaterialId
                    equals m.MaterialId

                    where
                        h.ProjectMasterId ==
                            projectId
                        &&
                        !h.IsDeleted

                    group h by new
                    {
                        m.MaterialId,
                        m.MaterialName,
                        Category =
                            m.MaterialCategory.CategoryName,
                        UnitName = m.UnitMaster.UnitName
                    }
                    into g

                    select new
                    {
                        materialId =
                            g.Key.MaterialId,

                        materialName =
                            g.Key.MaterialName,

                        category =
                            g.Key.Category,

                        unitName =
                            g.Key.UnitName,

                        transactionCount =
                            g.Count(),

                        totalQuantity =
                            g.Sum(x =>
                                x.ActualQuantity)
                    }
                )
                .OrderBy(x =>
                    x.category)
                .ThenBy(x =>
                    x.materialName)
                .ToListAsync();

            return ServiceResult.Ok(new
            {
                totalCount =
                    history.Count,

                items =
                    history
            });
        }


        public async Task<ServiceResult>
            GetTwoASummaryAsync(
                int projectId)
        {
            var project =
                await _context.ProjectMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == projectId &&
                        !x.IsDeleted
                    );

            if (project == null)
            {
                return ServiceResult.NotFound(
                    "找不到指定建案"
                );
            }

            var sourceItems =
                await _context.FinalBidItems
                    .AsNoTracking()
                    .Where(x =>
                        x.ProjectMasterId ==
                            projectId &&
                        !x.IsIgnored
                    )
                    .Select(x => new
                    {
                        x.Category1,
                        x.Category2,
                        x.Category3,
                        x.MajorCategoryOverride,
                        x.Qty,
                        x.UnitPrice,
                        x.Amount,
                        MaterialName =
                            x.Material != null
                                ? x.Material.MaterialName
                                : null
                    })
                    .ToListAsync();

            var structuralItems =
                sourceItems
                    .Where(x =>
                        string.Equals(
                            TwoAScopePolicy
                                .ResolveEffectiveMajorCategory(
                                    x.MajorCategoryOverride,
                                    x.Category1,
                                    x.Category2,
                                    x.Category3
                                ),
                            "結構體工程",
                            StringComparison
                                .OrdinalIgnoreCase
                        ))
                    .ToList();

            decimal structuralAmount =
                0m;

            int structuralPricedItemCount =
                0;

            foreach (var item in structuralItems)
            {
                decimal? amount =
                    item.Amount.HasValue &&
                    item.Amount.Value > 0
                        ? item.Amount.Value
                        : item.Qty.HasValue &&
                          item.Qty.Value > 0 &&
                          item.UnitPrice.HasValue &&
                          item.UnitPrice.Value > 0
                            ? item.Qty.Value *
                              item.UnitPrice.Value
                            : null;

                if (!amount.HasValue)
                {
                    continue;
                }

                structuralAmount +=
                    amount.Value;

                structuralPricedItemCount++;
            }

            var history =
                await (
                    from h in
                        _context.HistoricalTransactions
                            .AsNoTracking()

                    join m in
                        _context.MaterialMasters
                            .AsNoTracking()

                    on h.MaterialId
                    equals m.MaterialId

                    where
                        h.ProjectMasterId ==
                            projectId &&
                        !h.IsDeleted &&
                        !h.IsOutlier &&
                        (
                            m.MaterialName ==
                                "鋼筋" ||
                            m.MaterialName ==
                                "混凝土"
                        )

                    group h by m.MaterialName
                    into g

                    select new
                    {
                        MaterialName =
                            g.Key,

                        TransactionCount =
                            g.Count(),

                        Quantity =
                            g.Sum(x =>
                                x.ActualQuantity)
                    }
                )
                .ToListAsync();

            var rebar =
                history.FirstOrDefault(x =>
                    x.MaterialName == "鋼筋");

            var concrete =
                history.FirstOrDefault(x =>
                    x.MaterialName == "混凝土");

            decimal? structuralPricePerPing =
                null;

            if (
                structuralAmount > 0 &&
                project.TotalFloorArea.HasValue &&
                project.TotalFloorArea.Value > 0
            )
            {
                decimal floorAreaPing =
                    project.TotalFloorArea.Value /
                    3.305785m;

                if (floorAreaPing > 0)
                {
                    structuralPricePerPing =
                        Math.Round(
                            structuralAmount /
                            floorAreaPing,
                            2
                        );
                }
            }

            int otherStructuralItemCount =
                structuralItems.Count(x =>
                    !string.Equals(
                        x.MaterialName,
                        "鋼筋",
                        StringComparison
                            .OrdinalIgnoreCase
                    ) &&
                    !string.Equals(
                        x.MaterialName,
                        "混凝土",
                        StringComparison
                            .OrdinalIgnoreCase
                    ));

            return ServiceResult.Ok(
                new ProjectTwoASummaryDto
                {
                    ProjectId =
                        project.ProjectId,

                    StructuralItemCount =
                        structuralItems.Count,

                    StructuralPricedItemCount =
                        structuralPricedItemCount,

                    StructuralAmount =
                        Math.Round(
                            structuralAmount,
                            2
                        ),

                    StructuralPricePerPing =
                        structuralPricePerPing,

                    RebarQuantity =
                        rebar == null
                            ? null
                            : Math.Round(
                                rebar.Quantity,
                                4
                            ),

                    RebarTransactionCount =
                        rebar?.TransactionCount
                        ?? 0,

                    ContributesRebarHistory =
                        rebar != null &&
                        rebar.Quantity > 0,

                    ConcreteQuantity =
                        concrete == null
                            ? null
                            : Math.Round(
                                concrete.Quantity,
                                4
                            ),

                    ConcreteTransactionCount =
                        concrete
                            ?.TransactionCount
                        ?? 0,

                    ContributesConcreteHistory =
                        concrete != null &&
                        concrete.Quantity > 0,

                    OtherStructuralItemCount =
                        otherStructuralItemCount,

                    Message =
                        structuralItems.Count == 0
                            ? "此最終標單尚未辨識到結構體工程工項。"
                            : "此摘要顯示本建案實際提供給 2A 的結構體工程價格基準，以及鋼筋、混凝土數量歷史基準。"
                }
            );
        }


        private static ServiceResult?
            ValidateRequest(
                ProjectSaveRequest request,
                out DateTime projectDate)
        {
            projectDate =
                default;

            if (string.IsNullOrWhiteSpace(
                    request.ProjectName))
            {
                return ServiceResult.Validation(
                    "請輸入建案名稱"
                );
            }

            if (
                request.ProjectType != "住宅" &&
                request.ProjectType != "廠房"
            )
            {
                return ServiceResult.Validation(
                    "建案類型只允許住宅或廠房"
                );
            }

            if (
                request.StructureType != "RC" &&
                request.StructureType != "SC" &&
                request.StructureType != "SRC"
            )
            {
                return ServiceResult.Validation(
                    "結構類型只允許 RC、SC 或 SRC"
                );
            }

            if (string.IsNullOrWhiteSpace(
                    request.City))
            {
                return ServiceResult.Validation(
                    "請選擇縣市"
                );
            }

            if (!request.TotalFloorArea.HasValue ||
                request.TotalFloorArea.Value <= 0)
            {
                return ServiceResult.Validation(
                    "總樓地板面積必須大於 0"
                );
            }

            if (!request.SiteArea.HasValue ||
                request.SiteArea.Value <= 0)
            {
                return ServiceResult.Validation(
                    "基地面積必須大於 0"
                );
            }

            if (!request.FloorCount.HasValue ||
                request.FloorCount.Value <= 0)
            {
                return ServiceResult.Validation(
                    "地上樓層數必須大於 0"
                );
            }

            if (!request.BasementFloorCount.HasValue ||
                request.BasementFloorCount.Value < 0)
            {
                return ServiceResult.Validation(
                    "地下樓層數不可小於 0"
                );
            }

            if (!DateTime.TryParseExact(
                    request.ProjectMonth,
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out projectDate))
            {
                return ServiceResult.Validation(
                    "建案年月格式錯誤"
                );
            }

            projectDate =
                new DateTime(
                    projectDate.Year,
                    projectDate.Month,
                    1
                );

            return null;
        }


    }
}