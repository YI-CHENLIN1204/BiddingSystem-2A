using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace 崴正.Models
{
    [Table("analysis_bid_version")]
    public class AnalysisBidVersion
    {
        [Key]
        [Column("analysis_bid_version_id")]
        public long AnalysisBidVersionId { get; set; }

        [Column("analysis_case_id")]
        public int AnalysisCaseId { get; set; }

        [Column("version_no")]
        public int VersionNo { get; set; }

        [Column("source_type")]
        public string SourceType { get; set; } = string.Empty;

        [Column("source_1d_project_id")]
        public int? SourceOneDProjectId { get; set; }

        [Column("source_file_name")]
        public string? SourceFileName { get; set; }

        [Column("source_file_hash")]
        public string? SourceFileHash { get; set; }

        [Column("source_note")]
        public string? SourceNote { get; set; }

        [Column("is_current")]
        public bool IsCurrent { get; set; }

        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AnalysisCaseId))]
        public AnalysisCase AnalysisCase { get; set; } = null!;

        public ICollection<AnalysisBidItem> Items { get; set; }
            = new List<AnalysisBidItem>();

        public ICollection<AnalysisRun> Runs { get; set; }
            = new List<AnalysisRun>();
    }
}
