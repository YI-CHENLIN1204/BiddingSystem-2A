namespace 崴正.Models;

public record MaterialCapabilityMaterial(int MaterialId, string Name, string Unit);
public record MaterialCapabilityHistory(int MaterialId, int ProjectId, string StructureType, string ProjectName);
public record MaterialCapabilityMapping(int MaterialId, string CategoryName, string MajorCategory, string Unit);
public record MaterialCandidateDefinition(string Key, string Name, string Unit, string[] NameHints,
    string PatternBasis, string[] MissingEngineeringConditions, bool ExistingModel = false,
    string[]? SpecificationKeys = null);

public record MaterialCapabilityItem(string Key, string Name, string StandardUnit, string Status,
    string StatusLabel, bool CurrentModelSupported, bool FormalAnomalyEnabled,
    bool SpecificationEstimateAvailable, string PatternBasis, int NameCandidateMaterialCount,
    int MappedMaterialCount, int IncompatibleUnitMaterialCount, int HistoricalProjectCount,
    int RecognizableSyntheticNameProjectCount, int MinimumSampleCount,
    string[] MissingEngineeringConditions, string[] Reasons, string[] SpecificationKeys);

public record MaterialCapabilityResponse(string? StructureType, string Notice, MaterialCapabilityItem[] Items);

public record MaterialSpecificationProfile(string Key, string MaterialKey, string ProductName,
    string BasisLabel, string OutputUnit, string RateKind, decimal LowerRate, decimal UpperRate,
    string RateUnit, string SourceUrl, string SourceVersion, string[] Conditions);

public class MaterialSpecificationEstimateRequest
{
    public string ProfileKey { get; set; } = string.Empty;
    public decimal WorkAreaM2 { get; set; }
    public decimal? ObservedQuantity { get; set; }
}

public record MaterialSpecificationEstimate(string ProfileKey, string ProductName, decimal WorkAreaM2,
    string BasisLabel, string Unit, decimal LowerQuantity, decimal UpperQuantity,
    string RangeKind, bool FormalAnomalyEnabled, string ReviewStatus, string Message,
    string SourceUrl, string SourceVersion, string[] Conditions);
