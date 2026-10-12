using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace 崴正.Models
{
    [Table("analysis_case")]
    public class AnalysisCase
    {
        [Key]
        [Column("analysis_case_id")]
        public int AnalysisCaseId { get; set; }

        [Column("case_name")]
        public string CaseName { get; set; } = string.Empty;

        [Column("project_type")]
        public string ProjectType { get; set; } = string.Empty;

        [Column("structure_type")]
        public string StructureType { get; set; } = string.Empty;

        [Column("city")]
        public string City { get; set; } = string.Empty;

        [Column("total_floor_area")]
        public decimal TotalFloorArea { get; set; }

        [Column("site_area")]
        public decimal SiteArea { get; set; }

        [Column("floor_count")]
        public int FloorCount { get; set; }

        [Column("basement_floor_count")]
        public int BasementFloorCount { get; set; }

        [Column("project_date", TypeName = "date")]
        public DateTime ProjectDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", TypeName = "timestamp with time zone")]
        public DateTime UpdatedAt { get; set; }

        public ICollection<AnalysisBidVersion> Versions { get; set; }
            = new List<AnalysisBidVersion>();
    }
}
