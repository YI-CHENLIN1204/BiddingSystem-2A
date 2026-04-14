namespace 崴正.Models
{
    public class MaterialMaster
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string BaseUnit { get; set; } = string.Empty;
    }
}