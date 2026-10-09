namespace 崴正.Models
{
    public class DetectionCategory
    {
        public int DetectionCategoryId { get; set; }

        public string MajorCategoryName { get; set; }
            = string.Empty;

        public string DetectionCategoryName { get; set; }
            = string.Empty;

        public int UnitId { get; set; }

        public bool IsActive { get; set; }
            = true;

        public int SortOrder { get; set; }

        public UnitMaster UnitMaster { get; set; }
            = null!;

        public ICollection<DetectionCategoryMaterialMap>
            MaterialMappings { get; set; }
                = new List<DetectionCategoryMaterialMap>();
    }
}
