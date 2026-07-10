namespace 崴正.Models
{
    public class ProjectMaster
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public decimal? TotalFloorArea { get; set; }
        public DateTime? StartDate { get; set; }
        public bool IsDeleted { get; set; }
        public string? ProjectType { get; set; }
        public string? StructureType { get; set; }
        public string? City { get; set; }
    }
}
