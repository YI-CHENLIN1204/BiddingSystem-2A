namespace 崴正.Models
{
    public class UnitMaster
    {
        public int UnitId { get; set; }

        public string UnitName { get; set; }
            = string.Empty;

        public int UnitCategoryId { get; set; }

        public decimal? ToBaseFactor { get; set; }

        public bool IsBaseUnit { get; set; }

        public bool IsActive { get; set; }
            = true;

        public int SortOrder { get; set; }

        public UnitCategory UnitCategory { get; set; }
            = null!;
    }
}