namespace 崴正.Models
{
    public class EstimationProjectOptionDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public string ProjectType { get; set; } = string.Empty;

        public string StructureType { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
    }


    public class EstimationMaterialOptionDto
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; } = string.Empty;

        public string UnitName { get; set; } = string.Empty;

        public string? Category { get; set; }
    }


    public class EstimationPageViewModel
    {
        public List<EstimationProjectOptionDto>
            HistoricalProjects { get; set; } = new();

        public List<EstimationMaterialOptionDto>
            Materials { get; set; } = new();
        public List<EstimationCoreMaterialOptionDto>
            CoreMaterials { get; set; }= new();
    }

    public class EstimationCoreMaterialOptionDto
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;

        public string UnitName { get; set; }
            = string.Empty;

        public string? Category { get; set; }

        public string ProjectType { get; set; }
            = string.Empty;

        public string StructureType { get; set; }
            = string.Empty;
    }
}