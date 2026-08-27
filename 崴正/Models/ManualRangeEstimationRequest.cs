namespace 崴正.Models
{
    public class ManualRangeEstimationRequest
    {
        public string ProjectType { get; set; } =
            string.Empty;

        public string StructureType { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public decimal TotalFloorArea { get; set; }

        public int FloorCount { get; set; }

        public int BasementFloorCount { get; set; }

        public string ProjectMonth { get; set; } =
            string.Empty;

        public int MaterialId { get; set; }
    }
}