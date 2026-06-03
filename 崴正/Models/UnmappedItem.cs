using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace 崴正.Models
{
    [Table("unmapped_items")]
    public class UnmappedItem
    {
        [Key]
        [Column("unmapped_id")]
        public int UnmappedId { get; set; }

        [Column("source_file_name")]
        public string? SourceFileName { get; set; }

        [Column("project_name")]
        public string? ProjectName { get; set; }

        [Column("item_name")]
        public string ItemName { get; set; } = "";

        [Column("spec")]
        public string? Spec { get; set; }

        [Column("unit")]
        public string? Unit { get; set; }

        [Column("qty")]
        public decimal? Qty { get; set; }

        [Column("unit_price")]
        public decimal? UnitPrice { get; set; }

        [Column("status")]
        public string Status { get; set; } = "待處理";

        [Column("suggested_material_id")]
        public int? SuggestedMaterialId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}