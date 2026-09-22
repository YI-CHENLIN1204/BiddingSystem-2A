using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext
            _context;

        public AuditLogService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public AuditLog AddLog(
            string module,
            string actionType,
            string entityType,
            object? entityId,
            string description,
            string actionUser,
            int? projectId = null,
            object? oldValue = null,
            object? newValue = null)
        {
            var log =
                new AuditLog
                {
                    Module =
                        Normalize(module),

                    ActionType =
                        Normalize(actionType),

                    EntityType =
                        Normalize(entityType),

                    EntityId =
                        entityId?.ToString(),

                    ProjectId =
                        projectId,

                    Description =
                        Normalize(description),

                    OldValue =
                        SerializeValue(
                            oldValue),

                    NewValue =
                        SerializeValue(
                            newValue),

                    // 目前系統不做登入 / 權限，
                    // 因此不建立虛假的使用者身分。
                    ActionUser =
                        "System",

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.AuditLogs.Add(
                log
            );

            return log;
        }


        // ==========================================
        // 操作紀錄管理頁
        // ==========================================
        public async Task<AuditLogPageViewModel>
            GetPageAsync(
                string? search,
                string? module,
                string? actionType,
                DateTime? startDate,
                DateTime? endDate,
                int page,
                int pageSize)
        {
            search =
                NormalizeOptional(
                    search
                );

            module =
                NormalizeOptional(
                    module
                );

            actionType =
                NormalizeOptional(
                    actionType
                );


            if (page < 1)
            {
                page = 1;
            }


            if (
                pageSize != 25 &&
                pageSize != 50 &&
                pageSize != 100
            )
            {
                pageSize = 25;
            }


            var query =
                _context.AuditLogs
                    .AsNoTracking()
                    .AsQueryable();


            if (!string.IsNullOrWhiteSpace(
                    search))
            {
                string pattern =
                    $"%{search}%";

                query =
                    query.Where(x =>
                        EF.Functions.ILike(
                            x.Module,
                            pattern
                        )
                        ||
                        EF.Functions.ILike(
                            x.ActionType,
                            pattern
                        )
                        ||
                        EF.Functions.ILike(
                            x.EntityType,
                            pattern
                        )
                        ||
                        (
                            x.EntityId != null &&
                            EF.Functions.ILike(
                                x.EntityId,
                                pattern
                            )
                        )
                        ||
                        EF.Functions.ILike(
                            x.Description,
                            pattern
                        )
                    );
            }


            if (!string.IsNullOrWhiteSpace(
                    module))
            {
                query =
                    query.Where(x =>
                        x.Module ==
                        module
                    );
            }


            if (!string.IsNullOrWhiteSpace(
                    actionType))
            {
                query =
                    query.Where(x =>
                        x.ActionType ==
                        actionType
                    );
            }


            if (startDate.HasValue)
            {
                DateTime startUtc =
                    DateTime.SpecifyKind(
                        startDate.Value.Date,
                        DateTimeKind.Utc
                    );

                query =
                    query.Where(x =>
                        x.CreatedAt >=
                        startUtc
                    );
            }


            if (endDate.HasValue)
            {
                DateTime endExclusiveUtc =
                    DateTime.SpecifyKind(
                        endDate.Value.Date
                            .AddDays(1),
                        DateTimeKind.Utc
                    );

                query =
                    query.Where(x =>
                        x.CreatedAt <
                        endExclusiveUtc
                    );
            }


            int totalCount =
                await query.CountAsync();


            int totalPages =
                totalCount == 0
                    ? 1
                    : (int)Math.Ceiling(
                        totalCount /
                        (double)pageSize
                    );


            if (page > totalPages)
            {
                page =
                    totalPages;
            }


            var logs =
                await query
                    .OrderByDescending(x =>
                        x.CreatedAt)
                    .ThenByDescending(x =>
                        x.LogId)
                    .Skip(
                        (page - 1) *
                        pageSize
                    )
                    .Take(
                        pageSize
                    )
                    .ToListAsync();


            var projectIds =
                logs
                    .Where(x =>
                        x.ProjectId.HasValue)
                    .Select(x =>
                        x.ProjectId!.Value)
                    .Distinct()
                    .ToList();


            var projectNames =
                projectIds.Count == 0
                    ? new Dictionary<int, string>()
                    : await _context.ProjectMasters
                        .AsNoTracking()
                        .Where(x =>
                            projectIds.Contains(
                                x.ProjectId
                            ))
                        .ToDictionaryAsync(
                            x =>
                                x.ProjectId,
                            x =>
                                x.ProjectName
                        );


            var modules =
                await _context.AuditLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.Module != "")
                    .Select(x =>
                        x.Module)
                    .Distinct()
                    .OrderBy(x =>
                        x)
                    .ToListAsync();


            var actionTypes =
                await _context.AuditLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.ActionType != "")
                    .Select(x =>
                        x.ActionType)
                    .Distinct()
                    .OrderBy(x =>
                        x)
                    .ToListAsync();


            return new AuditLogPageViewModel
            {
                Search =
                    search,

                Module =
                    module,

                ActionType =
                    actionType,

                StartDate =
                    startDate?.Date,

                EndDate =
                    endDate?.Date,

                Page =
                    page,

                PageSize =
                    pageSize,

                TotalCount =
                    totalCount,

                TotalPages =
                    totalPages,

                Modules =
                    modules,

                ActionTypes =
                    actionTypes,

                Items =
                    logs
                        .Select(log =>
                            new AuditLogListItemViewModel
                            {
                                LogId =
                                    log.LogId,

                                Module =
                                    log.Module,

                                ActionType =
                                    log.ActionType,

                                EntityType =
                                    log.EntityType,

                                EntityId =
                                    log.EntityId,

                                ProjectId =
                                    log.ProjectId,

                                ProjectName =
                                    log.ProjectId.HasValue &&
                                    projectNames.TryGetValue(
                                        log.ProjectId.Value,
                                        out var projectName
                                    )
                                        ? projectName
                                        : null,

                                Description =
                                    log.Description,

                                OldValue =
                                    log.OldValue,

                                NewValue =
                                    log.NewValue,

                                CreatedAt =
                                    log.CreatedAt
                            }
                        )
                        .ToList()
            };
        }


        private static string?
            SerializeValue(
                object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is string text)
            {
                return text;
            }

            return JsonSerializer.Serialize(
                value
            );
        }


        private static string Normalize(
            string? value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? string.Empty
                : value.Trim();
        }


        private static string?
            NormalizeOptional(
                string? value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? null
                : value.Trim();
        }
    }
}
