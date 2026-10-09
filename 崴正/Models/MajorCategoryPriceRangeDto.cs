namespace 崴正.Models
{
    public class MajorCategoryPriceRangeRequest
    {
        public ProjectSimilarityTargetDto Target { get; set; }
            = new();

        public string MajorCategory { get; set; }
            = string.Empty;

        public decimal? ObservedAmount { get; set; }

        public int? TopCount { get; set; }
    }


    public class MajorCategoryPriceRangeResultDto
    {
        public string MajorCategory { get; set; }
            = string.Empty;

        public int CandidateProjectCount { get; set; }

        public int AvailableProjectCount { get; set; }

        public int ReferenceProjectCount { get; set; }

        public bool HasEnoughSamples { get; set; }

        public string StatusMessage { get; set; }
            = string.Empty;

        public decimal? LowerPricePerPing { get; set; }

        public decimal? MedianPricePerPing { get; set; }

        public decimal? UpperPricePerPing { get; set; }

        public decimal? LowerAmount { get; set; }

        public decimal? MedianAmount { get; set; }

        public decimal? UpperAmount { get; set; }

        public decimal? ObservedAmount { get; set; }

        public decimal? ObservedPricePerPing { get; set; }

        public string AnalysisStatus { get; set; }
            = string.Empty;

        public string TrafficLight { get; set; }
            = "Gray";

        public List<MajorCategoryPriceReferenceDto>
            References { get; set; }
                = new();
    }


    public class MajorCategoryPriceReferenceDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; }
            = string.Empty;

        public DateTime ProjectDate { get; set; }

        public decimal FloorAreaM2 { get; set; }

        public decimal FloorAreaPing { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal PricePerPing { get; set; }

        public double SimilarityScore { get; set; }

        public double AgeYears { get; set; }

        public decimal SimilarityWeight { get; set; }

        public decimal TimeWeight { get; set; }

        public decimal FinalWeight { get; set; }
    }
}
