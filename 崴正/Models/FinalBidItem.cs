namespace 崴正.Models
{
    public class FinalBidItem
    {
        public long FinalBidItemId { get; set; }

        public int ProjectMasterId { get; set; }

        public string SourceFileName { get; set; } = string.Empty;

        public string? SheetName { get; set; }

        public int? RowNumber { get; set; }

        public string? ItemCode { get; set; }

        public string? Category1 { get; set; }

        public string? Category2 { get; set; }

        public string? Category3 { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string? Spec { get; set; }

        public string? Unit { get; set; }

        public decimal? Qty { get; set; }

        public decimal? UnitPrice { get; set; }

        public decimal? Amount { get; set; }

        public string? Note { get; set; }

        public int? MaterialId { get; set; }

        // Auto / Manual / Unmapped
        // Pending / Auto / Manual
        public string MappingStatus { get; set; } = "Pending";

        public string? MappingIssueCode { get; set; }

        public string? MappingIssueMessage { get; set; }

        public string? MatchedKeyword { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public ProjectMaster ProjectMaster { get; set; } = null!;

        public MaterialMaster? Material { get; set; }
    }
}