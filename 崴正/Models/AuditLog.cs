using System;
using System.ComponentModel.DataAnnotations;

namespace 崴正.Models
{
    public class AuditLog
    {
        [Key]
        public long LogId { get; set; }

        public string Module { get; set; }
            = string.Empty;

        public string ActionType { get; set; }
            = string.Empty;

        public string EntityType { get; set; }
            = string.Empty;

        public string? EntityId { get; set; }

        public int? ProjectId { get; set; }

        public string Description { get; set; }
            = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string ActionUser { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}