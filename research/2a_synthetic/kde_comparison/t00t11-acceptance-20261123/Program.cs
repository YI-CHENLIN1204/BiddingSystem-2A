using System.Text.Json;
using 崴正.Data;
using 崴正.Services;

var enabled = new Category(true);
var disabled = new Category(false);
var service = new UnitConversionService(new ApplicationDbContext(new Unit[]
{
    new(1, "kg", 1, enabled, 1m, true),
    new(2, "t", 1, enabled, 1000m, false),
    new(3, "zero-factor", 1, enabled, 0m, false),
    new(4, "negative-factor", 1, enabled, -2m, false),
    new(5, "m2", 2, enabled, 1m, true),
    new(6, "m3", 3, enabled, 1m, true),
    new(7, "piece", 4, disabled, 1m, true),
    new(8, "box", 4, disabled, 12m, false),
    new(9, "missing-factor", 1, enabled, null, false),
    new(10, "no-base", 5, enabled, 1m, false)
}));

var cases = new (string Id, Func<UnitConversionResult> Convert, decimal? Expected)[]
{
    ("fractional_tonne_amount", () => service.Convert(0.125m, 3200m, " T ", 1), 1000m),
    ("fractional_kg_amount", () => service.Convert(0.125m, 3200m, "K G", 2), 0.001m),
    ("base_normalization", () => service.ConvertQuantityToBaseUnit(2m, " T "), 1000m),
    ("base_missing", () => service.ConvertQuantityToBaseUnit(2m, "no-base"), null),
    ("base_source_unknown", () => service.ConvertQuantityToBaseUnit(2m, "unknown"), null),
    ("area_vs_volume", () => service.Convert(2m, 300m, "m2", 6), null),
    ("volume_vs_area", () => service.Convert(2m, 300m, "m3", 5), null),
    ("disabled_same_unit", () => service.Convert(2m, 300m, "box", 8), 1m),
    ("disabled_cross_unit", () => service.Convert(2m, 300m, "box", 7), null),
    ("missing_source_factor", () => service.Convert(2m, 300m, "missing-factor", 1), null),
    ("zero_target_factor", () => service.Convert(2m, 300m, "kg", 3), null),
    ("zero_source_factor", () => service.Convert(2m, 300m, "zero-factor", 1), null),
    ("negative_source_factor", () => service.Convert(2m, 300m, "negative-factor", 1), null)
};

var failed = 0;
foreach (var test in cases)
{
    try
    {
        var result = test.Convert();
        var passed = result.Success == test.Expected.HasValue
            && (!result.Success || result.ConversionFactor == test.Expected);
        if (test.Id.StartsWith("fractional_", StringComparison.Ordinal))
            passed &= result.Quantity * result.UnitPrice == 400m;
        failed += passed ? 0 : 1;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            test.Id, Passed = passed, test.Expected, result.Success,
            result.ConversionFactor, result.Quantity, result.UnitPrice
        }));
    }
    catch (Exception error)
    {
        failed++;
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            test.Id, Passed = false, Error = error.GetType().Name
        }));
    }
}
return failed == 0 ? 0 : 1;
