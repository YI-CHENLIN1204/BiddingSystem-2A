namespace 崴正.Models
{
    public class MaterialMaster
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;



        // 新分類主檔 FK
        public int CategoryId { get; set; }

        public MaterialCategory MaterialCategory { get; set; }
            = null!;

        public string BaseUnit { get; set; }
            = string.Empty;
        public bool IsActive { get; set; }
        = true;
    }
}