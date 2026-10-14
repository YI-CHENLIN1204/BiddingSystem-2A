namespace 崴正.Models
{
    public class ProjectSimilarityDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public string? ProjectType { get; set; }

        public string? StructureType { get; set; }

        public string? City { get; set; }

        public decimal? TotalFloorArea { get; set; }

        public decimal? SiteArea { get; set; }

        public int? FloorCount { get; set; }
        public int? BasementFloorCount { get; set; }

        public int? ProjectYear { get; set; }

        public double TypeScore { get; set; }

        public double StructureScore { get; set; }

        public double AreaScore { get; set; }

        public double SiteAreaScore { get; set; }

        public double FloorScore { get; set; }

        public double YearScore { get; set; }

        public double CityScore { get; set; }

        public double SimilarityScore { get; set; }
    }
}