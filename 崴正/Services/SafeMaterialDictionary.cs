using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;

namespace 崴正.Services;

public sealed record DictionaryMaterial(string Keyword, int MaterialId, int UnitId, bool IsActive);
public sealed record SafeMappingDecision(DictionaryMaterial? Match, string? Code, string? Message);

/// <summary>Explicit name/spec aliases authorize Auto; legacy terms can only veto it.</summary>
public sealed class SafeMaterialDictionary
{
    private readonly ILookup<string, DictionaryMaterial> _terms;

    public SafeMaterialDictionary(IEnumerable<DictionaryMaterial> entries)
    {
        _terms = entries.ToLookup(x => Normalize(x.Keyword), StringComparer.Ordinal);
    }

    internal static IEnumerable<DictionaryMaterial> Read(ApplicationDbContext context) =>
        (from entry in context.ItemDictionaries.AsNoTracking()
         join material in context.MaterialMasters.AsNoTracking()
             on entry.Material_Id equals material.MaterialId into materials
         from material in materials.DefaultIfEmpty()
         select new DictionaryMaterial(entry.Keyword, entry.Material_Id,
             material == null ? 0 : material.UnitId, material != null && material.IsActive)).ToList();

    public static string Normalize(string? value) =>
        Regex.Replace((value ?? "").Normalize(NormalizationForm.FormKC).Trim(), @"\s+", " ")
            .ToUpperInvariant();

    private static bool Informative(string value) =>
        value.Count(char.IsLetterOrDigit) >= 2 && value.Any(char.IsLetter) &&
        value is not ("其他" or "一般" or "項目" or "材料" or "工程" or "規格" or "追加" or "不詳" or "無規格" or "NA" or "N/A" or "NONE" or "UNKNOWN");

    public static string? QualifiedKeyword(string? itemName, string? spec)
    {
        string name = Normalize(itemName);
        string specification = Normalize(spec);
        // Disallow free-form row identifiers and prevent ambiguous field boundaries.
        return Informative(name) && Informative(specification) &&
               !TwoAScopePolicy.ExcludesMaterialQuantity(itemName, spec) &&
               !Regex.IsMatch(name, @"^[A-Z]*[-_. ]*\d[\dA-Z_. -]*$") &&
               !name.Contains('|') && !specification.Contains('|') && name != specification
            ? $"{name} | {specification}" : null;
    }

    public SafeMappingDecision Decide(string? itemName, string? spec, string? unit, decimal? quantity,
        Func<string?, int, bool> canConvert)
    {
        if (TwoAScopePolicy.IsExplicitSupplyAndInstall(itemName, spec))
            return new(null, "COMPOSITE_QUANTITY_EXCLUDED", "明示供料施工，排除純材料數量；依原工程大項規則核對金額。");

        string? key = QualifiedKeyword(itemName, spec);
        if (key is null)
            return new(null, "MAPPING_CONTEXT_REQUIRED", "工項名稱或規格不足，請人工對應。");

        var exact = _terms[key].ToList();
        var candidateIds = Candidates(itemName, spec, key)
            .Select(entry => entry.MaterialId).Distinct().Take(2).Count();

        if (candidateIds > 1)
            return new(null, "DICTIONARY_CONFLICT", "工項或規格指向不同材料，請人工核對字典。");

        if (exact.Count == 0)
            return new(null, candidateIds == 0 ? "NO_DICTIONARY_MATCH" : "UNVERIFIED_SPEC",
                "找不到已核定的完整工項與規格組合，請人工對應。");

        var material = exact[0];
        if (!material.IsActive)
            return new(null, "MATERIAL_UNAVAILABLE", "字典對應材料已停用或不存在。");

        if (!quantity.HasValue || quantity <= 0)
            return new(null, "INVALID_MAPPING_QUANTITY", "數量空白或小於等於 0，請人工覆核。");

        if (!canConvert(unit, material.UnitId))
            return new(null, "MAPPING_UNIT_MISMATCH", "標單單位空白、未定義或無法換算至標準材料單位。");

        return new(material, null, null);
    }

    public int? SuggestForReview(string? name, string? spec, string? unit, decimal? quantity,
        Func<string?, int, bool> canConvert)
    {
        string? key = QualifiedKeyword(name, spec);
        if (key is null || quantity is null or <= 0) return null;
        var candidates = Candidates(name, spec, key).DistinctBy(x => x.MaterialId).Take(2).ToArray();
        return candidates is [var candidate] && candidate.IsActive && canConvert(unit, candidate.UnitId)
            ? candidate.MaterialId : null;
    }

    private IEnumerable<DictionaryMaterial> Candidates(string? itemName, string? spec, string key)
    {
        string name = Normalize(itemName);
        string specification = Normalize(spec);
        return _terms.Where(term => !term.Key.Contains('|') && Informative(term.Key) &&
                (name.Contains(term.Key, StringComparison.Ordinal) ||
                 specification.Contains(term.Key, StringComparison.Ordinal)))
            .SelectMany(term => term).Concat(_terms[key]);
    }
}
