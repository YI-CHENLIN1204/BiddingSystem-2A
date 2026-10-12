namespace 崴正.Models
{
    public class EstimationSettingsUpdateRequest
    {
        public int TopK { get; set; }

        public int MinimumSampleCount { get; set; }

        public decimal LowerQuantile { get; set; }

        public decimal MedianQuantile { get; set; }

        public decimal UpperQuantile { get; set; }

        public decimal SimilarityExponent { get; set; }

        public decimal TimeHalfLifeYears { get; set; }

        public decimal MinimumSimilarityScore { get; set; }

        public decimal StructureWeight { get; set; }

        public decimal AreaWeight { get; set; }

        public decimal SiteAreaWeight { get; set; }

        public decimal FloorWeight { get; set; }

        public decimal YearWeight { get; set; }

        public decimal CityWeight { get; set; }

        public decimal AreaDifferenceCutoffRatio { get; set; }

        public decimal SiteAreaDifferenceCutoffRatio { get; set; }

        public decimal GroundFloorPenaltyRatio { get; set; }

        public decimal BasementFloorPenaltyRatio { get; set; }

        public int YearDifferenceCutoffYears { get; set; }
    }
}