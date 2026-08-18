namespace 崴正.Models
{
    public class SimilarityEstimationDto
    {
        public int TargetProjectId { get; set; }

        public int MaterialId { get; set; }

        public decimal TargetFloorArea { get; set; }

        // 加權後每平方公尺材料用量
        public decimal WeightedUsagePerArea { get; set; }

        // 最終預估數量
        public decimal EstimatedQuantity { get; set; }

        // 真正有此材料歷史資料的相似案件數
        public int ReferenceProjectCount { get; set; }

        public List<SimilarityEstimationReferenceDto> References { get; set; }
            = new();
    }

    public class SimilarityEstimationReferenceDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public double SimilarityScore { get; set; }

        public decimal FloorArea { get; set; }

        // 該建案此材料總用量
        public decimal ActualQuantity { get; set; }

        // ActualQuantity / FloorArea
        public decimal UsagePerArea { get; set; }
    }
}