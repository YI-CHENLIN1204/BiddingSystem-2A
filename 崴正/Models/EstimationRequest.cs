namespace 崴正.Models
{
    public enum EstimationTargetMode
    {
        HistoricalProject,
        ManualInput
    }

    public enum EstimationScopeMode
    {
        CoreMaterials,
        SpecifiedMaterials,
        UploadedFile
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


    public class EstimationRequest
    {
        public EstimationTargetRequest Target { get; set; } =
            new();

        public EstimationScopeMode Scope { get; set; }


        // 指定材料模式使用
        public List<int> MaterialIds { get; set; } =
            new();
    }
}