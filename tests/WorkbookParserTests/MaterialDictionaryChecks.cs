using System.Text.Json;
using 崴正.Services;

internal static class MaterialDictionaryChecks
{
    public static void Run()
    {
        const string title = "竹節鋼筋";
        const string spec = "SD420";
        var legacy = new DictionaryMaterial(title, 1, 10, true);
        var approved = new DictionaryMaterial(title + " | " + spec, 1, 10, true);
        var concrete = new DictionaryMaterial("預拌混凝土 | 280 kgf/cm²", 2, 11, true);
        var dictionary = new SafeMaterialDictionary([legacy, approved, concrete]);

        bool Compatible(string? unit, int id) => (unit, id) is ("kg" or "t", 10) or ("m3", 11);
        SafeMappingDecision Decide(string? name, string? grade, string? unit = "kg", decimal? qty = 2) =>
            dictionary.Decide(name, grade, unit, qty, Compatible);
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + label);
            Console.WriteLine("PASS: " + label);
        }

        Check(Decide(" 竹節鋼筋　", "ｓｄ４２０", "t").Match?.MaterialId == 1, "qualified alias, width/case/whitespace and convertible unit");
        Check(Decide("預拌混凝土", "280 kgf/cm2", "m3").Match?.MaterialId == 2, "qualified concrete strength");
        Check(Decide("預拌混凝土", "350 kgf/cm2", "m3").Match is null, "different concrete strength is pending");
        Check(Decide("未知別名", spec).Code == "NO_DICTIONARY_MATCH", "unseen alias");
        Check(Decide("材料", spec).Match is null && Decide("A-01", spec).Match is null, "generic names and codes");
        Check(Decide(title, null).Match is null && Decide(title, "N/A").Match is null, "specification required");
        Check(Decide(title, spec, null).Match is null && Decide(title, spec, "unknown").Match is null, "missing and unknown units");
        Check(new decimal?[] {null, 0, -1}.All(q => Decide(title, spec, qty:q).Match is null), "invalid quantity");
        Check(new SafeMaterialDictionary([approved with {IsActive = false}])
            .Decide(title, spec, "kg", 1, Compatible).Code == "MATERIAL_UNAVAILABLE", "inactive or missing material");
        Check(new SafeMaterialDictionary([approved, legacy with {MaterialId = 2}])
            .Decide(title, spec, "kg", 1, Compatible).Code == "DICTIONARY_CONFLICT", "more than one material across matching aliases");
        Check(new SafeMaterialDictionary([approved, approved with {MaterialId = 2}])
            .Decide(title, spec, "kg", 1, Compatible).Code == "DICTIONARY_CONFLICT", "same dictionary term with different materials");
        Check(new SafeMaterialDictionary([legacy]).Decide(title, spec, "kg", 1, Compatible).Match is null, "legacy alias is only a candidate");
    }

    public static void Replay(string inputPath, string outputPath)
    {
        var input = JsonSerializer.Deserialize<ReplayInput>(File.ReadAllText(inputPath))!;
        var dictionary = new SafeMaterialDictionary(input.Dictionary);
        var baseline = input.Dictionary.OrderByDescending(x => x.Keyword.Length).ToArray();
        var records = input.Rows.Select(row =>
        {
            var old = baseline.FirstOrDefault(x => row.Name.Contains(x.Keyword, StringComparison.OrdinalIgnoreCase)
                || row.Spec.Contains(x.Keyword, StringComparison.OrdinalIgnoreCase));
            var next = dictionary.Decide(row.Name, row.Spec, row.Unit, row.Quantity,
                (unit, id) => input.Units.GetValueOrDefault(unit ?? "") == id);
            return new { row.Id, Before = old?.MaterialId, After = next.Match?.MaterialId, next.Code };
        }).ToArray();
        File.WriteAllText(outputPath, JsonSerializer.Serialize(records));
    }

    private sealed record ReplayInput(DictionaryMaterial[] Dictionary, ReplayRow[] Rows, Dictionary<string, int> Units);
    private sealed record ReplayRow(string Id, string Name, string Spec, string Unit, decimal? Quantity);
}
