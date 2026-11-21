using System.Reflection;
using 崴正.Models;
using 崴正.Services;

// Offline contract: exercises the actual calculation functions in the
// feature/1d-api-integration backend without a database connection.
// NO changes to TwoAScopePolicy or approved anomaly traffic lights.
static decimal InvokeScore(string name, ProjectSimilarityTargetDto target,
    ProjectMaster candidate, EstimationSetting settings)
{
    var method = typeof(ProjectSimilarityService).GetMethod(
        name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Missing score method: " + name);
    return (decimal)(method.Invoke(null, new object[] { target, candidate, settings })
        ?? throw new InvalidOperationException("Null score"));
}

static void Check(string name, decimal actual, decimal expected)
{
    if (actual != expected)
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    Console.WriteLine($"PASS {name}: {actual}");
}

var settings = new EstimationSetting {
    CityWeight = 10m, StructureWeight = 25m, SiteAreaWeight = 10m,
    SiteAreaDifferenceCutoffRatio = 1m
};
var target = new ProjectSimilarityTargetDto {
    ProjectType = "住宅", StructureType = "SRC", City = "臺中市",
    TotalFloorArea = 20000m, SiteArea = 2000m,
    FloorCount = 12, BasementFloorCount = 2,
    ProjectDate = new DateTime(2024, 1, 1)
};
var candidate = new ProjectMaster {
    ProjectId = 1, StructureType = "SRC", City = "臺中市",
    SiteArea = 2000m
};
Check("SRC exact structure", InvokeScore("CalculateStructureScore", target, candidate, settings), 25m);
candidate.StructureType = "RC";
Check("SRC vs RC has no structure match bonus", InvokeScore("CalculateStructureScore", target, candidate, settings), 0m);
candidate.StructureType = "SC";
Check("SRC vs SC has no structure match bonus", InvokeScore("CalculateStructureScore", target, candidate, settings), 0m);
candidate.StructureType = "SRC";
Check("same city", InvokeScore("CalculateCityScore", target, candidate, settings), 10m);
candidate.City = "高雄市";
Check("different city", InvokeScore("CalculateCityScore", target, candidate, settings), 0m);
candidate.City = "臺中市";
Check("same site area", InvokeScore("CalculateSiteAreaScore", target, candidate, settings), 10m);
candidate.SiteArea = 3000m;
Check("site area 50 percent difference", InvokeScore("CalculateSiteAreaScore", target, candidate, settings), 5m);
candidate.SiteArea = 4000m;
Check("site area at cutoff", InvokeScore("CalculateSiteAreaScore", target, candidate, settings), 0m);
candidate.SiteArea = null;
Check("missing site area", InvokeScore("CalculateSiteAreaScore", target, candidate, settings), 0m);
Console.WriteLine("Similarity contract PASS (offline actual C# calculations, NOT API/DB E2E)");
