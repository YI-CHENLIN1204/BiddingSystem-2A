namespace 崴正.Models
{
    public class CoreMaterialUpdateRequest
    {
        public int MaterialId { get; set; }

        public string? ProjectType { get; set; }

        public string? StructureType { get; set; }

        public bool IsCoreEstimation { get; set; }
    }
}