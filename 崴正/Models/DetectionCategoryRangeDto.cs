namespace 崴正.Models
{
    public class DetectionCategoryRangeRequest
    {
        public ProjectSimilarityTargetDto Target { get; set; }
            = new();

        public int? TopCount { get; set; }
    }


    public class DetectionCategoryRangeResultDto
    {
        public int DetectionCategoryId { get; set; }

        public string MajorCategoryName { get; set; }
            = string.Empty;

        public string DetectionCategoryName { get; set; }
            = string.Empty;

        public string UnitName { get; set; }
            = string.Empty;

        public string UsagePerAreaUnit { get; set; }
            = string.Empty;

        public int CandidateProjectCount { get; set; }

        public int AvailableProjectCount { get; set; }

        public int ReferenceProjectCount { get; set; }

        // Descriptive diagnostics only; neither is a prediction confidence rate.
        public decimal EffectiveSampleSize { get; set; }
        public decimal LargestWeightSharePercent { get; set; }

        public int SkippedTransactionCount { get; set; }

        public bool HasEnoughSamples { get; set; }

        public string StatusMessage { get; set; }
            = string.Empty;

        public decimal? LowerUsagePerArea { get; set; }

        public decimal? MedianUsagePerArea { get; set; }

        public decimal? UpperUsagePerArea { get; set; }

        public decimal? LowerQuantity { get; set; }

        public decimal? MedianQuantity { get; set; }

        public decimal? UpperQuantity { get; set; }

        public List<DetectionCategoryRangeReferenceDto>
            References { get; set; }
                = new();
    }


    public class DetectionCategoryRangeReferenceDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; }
            = string.Empty;

        public DateTime ProjectDate { get; set; }

        public decimal FloorArea { get; set; }

        public decimal ActualQuantity { get; set; }

        public decimal UsagePerArea { get; set; }

        public double SimilarityScore { get; set; }

        public double AgeYears { get; set; }

        public decimal SimilarityWeight { get; set; }

        public decimal TimeWeight { get; set; }

        public decimal FinalWeight { get; set; }
    }
}
