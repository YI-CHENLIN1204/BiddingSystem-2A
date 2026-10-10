namespace 崴正.Models
{
    public class AnalysisCaseMetadataRequest
    {
        public string CaseName { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string StructureType { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal TotalFloorArea { get; set; }
        public int FloorCount { get; set; }
        public int BasementFloorCount { get; set; }
        public DateTime ProjectDate { get; set; }
    }


    public class AnalysisCaseVersionNoteRequest
    {
        public string? Note { get; set; }
    }


    public class AnalysisCaseQuantityRunRequest
    {
        public int? TopCount { get; set; }
    }


    public class AnalysisCasePriceRunRequest
    {
        public string MajorCategory { get; set; } = string.Empty;
        public int? TopCount { get; set; }
    }


    public class AnalysisCaseSummaryDto
    {
        public int AnalysisCaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string StructureType { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public decimal TotalFloorArea { get; set; }
        public int FloorCount { get; set; }
        public int BasementFloorCount { get; set; }
        public DateTime ProjectDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int VersionCount { get; set; }
        public long? CurrentVersionId { get; set; }
        public int? CurrentVersionNo { get; set; }
        public string? CurrentSourceType { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    public class AnalysisBidVersionDto
    {
        public long AnalysisBidVersionId { get; set; }
        public int VersionNo { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public int? SourceOneDProjectId { get; set; }
        public string? SourceFileName { get; set; }
        public string? SourceNote { get; set; }
        public bool IsCurrent { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    public class AnalysisBidItemDto
    {
        public long AnalysisBidItemId { get; set; }
        public int? SourceSequence { get; set; }
        public string? SheetName { get; set; }
        public int? RowNumber { get; set; }
        public string? ItemCode { get; set; }
        public string? SourceCategory1 { get; set; }
        public string? SourceCategory2 { get; set; }
        public string? SourceCategory3 { get; set; }
        public string? MajorCategory { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Spec { get; set; }
        public string? Unit { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }
        public decimal? SourceConfidence { get; set; }
        public string? Note { get; set; }
    }


    public class AnalysisCaseDetailDto
    {
        public AnalysisCaseSummaryDto Case { get; set; } = new();
        public List<AnalysisBidVersionDto> Versions { get; set; } = new();
    }


    public class AnalysisVersionDetailDto
    {
        public AnalysisCaseSummaryDto Case { get; set; } = new();
        public AnalysisBidVersionDto Version { get; set; } = new();
        public List<AnalysisBidItemDto> Items { get; set; } = new();
    }


    public class AnalysisRunSummaryDto
    {
        public long AnalysisRunId { get; set; }
        public long AnalysisBidVersionId { get; set; }
        public int VersionNo { get; set; }
        public string RunType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
