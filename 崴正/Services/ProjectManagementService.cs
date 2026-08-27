using System.Globalization;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class ProjectManagementService
    {
        private readonly ApplicationDbContext _context;

        public ProjectManagementService(
            ApplicationDbContext context)
        {
            _context = context;
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
                ProjectSaveRequest request)
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

            project.FloorCount =
                request.FloorCount;

            project.BasementFloorCount =
                request.BasementFloorCount;

            project.FloorRange =
                BuildFloorRange(
                    request.FloorCount!.Value,
                    request
                        .BasementFloorCount!
                        .Value
                );

            project.StartDate =
                projectDate;

            await _context.SaveChangesAsync();

            return ServiceResult.Ok(new
            {
                message =
                    "建案資料已更新"
            });
        }


        public async Task<ServiceResult>
            DeleteProjectAsync(
                int projectId)
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
                                ? x.Material.BaseUnit
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
                        m.Category,
                        m.BaseUnit
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

                        baseUnit =
                            g.Key.BaseUnit,

                        transactionCount =
                            g.Count(),

                        totalQuantity =
                            g.Sum(x =>
                                x.ActualQuantity),

                        averageUnitPrice =
                            g.Average(x =>
                                x.UnitPrice)
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


        private static string BuildFloorRange(
            int floorCount,
            int basementFloorCount)
        {
            return basementFloorCount > 0
                ? $"B{basementFloorCount}~{floorCount}F"
                : $"1~{floorCount}F";
        }
    }
}