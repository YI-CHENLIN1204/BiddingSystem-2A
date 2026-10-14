using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace 崴正.Models
{
    [Table("analysis_run")]
    public class AnalysisRun
    {
        [Key]
        [Column("analysis_run_id")]
        public long AnalysisRunId { get; set; }

        [Column("analysis_bid_version_id")]
        public long AnalysisBidVersionId { get; set; }

        [Column("run_type")]
        public string RunType { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("result_json", TypeName = "jsonb")]
        public string ResultJson { get; set; } = "{}";

        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(AnalysisBidVersionId))]
        public AnalysisBidVersion AnalysisBidVersion { get; set; } = null!;
    }
}
