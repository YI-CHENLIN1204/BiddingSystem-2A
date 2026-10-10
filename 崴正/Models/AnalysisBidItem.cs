using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace 崴正.Models
{
    [Table("analysis_bid_item")]
    public class AnalysisBidItem
    {
        [Key]
        [Column("analysis_bid_item_id")]
        public long AnalysisBidItemId { get; set; }

        [Column("analysis_bid_version_id")]
        public long AnalysisBidVersionId { get; set; }

        [Column("source_sequence")]
        public int? SourceSequence { get; set; }

        [Column("sheet_name")]
        public string? SheetName { get; set; }

        [Column("row_number")]
        public int? RowNumber { get; set; }

        [Column("item_code")]
        public string? ItemCode { get; set; }

        [Column("source_category1")]
        public string? SourceCategory1 { get; set; }

        [Column("source_category2")]
        public string? SourceCategory2 { get; set; }

        [Column("source_category3")]
        public string? SourceCategory3 { get; set; }

        [Column("major_category")]
        public string? MajorCategory { get; set; }

        [Column("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [Column("spec")]
        public string? Spec { get; set; }

        [Column("unit")]
        public string? Unit { get; set; }

        [Column("quantity")]
        public decimal? Quantity { get; set; }

        [Column("unit_price")]
        public decimal? UnitPrice { get; set; }

        [Column("amount")]
        public decimal? Amount { get; set; }

        [Column("source_confidence")]
        public decimal? SourceConfidence { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [ForeignKey(nameof(AnalysisBidVersionId))]
        public AnalysisBidVersion AnalysisBidVersion { get; set; } = null!;
    }
}
