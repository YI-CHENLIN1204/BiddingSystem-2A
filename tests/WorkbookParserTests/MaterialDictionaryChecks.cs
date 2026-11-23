using System.Text.Json;
using 崴正.Services;

internal static class MaterialDictionaryChecks
{
    public static void Run()
    {
        VerifyIndependentPanel();
        var crossSource = new SafeMaterialDictionary([
            new DictionaryMaterial("耐蝕管 | ASTM A312", 42, 10, true),
            new DictionaryMaterial("耐蝕管 | ASTM A312 供料及安裝", 42, 10, true)
        ]);
        bool CompatiblePipe(string? unit, int id) => unit == "m" && id == 10;
        if (crossSource.Decide("耐蝕管", "ＡＳＴＭ　Ａ３１２", "m", 1, CompatiblePipe).Match?.MaterialId != 42 ||
            crossSource.Decide("耐蝕管異名", "ASTM A312", "m", 1, CompatiblePipe).Match is not null ||
            crossSource.Decide("耐蝕管", "ASTM A312 供料及安裝", "m", 1, CompatiblePipe).Code != "COMPOSITE_QUANTITY_EXCLUDED" ||
            crossSource.SuggestForReview("耐蝕管", "ASTM A312 供料及安裝", "m", 1, CompatiblePipe) is not null)
            throw new InvalidOperationException("cross-source alias and visible composite guard");
        foreach (var (name, wordingSpec, excluded) in new (string Name, string? Spec, bool Excluded)[]
        {
            ("鋼筋材料及加工綁紮", "SD420", true),
            ("預拌混凝土", "含泵送與澆置", true),
            ("鋼筋供料及施工分開計價", "SD420", false),
            ("鋼筋材料", "供料與安裝分別計價", false),
            ("預拌混凝土", "不含泵送與澆置", false),
            ("預拌混凝土", "泵送規格 3000 PSI", false),
            ("鋼筋材料", "加工設備", false),
            ("鋼筋供料及施工分開計價", "連工帶料", true),
        })
        {
            if (TwoAScopePolicy.IsExplicitSupplyAndInstall(name, wordingSpec) != excluded)
                throw new InvalidOperationException($"composite wording: {name} {wordingSpec}");
            Console.WriteLine($"PASS: composite wording: {name} {wordingSpec}");
        }
        Console.WriteLine("PASS: cross-source formatted exact alias, unknown alias and composite guard");
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

    private static void VerifyIndependentPanel()
    {
        byte[] contents = System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "material-alias-panel.json")).Replace("\r\n", "\n"));
        const string expected = "9258d7c12dd9232f6b8adc404dcb2215e9b655dd66b1d29b9feecc0d430a084e";
        if (!Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(contents))
                .Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("independent alias panel digest mismatch");
        var panel = JsonSerializer.Deserialize<ReplayInput>(contents)!;
        var matcher = new SafeMaterialDictionary(panel.Dictionary);
        bool Compatible(string? unit, int id) => panel.Units.GetValueOrDefault(unit ?? "") == id;
        foreach (var row in panel.Rows)
        {
            var actual = matcher.Decide(row.Name, row.Spec, row.Unit, row.Quantity, Compatible);
            var candidate = matcher.SuggestForReview(row.Name, row.Spec, row.Unit, row.Quantity, Compatible);
            if (actual.Match?.MaterialId != row.ExpectedAuto || candidate != row.ExpectedCandidate)
                throw new InvalidOperationException("independent alias panel: " + row.Id);
            Console.WriteLine("PASS: independent alias panel: " + row.Id);
        }
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
            int? candidate = next.Code == "UNVERIFIED_SPEC"
                ? dictionary.SuggestForReview(row.Name, row.Spec, row.Unit, row.Quantity,
                    (unit, id) => input.Units.GetValueOrDefault(unit ?? "") == id)
                : null;
            bool priceEligible = TwoAScopePolicy.ResolveEffectiveMajorCategory(null, row.Category, null, null) == "結構體工程" &&
                (row.Amount > 0 || (row.Quantity > 0 && row.UnitPrice > 0));
            return new { row.Id, Before = old?.MaterialId, After = next.Match?.MaterialId, next.Code, Candidate = candidate,
                PriceEligible = priceEligible, ExplicitComposite = TwoAScopePolicy.IsExplicitSupplyAndInstall(row.Name, row.Spec) };
        }).ToArray();
        File.WriteAllText(outputPath, JsonSerializer.Serialize(records));
    }

    private sealed record ReplayInput(DictionaryMaterial[] Dictionary, ReplayRow[] Rows, Dictionary<string, int> Units);
    private sealed record ReplayRow(string Id, string Name, string Spec, string Unit, decimal? Quantity,
        int? ExpectedAuto = null, int? ExpectedCandidate = null, string? Category = null,
        decimal? Amount = null, decimal? UnitPrice = null);
}
