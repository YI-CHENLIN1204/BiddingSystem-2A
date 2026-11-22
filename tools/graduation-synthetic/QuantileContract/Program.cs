using System.Reflection;
using System.Text.Json;
using 崴正.Models;
using 崴正.Services;

var cases = JsonSerializer.Deserialize<ReplayCase[]>(File.ReadAllText(args[0]))!;
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

record ReplayCase(string Project, string Scope, decimal Area, decimal[] Values, decimal[] Weights, decimal[] Expected);
