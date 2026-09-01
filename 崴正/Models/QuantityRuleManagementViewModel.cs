namespace 崴正.Models
{
    public class EstimationSettingsFormViewModel
    {
        public int TopK { get; set; }

        public int MinimumSampleCount { get; set; }


        public decimal LowerQuantilePercent { get; set; }

        public decimal MedianQuantilePercent { get; set; }

        public decimal UpperQuantilePercent { get; set; }


        public decimal SimilarityExponent { get; set; }

        public decimal TimeHalfLifeYears { get; set; }

        public decimal MinimumSimilarityScore { get; set; }


        public decimal StructureWeight { get; set; }

        public decimal AreaWeight { get; set; }

        public decimal FloorWeight { get; set; }

        public decimal YearWeight { get; set; }

        public decimal CityWeight { get; set; }


        public decimal AreaDifferenceCutoffPercent { get; set; }

        public decimal GroundFloorPenaltyPercent { get; set; }

        public decimal BasementFloorPenaltyPercent { get; set; }


        public int YearDifferenceCutoffYears { get; set; }


        public DateTime UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }
    }


    public class QuantityRuleManagementViewModel
    {
        public EstimationSettingsFormViewModel
            EstimationSettings { get; set; }
                = new();


        public List<CoreMaterialDto>
            CoreMaterials { get; set; }
                = new();
    }
}