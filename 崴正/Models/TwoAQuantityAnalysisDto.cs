namespace 崴正.Models
{
    public class TwoAQuantityAnalysisRequest
    {
        public ProjectSimilarityTargetDto Target { get; set; }
            = new();

        public int? TopCount { get; set; }
    }


    public class TwoAQuantityAnalysisResultDto
    {
        public int AnalysisCaseId { get; set; }

        public int TotalGroupCount { get; set; }

        public int AnalyzedGroupCount { get; set; }

        public int InsufficientDataCount { get; set; }

        public int SkippedGroupCount { get; set; }

        public List<TwoAQuantityAnalysisItemDto>
            Items { get; set; }
                = new();
    }


    public class TwoAQuantityAnalysisItemDto
    {
        public string MajorCategoryName { get; set; }
            = string.Empty;

        public string DetectionCategoryName { get; set; }
            = string.Empty;

        public int? DetectionCategoryId { get; set; }

        public decimal? ObservedQuantity { get; set; }

        public string? UnitName { get; set; }

        public decimal? ObservedUsagePerArea { get; set; }

        public string AnalysisStatus { get; set; }
            = string.Empty;

        public string TrafficLight { get; set; }
            = "Gray";

        public string Message { get; set; }
            = string.Empty;

        public DetectionCategoryRangeResultDto? Range
        {
            get;
            set;
        }
    }
}
