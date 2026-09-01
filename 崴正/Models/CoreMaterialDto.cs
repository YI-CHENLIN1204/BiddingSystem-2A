namespace 崴正.Models
{
    public class CoreMaterialDto
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; } = string.Empty;

        public string BaseUnit { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? ProjectType { get; set; }

        public string? StructureType { get; set; }

        public bool IsCoreEstimation { get; set; }
        public int ValidProjectCount { get; set; }
        public int HistoryRowCount { get; set; }
    }
}