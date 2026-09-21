namespace 崴正.Models
{
    public class RangeEstimationDto
    {
        // =========================
        // 估算目標
        // =========================

        public EstimationTargetMode TargetMode { get; set; }

        // 歷史建案才有 ProjectId
        // 新案直接輸入時為 null
        public int? TargetProjectId { get; set; }

        public string TargetProjectName { get; set; } =
            string.Empty;

        public string TargetProjectType { get; set; } =
            string.Empty;

        public string TargetStructureType { get; set; } =
            string.Empty;

        public string TargetCity { get; set; } =
            string.Empty;

        public decimal TargetFloorArea { get; set; }

        public int TargetFloorCount { get; set; }

        public int TargetBasementFloorCount { get; set; }

        public DateTime TargetProjectDate { get; set; }


        // =========================
        // 標準材料
        // =========================

        public int MaterialId { get; set; }

        public string MaterialName { get; set; } =
            string.Empty;

        public string UnitName { get; set; } =
            string.Empty;

        public string UsagePerAreaUnit { get; set; } =
            string.Empty;


        // =========================
        // 樣本狀態
        // =========================

        public bool HasEnoughSamples { get; set; }

        public string StatusMessage { get; set; } =
            string.Empty;

        public int CandidateProjectCount { get; set; }

        public int MaterialAvailableProjectCount { get; set; }

        public int ReferenceProjectCount { get; set; }


        // =========================
        // 本次使用的參數
        // =========================

        public int TopK { get; set; }

        public int MinimumSampleCount { get; set; }

        public decimal LowerQuantile { get; set; }

        public decimal MedianQuantile { get; set; }

        public decimal UpperQuantile { get; set; }

        public decimal SimilarityExponent { get; set; }

        public decimal TimeHalfLifeYears { get; set; }


        // =========================
        // 單位面積合理區間
        // =========================

        public decimal? LowerUsagePerArea { get; set; }

        public decimal? MedianUsagePerArea { get; set; }

        public decimal? UpperUsagePerArea { get; set; }


        // =========================
        // 換算成目標建案總用量
        // =========================

        public decimal? LowerQuantity { get; set; }

        public decimal? MedianQuantity { get; set; }

        public decimal? UpperQuantity { get; set; }


        // =========================
        // 本次估算參考建案
        // =========================

        public List<RangeEstimationReferenceDto> References
        {
            get;
            set;
        } = new();
    }
}