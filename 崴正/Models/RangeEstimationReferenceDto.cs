namespace 崴正.Models
{
    public class RangeEstimationReferenceDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } =
            string.Empty;

        public DateTime ProjectDate { get; set; }

        public double SimilarityScore { get; set; }

        public decimal FloorArea { get; set; }

        public decimal ActualQuantity { get; set; }

        public decimal UsagePerArea { get; set; }

        public double AgeYears { get; set; }

        public decimal SimilarityWeight { get; set; }

        public decimal TimeWeight { get; set; }

        public decimal FinalWeight { get; set; }
    }
}