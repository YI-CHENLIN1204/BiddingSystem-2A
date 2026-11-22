namespace 崴正.Models;

public record BulkMaterialProfile(string Key, string Name, string Unit, string Basis, string[] Structures, string Notice);
public record BulkMaterialObservedQuantity(string ProfileKey, int MaterialId, string MaterialName,
    string Unit, decimal? ObservedQuantity, int SourceRowCount);
public record BulkMaterialOption(int MaterialId, string MaterialName, string UnitName, string CategoryName);
public record BulkMaterialCandidate(int ProjectId, string ProjectName, string ProjectType, string StructureType,
    decimal FloorArea, decimal SiteArea, int FloorCount, int BasementFloorCount, DateTime ProjectDate,
    string HistoryStatus, bool IsDeleted, double SimilarityScore, string City = "");
public record BulkMaterialHistoricalRow(int TransactionId, int ProjectId, long? FinalBidItemId, decimal Quantity,
    int? SourceProjectId, int? SourceMaterialId, string? MappingStatus, bool IsIgnored,
    string ItemName, string? SourceUnit, decimal? SourceQuantity, string? MajorCategory,
    string? SourceCategory1 = null, string? SourceCategory2 = null, string? SourceCategory3 = null,
    bool HistoryIsOutlier = false, bool HasHistoricalTransaction = true);
public record BulkMaterialSource(int ProjectId, long FinalBidItemId);
public record BulkMaterialExclusion(int ProjectId, string Reason);
public record BulkMaterialReference(int ProjectId, string ProjectName, string StructureType, DateTime ProjectDate,
    decimal BasisArea, decimal Quantity, decimal UsagePerArea, int SourceRowCount, double SimilarityScore,
    decimal FinalWeight, int FinalBidOnlyRowCount);

public class BulkMaterialEstimateRequest
{
    public string ProfileKey { get; set; } = string.Empty;
    public int MaterialId { get; set; }
    public ProjectSimilarityTargetDto Target { get; set; } = new();
    public int? TopCount { get; set; }
    public int? ExcludeProjectId { get; set; }
    public decimal? ObservedQuantity { get; set; }
}

public class BulkMaterialEstimateResult
{
    public string ProfileKey { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string BasisLabel { get; set; } = string.Empty;
    public string RangeKind { get; set; } = "WeightedHistoricalReference";
    public string SourceKind { get; set; } = "ApprovedFinalBidWithOptionalHistoryCrossCheck";
    public bool FormalAnomalyEnabled { get; set; } = false;
    public bool PriceComparisonSupported { get; set; } = false;
    public string PriceLimitation { get; set; } = "歷史資料沒有一致的純材料／加工安裝計價標記，暫不產生單價區間。";
    public bool HasEnoughSamples { get; set; }
    public string Status { get; set; } = "InsufficientSamples";
    public string Message { get; set; } = string.Empty;
    public int CandidateProjectCount { get; set; }
    public int AvailableProjectCount { get; set; }
    public int ReferenceProjectCount { get; set; }
    public int RecognizableSyntheticNameProjectCount { get; set; }
    public int MinimumSampleCount { get; set; }
    public decimal EffectiveSampleSize { get; set; }
    public decimal LargestWeightSharePercent { get; set; }
    public decimal LowerQuantile { get; set; }
    public decimal MedianQuantile { get; set; }
    public decimal UpperQuantile { get; set; }
    public decimal? LowerQuantity { get; set; }
    public decimal? MedianQuantity { get; set; }
    public decimal? UpperQuantity { get; set; }
    public decimal? ObservedQuantity { get; set; }
    public List<BulkMaterialReference> References { get; set; } = [];
    public List<BulkMaterialExclusion> ExcludedProjects { get; set; } = [];
    public string[] Warnings { get; set; } = [];
}

