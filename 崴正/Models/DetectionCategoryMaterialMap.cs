namespace 崴正.Models
{
    public class DetectionCategoryMaterialMap
    {
        public int DetectionCategoryId { get; set; }

        public int MaterialId { get; set; }

        public bool IsActive { get; set; }
            = true;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DetectionCategory DetectionCategory
        {
            get;
            set;
        } = null!;

        public MaterialMaster Material { get; set; }
            = null!;
    }
}
