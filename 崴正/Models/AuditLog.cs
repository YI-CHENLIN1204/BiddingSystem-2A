using System;
using System.ComponentModel.DataAnnotations;

namespace 崴正.Models
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }
        public string SourceFileName { get; set; } = "";
        public string SheetName { get; set; } = "";
        public int RowNumber { get; set; }
        public string ItemName { get; set; } = "";
        public string? OriginalStatusMessage { get; set; }
        public string OverrideReason { get; set; } = "";
        public string ActionUser { get; set; } = "Estimator_Staff";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 順便定義前端傳進來的包裹 (DTO)
    public class OverrideRequestDto
    {
        public string SourceFileName { get; set; } = "";
        public string SheetName { get; set; } = "";
        public int RowNumber { get; set; }
        public string ItemName { get; set; } = "";
        public string OriginalStatusMessage { get; set; } = "";
        public string OverrideReason { get; set; } = "";
    }
}