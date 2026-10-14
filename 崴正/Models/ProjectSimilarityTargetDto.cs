namespace 崴正.Models
{
    public class ProjectSimilarityTargetDto
    {
        public string ProjectType { get; set; } =
            string.Empty;

        public string StructureType { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public decimal TotalFloorArea { get; set; }

        public decimal SiteArea { get; set; }

        public int FloorCount { get; set; }

        public int BasementFloorCount { get; set; }

        public DateTime ProjectDate { get; set; }
    }
}