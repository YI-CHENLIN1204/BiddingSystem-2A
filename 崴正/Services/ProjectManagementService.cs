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
        private readonly FinalBidService _finalBidService;

        public ProjectManagementService(
            ApplicationDbContext context,
            AuditLogService auditLogService,
            FinalBidService finalBidService)
        {
            _context = context;
            _auditLogService = auditLogService;
            _finalBidService = finalBidService;
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
                            historicalCount,

                        HistoryStatus =
                            project.HistoryStatus,

                        HistoryApprovedAt =
                            project.HistoryApprovedAt,

                        HistoryApprovedBy =
                            project.HistoryApprovedBy
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
                        historyCount,

                    HistoryStatus =
                        project.HistoryStatus,

                    HistoryApprovedAt =
                        project.HistoryApprovedAt,

                    HistoryApprovedBy =
                        project.HistoryApprovedBy
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

            if (string.Equals(
                    project.HistoryStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Conflict(
                    "此建案已納入 2A 歷史基準。若要修改建案條件，請先重新開啟歷史基準審核。"
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
            PublishHistoryAsync(
                int projectId,
                string? actionUser)
        {
            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // Serialize all history-state transitions per project, including across API instances.
                // Advisory XACT lock automatically releases on commit/rollback/disconnect.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"SELECT pg_advisory_xact_lock(2202, {projectId})");
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

                if (string.Equals(
                        project.HistoryStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult.Conflict(
                        "此建案已經納入 2A 歷史基準"
                    );
                }

                var items =
                    await _context.FinalBidItems
                        .Include(x =>
                            x.Material)
                        .Include(x =>
                            x.ProjectMaster)
                        .Where(x =>
                            x.ProjectMasterId ==
                                projectId)
                        .ToListAsync();

                if (!items.Any())
                {
                    return ServiceResult.Validation(
                        "此建案沒有可審核的最終標單工項"
                    );
                }

                var reviewItems =
                    items
                        .Where(x =>
                            !x.IsIgnored)
                        .ToList();

                int unclassifiedItemCount =
                    reviewItems.Count(x =>
                        TwoAScopePolicy
                            .ResolveEffectiveMajorCategory(
                                x.MajorCategoryOverride,
                                x.Category1,
                                x.Category2,
                                x.Category3
                            ) == null);

                var structuralReviewItems =
                    reviewItems
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
                                StringComparison.OrdinalIgnoreCase
                            ))
                        .ToList();

                int structuralUnpricedItemCount =
                    structuralReviewItems.Count(x =>
                        !(
                            x.Amount.HasValue &&
                            x.Amount.Value > 0
                        ) &&
                        !(
                            x.Qty.HasValue &&
                            x.Qty.Value > 0 &&
                            x.UnitPrice.HasValue &&
                            x.UnitPrice.Value > 0
                        ));

                int invalidQuantityBaselineCount =
                    structuralReviewItems.Count(x =>
                        TwoAScopePolicy
                            .IsQuantityHistoryMaterial(
                                x.Material?.MaterialName
                            ) &&
                        !_finalBidService
                            .CanBecomeHistoricalTransaction(
                                x
                            ));

                if (unclassifiedItemCount > 0 ||
                    structuralUnpricedItemCount > 0 ||
                    invalidQuantityBaselineCount > 0)
                {
                    var blockers =
                        new List<string>();

                    if (unclassifiedItemCount > 0)
                    {
                        blockers.Add(
                            $"未辨識工程大項 {unclassifiedItemCount} 筆"
                        );
                    }

                    if (structuralUnpricedItemCount > 0)
                    {
                        blockers.Add(
                            $"結構體無有效價格 {structuralUnpricedItemCount} 筆"
                        );
                    }

                    if (invalidQuantityBaselineCount > 0)
                    {
                        blockers.Add(
                            $"鋼筋／混凝土數量或單位不符合 {invalidQuantityBaselineCount} 筆"
                        );
                    }

                    return ServiceResult.Validation(
                        "尚不能發布歷史基準：" +
                        string.Join(
                            "、",
                            blockers
                        ) +
                        "。請先修正資料，或明確忽略不應參與模型的工項。"
                    );
                }


                string approvedBy =
                    string.IsNullOrWhiteSpace(
                        actionUser)
                        ? "System"
                        : actionUser.Trim();

                project.HistoryStatus =
                    "Approved";

                project.HistoryApprovedAt =
                    DateTime.UtcNow;

                project.HistoryApprovedBy =
                    approvedBy;

                int eligibleCandidateCount =
                    items.Count(x =>
                        _finalBidService
                            .CanBecomeHistoricalTransaction(
                                x
                            ));

                int createdOrUpdatedCount = 0;

                foreach (var item in items)
                {
                    string action =
                        _finalBidService
                            .SyncHistoricalTransaction(
                                item
                            );

                    if (action ==
                            "建立歷史交易" ||
                        action ==
                            "更新歷史交易")
                    {
                        createdOrUpdatedCount++;
                    }
                }

                _auditLogService.AddLog(
                    module:
                        "歷史基準",

                    actionType:
                        "PublishHistoryBaseline",

                    entityType:
                        "ProjectMaster",

                    entityId:
                        project.ProjectId,

                    description:
                        $"確認建案「{project.ProjectName}」並納入 2A 歷史基準",

                    actionUser:
                        approvedBy,

                    projectId:
                        project.ProjectId,

                    oldValue:
                        new
                        {
                            historyStatus =
                                "Draft"
                        },

                    newValue:
                        new
                        {
                            project.HistoryStatus,
                            project.HistoryApprovedAt,
                            project.HistoryApprovedBy,
                            eligibleCandidateCount,
                            createdOrUpdatedCount
                        }
                );

                await _context.SaveChangesAsync();
                int activeHistoryCount =
                    await _context
                        .HistoricalTransactions
                        .AsNoTracking()
                        .CountAsync(x =>
                            x.ProjectMasterId ==
                                projectId &&
                            !x.IsDeleted
                        );

                                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    new
                    {
                        message =
                            "已確認並納入 2A 歷史基準",

                        projectId =
                            project.ProjectId,

                        historyStatus =
                            project.HistoryStatus,

                        historyApprovedAt =
                            project.HistoryApprovedAt,

                        historyApprovedBy =
                            project.HistoryApprovedBy,

                        eligibleCandidateCount,

                        activeHistoryCount
                    }
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[PublishHistory] Original failure: " + ex);
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[PublishHistory] Rollback failed: " + rollbackEx);
                }
                throw;
            }
        }


        public async Task<ServiceResult>
            ReopenHistoryReviewAsync(
                int projectId,
                string? actionUser)
        {
            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                // Serialize all history-state transitions per project, including across API instances.
                // Advisory XACT lock automatically releases on commit/rollback/disconnect.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"SELECT pg_advisory_xact_lock(2202, {projectId})");
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

                if (!string.Equals(
                        project.HistoryStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ServiceResult.Conflict(
                        "此建案目前不是已發布的歷史基準"
                    );
                }

                var histories =
                    await _context
                        .HistoricalTransactions
                        .Where(x =>
                            x.ProjectMasterId ==
                                projectId &&
                            !x.IsDeleted
                        )
                        .ToListAsync();

                foreach (var history in histories)
                {
                    history.IsDeleted =
                        true;
                }

                var oldValue = new
                {
                    project.HistoryStatus,
                    project.HistoryApprovedAt,
                    project.HistoryApprovedBy,
                    activeHistoricalTransactionCount =
                        histories.Count
                };

                project.HistoryStatus =
                    "Draft";

                project.HistoryApprovedAt =
                    null;

                project.HistoryApprovedBy =
                    null;

                _auditLogService.AddLog(
                    module:
                        "歷史基準",

                    actionType:
                        "ReopenHistoryReview",

                    entityType:
                        "ProjectMaster",

                    entityId:
                        project.ProjectId,

                    description:
                        $"重新開啟建案「{project.ProjectName}」的歷史基準審核",

                    actionUser:
                        actionUser ?? "System",

                    projectId:
                        project.ProjectId,

                    oldValue:
                        oldValue,

                    newValue:
                        new
                        {
                            project.HistoryStatus,
                            disabledHistoricalTransactionCount =
                                histories.Count
                        }
                );

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult.Ok(
                    new
                    {
                        message =
                            "已重新開啟審核；此建案暫時不參與 2A 模型",

                        projectId =
                            project.ProjectId,

                        historyStatus =
                            project.HistoryStatus,

                        disabledHistoricalTransactionCount =
                            histories.Count
                    }
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[DatabaseTransaction] Operation failed: " + ex);
                try { await transaction.RollbackAsync(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[DatabaseTransaction] Rollback also failed: " + rollbackEx);
                }
                throw;
            }
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
            catch (Exception ex)
            {
                Console.Error.WriteLine("[DatabaseTransaction] Operation failed: " + ex);
                try { await transaction.RollbackAsync(); }
                catch (Exception rollbackEx)
                {
                    Console.Error.WriteLine("[DatabaseTransaction] Rollback also failed: " + rollbackEx);
                }
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
                        x.MaterialId,
                        MaterialName =
                            x.Material != null
                                ? x.Material.MaterialName
                                : null
                    })
                    .ToListAsync();

            var reviewEntities =
                await _context.FinalBidItems
                    .Include(x =>
                        x.Material)
                    .Include(x =>
                        x.ProjectMaster)
                    .Where(x =>
                        x.ProjectMasterId ==
                            projectId &&
                        !x.IsIgnored
                    )
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

            int unclassifiedItemCount =
                sourceItems.Count(x =>
                    TwoAScopePolicy
                        .ResolveEffectiveMajorCategory(
                            x.MajorCategoryOverride,
                            x.Category1,
                            x.Category2,
                            x.Category3
                        ) == null);

            int manualMajorCategoryCount =
                sourceItems.Count(x =>
                    !string.IsNullOrWhiteSpace(
                        x.MajorCategoryOverride));

            int structuralUnmappedItemCount =
                structuralItems.Count(x =>
                    !x.MaterialId.HasValue);

            int rebarSourceItemCount =
                structuralItems.Count(x =>
                    string.Equals(
                        x.MaterialName,
                        "鋼筋",
                        StringComparison.OrdinalIgnoreCase
                    ));

            int concreteSourceItemCount =
                structuralItems.Count(x =>
                    string.Equals(
                        x.MaterialName,
                        "混凝土",
                        StringComparison.OrdinalIgnoreCase
                    ));

            int rebarCandidateItemCount =
                reviewEntities.Count(x =>
                    string.Equals(
                        TwoAScopePolicy
                            .ResolveEffectiveMajorCategory(
                                x.MajorCategoryOverride,
                                x.Category1,
                                x.Category2,
                                x.Category3
                            ),
                        "結構體工程",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    string.Equals(
                        x.Material?.MaterialName,
                        "鋼筋",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    _finalBidService
                        .CanBecomeHistoricalTransaction(
                            x
                        ));

            int concreteCandidateItemCount =
                reviewEntities.Count(x =>
                    string.Equals(
                        TwoAScopePolicy
                            .ResolveEffectiveMajorCategory(
                                x.MajorCategoryOverride,
                                x.Category1,
                                x.Category2,
                                x.Category3
                            ),
                        "結構體工程",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    string.Equals(
                        x.Material?.MaterialName,
                        "混凝土",
                        StringComparison.OrdinalIgnoreCase
                    ) &&
                    _finalBidService
                        .CanBecomeHistoricalTransaction(
                            x
                        ));


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

            var publishBlockingIssues =
                new List<string>();

            if (unclassifiedItemCount > 0)
            {
                publishBlockingIssues.Add(
                    $"未辨識工程大項 {unclassifiedItemCount} 筆"
                );
            }

            int structuralUnpricedItemCount =
                structuralItems.Count -
                structuralPricedItemCount;

            if (structuralUnpricedItemCount > 0)
            {
                publishBlockingIssues.Add(
                    $"結構體無有效價格 {structuralUnpricedItemCount} 筆"
                );
            }

            int quantityBlockedItemCount =
                Math.Max(
                    0,
                    rebarSourceItemCount -
                    rebarCandidateItemCount
                ) +
                Math.Max(
                    0,
                    concreteSourceItemCount -
                    concreteCandidateItemCount
                );

            if (quantityBlockedItemCount > 0)
            {
                publishBlockingIssues.Add(
                    $"鋼筋／混凝土數量或單位不符合 {quantityBlockedItemCount} 筆"
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

                    HistoryStatus =
                        project.HistoryStatus,

                    HistoryApprovedAt =
                        project.HistoryApprovedAt,

                    HistoryApprovedBy =
                        project.HistoryApprovedBy,

                    StructuralItemCount =
                        structuralItems.Count,

                    StructuralPricedItemCount =
                        structuralPricedItemCount,

                    StructuralUnpricedItemCount =
                        structuralUnpricedItemCount,

                    UnclassifiedItemCount =
                        unclassifiedItemCount,

                    ManualMajorCategoryCount =
                        manualMajorCategoryCount,

                    StructuralUnmappedItemCount =
                        structuralUnmappedItemCount,

                    RebarSourceItemCount =
                        rebarSourceItemCount,

                    RebarCandidateItemCount =
                        rebarCandidateItemCount,

                    RebarExcludedItemCount =
                        Math.Max(
                            0,
                            rebarSourceItemCount -
                            rebarCandidateItemCount
                        ),

                    ConcreteSourceItemCount =
                        concreteSourceItemCount,

                    ConcreteCandidateItemCount =
                        concreteCandidateItemCount,

                    ConcreteExcludedItemCount =
                        Math.Max(
                            0,
                            concreteSourceItemCount -
                            concreteCandidateItemCount
                        ),

                    PublishReady =
                        publishBlockingIssues.Count == 0,

                    PublishBlockingIssues =
                        publishBlockingIssues,

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
                            : string.Equals(
                                project.HistoryStatus,
                                "Approved",
                                StringComparison.OrdinalIgnoreCase)
                                ? "此建案已人工確認並納入 2A 歷史基準。"
                                : "此建案目前仍在審核階段；下列資料僅為候選基準，尚未參與 2A 模型。"
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