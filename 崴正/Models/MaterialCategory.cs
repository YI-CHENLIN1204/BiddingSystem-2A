namespace 崴正.Models
{
    public class MaterialCategory
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public bool IsActive { get; set; }
            = true;

        public int SortOrder { get; set; }

        public ICollection<MaterialMaster>
            Materials { get; set; }
                = new List<MaterialMaster>();
    }
}