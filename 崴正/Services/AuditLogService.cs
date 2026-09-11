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

                    ActionUser =
                        string.IsNullOrWhiteSpace(
                            actionUser)
                            ? "System"
                            : actionUser.Trim(),

                    CreatedAt =
                        DateTime.UtcNow
                };


            _context.AuditLogs.Add(
                log
            );

            return log;
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
    }
}