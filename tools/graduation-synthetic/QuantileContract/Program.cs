using System.Reflection;
using System.Text.Json;
using 崴正.Models;
using 崴正.Services;

var payload = JsonSerializer.Deserialize<ReplayInput>(File.ReadAllText(args[0]))!;
var cases = payload.Cases;
var settings = new EstimationSetting
{
    StructureWeight = 25, AreaWeight = 30, SiteAreaWeight = 10, FloorWeight = 15,
    YearWeight = 10, CityWeight = 10, AreaDifferenceCutoffRatio = 1,
    SiteAreaDifferenceCutoffRatio = 1, GroundFloorPenaltyRatio = .1m,
    BasementFloorPenaltyRatio = .15m, YearDifferenceCutoffYears = 20
};
var projects = payload.Projects.ToDictionary(p => p.ProjectName);
var names = projects.Values.ToDictionary(p => p.ProjectId, p => p.ProjectName);
var ranked = new Dictionary<string, List<ProjectSimilarityDto>>();
int selected = 0;
foreach (var row in cases)
{
    if (!ranked.TryGetValue(row.Project, out var candidates))
    {
        var target = projects[row.Project];
        var request = new ProjectSimilarityTargetDto
        {
            ProjectType = target.ProjectType!, City = target.City!, StructureType = target.StructureType!,
            TotalFloorArea = target.TotalFloorArea!.Value, SiteArea = target.SiteArea!.Value,
            FloorCount = target.FloorCount!.Value, BasementFloorCount = target.BasementFloorCount!.Value,
            ProjectDate = target.StartDate!.Value
        };
        var pool = payload.Train.Select(id => projects[id]).Where(p =>
            p.ProjectType == target.ProjectType && (target.StructureType != "SRC" || p.StructureType == "SRC")).ToList();
        candidates = ProjectSimilarityService.ScoreCandidates(request, pool, settings);
        ranked.Add(row.Project, candidates);
    }
    var date = projects[row.Project].StartDate!.Value;
    var refs = candidates.Where(x => row.Available.Contains(names[x.ProjectId]) && x.SimilarityScore > 0)
        .Select(x => (Id: names[x.ProjectId], Score: x.SimilarityScore,
            Weight: x.SimilarityScore / 100 * Math.Pow(.5,
                Math.Abs((date - projects[names[x.ProjectId]].StartDate!.Value).TotalDays) / 365.2425 / 3)))
        .OrderByDescending(x => x.Score).ThenByDescending(x => x.Weight)
        .ThenBy(x => x.Id, StringComparer.Ordinal).Take(8).ToArray();
    if (!refs.Select(x => x.Id).SequenceEqual(row.Ids) ||
        refs.Where((x, i) => Math.Abs(x.Weight - (double)row.Weights[i]) > 1e-12).Any())
        throw new Exception($"Reference mismatch: {row.Project} {row.Scope}");
    selected += refs.Length;
}
Console.WriteLine($"PASS {selected} selected IDs, order and weights across {cases.Length} case-scope pairs.");
var flags = BindingFlags.Static | BindingFlags.NonPublic;
var quantity = typeof(DetectionCategoryRangeService).GetMethod("CalculateWeightedQuantile", flags)!;
var price = typeof(MajorCategoryPriceRangeService).GetMethod("CalculateWeightedQuantile", flags)!;
const decimal squareMetersPerPing = 3.305785m;
decimal[] probabilities = [.15m, .5m, .85m];
int checks = 0;
foreach (var row in cases)
{
    var range = row.Values.Zip(row.Weights).ToArray();
    for (int i = 0; i < probabilities.Length; i++)
    {
        var q = probabilities[i];
        decimal total;
        if (row.Scope == "structure_price")
        {
            var refs = range.Select(x => new MajorCategoryPriceReferenceDto
                { PricePerPing = x.First * squareMetersPerPing, FinalWeight = x.Second }).ToList();
            total = (decimal)price.Invoke(null, [refs, q])! * (row.Area / squareMetersPerPing);
        }
        else
        {
            var refs = range.Select(x => new DetectionCategoryRangeReferenceDto
                { UsagePerArea = x.First, FinalWeight = x.Second }).ToList();
            total = (decimal)quantity.Invoke(null, [refs, q])! * row.Area;
        }
        if (Math.Abs(Math.Round(total, 2) - row.Expected[i]) > .01m)
            throw new Exception($"Mismatch: {row.Project} {row.Scope} Q{q} => {total}, expected {row.Expected[i]}");
        checks++;
    }
}
Console.WriteLine($"PASS {checks} quantile comparisons across {cases.Length} case-scope pairs (tolerance 0.01).");

record ReplayCase(string Project, string Scope, decimal Area, decimal[] Values, decimal[] Weights,
    decimal[] Expected, string[] Ids, string[] Available);
record ReplayInput(ProjectMaster[] Projects, string[] Train, ReplayCase[] Cases);

