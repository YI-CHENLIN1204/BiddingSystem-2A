namespace 崴正.Models
{
    public enum EstimationTargetMode
    {
        HistoricalProject,
        ManualInput
    }



    public class EstimationTargetRequest
    {
        public EstimationTargetMode TargetMode { get; set; }


        // ==========================================
        // HistoricalProject 模式使用
        // ==========================================

        public int? HistoricalProjectId { get; set; }


        // ==========================================
        // ManualInput 模式使用
        // ==========================================

        public string? ProjectType { get; set; }

        public string? StructureType { get; set; }

        public string? City { get; set; }

        public decimal? TotalFloorArea { get; set; }

        public int? FloorCount { get; set; }

        public int? BasementFloorCount { get; set; }

        public string? ProjectMonth { get; set; }
    }

}