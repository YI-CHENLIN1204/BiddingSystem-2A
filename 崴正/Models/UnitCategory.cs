namespace 崴正.Models
{
    public class UnitCategory
    {
        public int UnitCategoryId { get; set; }

        public string CategoryName { get; set; }
            = string.Empty;

        public bool AllowConversion { get; set; }

        public bool IsActive { get; set; }
            = true;

        public int SortOrder { get; set; }

        public ICollection<UnitMaster> Units { get; set; }
            = new List<UnitMaster>();
    }
}