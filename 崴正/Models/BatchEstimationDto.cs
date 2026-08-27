namespace 崴正.Models
{
    public class BatchEstimationDto
    {
        // =========================
        // Target
        // =========================

        public EstimationTargetMode TargetMode { get; set; }

        public int? TargetProjectId { get; set; }

        public string TargetProjectName { get; set; } =
            string.Empty;

        public ProjectSimilarityTargetDto Target
        {
            get;
            set;
        } = new();


        // =========================
        // 本次估算狀態
        // =========================

        public int CandidateProjectCount { get; set; }

        public int RequestedMaterialCount { get; set; }

        public int ResultMaterialCount { get; set; }


        // =========================
        // 每種材料的 Range 結果
        // =========================

        public List<RangeEstimationDto> Results
        {
            get;
            set;
        } = new();
    }
}