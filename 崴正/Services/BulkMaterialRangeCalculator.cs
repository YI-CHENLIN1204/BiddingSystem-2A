using 崴正.Models;

namespace 崴正.Services;

// The same similarity/time weighting as the existing quantity estimator, with
// explicit material selection, structure segregation and source lineage gates.
// These are historical planning references, not certified engineering limits.
public static class BulkMaterialRangeCalculator
{
    public static IReadOnlyList<BulkMaterialProfile> Profiles { get; } =
    [
        new("structural-steel", "鋼骨", "kg", "FloorArea", ["SC", "SRC"],
            "同結構、同標準材料之重量／樓地板面積概算；仍須核對構件、跨度、載重及接頭範圍。"),
        new("formwork", "模板", "m2", "FloorArea", ["RC", "SC", "SRC"],
            "同標準模板工項之計量面積／樓地板面積概算；不是由圖說計算的實際接觸面積，也不等於採購板材面積。"),
        new("earthwork", "土方開挖", "m3", "SiteArea", ["RC", "SC", "SRC"],
            "同標準開挖工項、同地下層數之體積／基地面積概算；仍須核對開挖深度、土質與運棄範圍。")
    ];

    public static BulkMaterialProfile ValidateRequest(BulkMaterialEstimateRequest request)
    {
        var profile = Profiles.SingleOrDefault(x => x.Key == request.ProfileKey)
            ?? throw new ArgumentException("請選擇鋼骨、模板或土方開挖。");
        var t = request.Target ?? throw new ArgumentException("請提供建案條件。");
        if (request.MaterialId <= 0) throw new ArgumentException("請明確選擇已對應的標準材料。");
        if (!profile.Structures.Contains(t.StructureType))
            throw new ArgumentException(profile.Key == "structural-steel" ? "鋼骨估算目前僅適用 SC 或 SRC，兩者分開比較。" : "請選擇 RC、SC 或 SRC。");
        if (string.IsNullOrWhiteSpace(t.ProjectType) || string.IsNullOrWhiteSpace(t.City) ||
            t.TotalFloorArea <= 0 || t.TotalFloorArea > 1_000_000_000m || t.SiteArea <= 0 || t.SiteArea > 1_000_000_000m ||
            t.FloorCount <= 0 || t.BasementFloorCount < 0 || t.ProjectDate == default)
            throw new ArgumentException("請完整填寫用途、縣市、有效面積、樓層及年月。");
        if (profile.Key == "earthwork" && t.BasementFloorCount == 0)
            throw new ArgumentException("土方概算需有地下層數；無地下室案件須補實際開挖條件，不能套用此概算。");
        if (request.TopCount is <= 0 or > 100 || request.ExcludeProjectId is <= 0 || request.ObservedQuantity < 0)
            throw new ArgumentException("Top K 須為 1–100、排除建案 ID 須為正數，待比用量不可為負數。");
        return profile;
    }

    public static bool TryNormalizeQuantity(decimal quantity, string? unit, string targetUnit, out decimal normalized)
    {
        string u = (unit ?? "").Trim().ToUpperInvariant().Replace(" ", "").Replace("²", "2").Replace("³", "3").Replace("^", "");
        decimal factor = (u, targetUnit) switch
        {
            ("KG" or "公斤", "kg") => 1m,
            ("T" or "TON" or "TONNE" or "公噸" or "噸", "kg") => 1000m,
            ("M2" or "平方公尺", "m2") => 1m,
            ("M3" or "立方公尺", "m3") => 1m,
            _ => 0m
        };
        normalized = quantity * factor;
        return factor > 0;
    }

    public static bool MatchesMaterial(BulkMaterialProfile profile, BulkMaterialOption material)
    {
        var name = material.MaterialName;
        bool named = profile.Key switch
        {
            "structural-steel" => new[] { "鋼骨", "H型鋼", "H型結構鋼", "鋼構" }.Any(name.Contains),
            "formwork" => name.Contains("模板"),
            "earthwork" => new[] { "土方開挖", "開挖土方", "基礎開挖" }.Any(name.Contains),
            _ => false
        };
        return named && !IsServiceOnly(name) && TryNormalizeQuantity(1, material.UnitName, profile.Unit, out _);
    }

    internal static bool IsServiceOnly(string name) =>
        new[] { "費", "補貼", "租賃", "試驗", "檢測", "吊車", "鷹架", "圍籬", "欄杆", "輕鋼架", "鋼筋", "臨時支撐" }.Any(name.Contains);

    public static BulkMaterialEstimateResult Calculate(BulkMaterialEstimateRequest request, BulkMaterialOption material,
        EstimationSetting settings, IReadOnlyList<BulkMaterialCandidate> candidates, IReadOnlyList<BulkMaterialHistoricalRow> rows,
        IReadOnlyList<BulkMaterialSource> sources)
    {
        var profile = ValidateRequest(request);
        if (material.MaterialId != request.MaterialId || !MatchesMaterial(profile, material))
            throw new ArgumentException("標準材料與所選大宗工項或單位不符，不能用名稱候選自動替代。");
        if (settings.TopK < 1 || settings.MinimumSampleCount < 1 || settings.MinimumSampleCount > settings.TopK ||
            settings.SimilarityExponent <= 0 || settings.TimeHalfLifeYears <= 0 ||
            settings.LowerQuantile < 0 || settings.LowerQuantile >= settings.MedianQuantile ||
            settings.MedianQuantile >= settings.UpperQuantile || settings.UpperQuantile > 1)
            throw new ArgumentException("估算參數不完整或分位數設定不正確。");
        var t = request.Target;
        var result = new BulkMaterialEstimateResult {
            ProfileKey = profile.Key, MaterialName = material.MaterialName, Unit = profile.Unit,
            BasisLabel = profile.Basis == "SiteArea" ? "基地面積（m²）" : "總樓地板面積（m²）",
            MinimumSampleCount = settings.MinimumSampleCount, ObservedQuantity = request.ObservedQuantity,
            LowerQuantile = settings.LowerQuantile, MedianQuantile = settings.MedianQuantile, UpperQuantile = settings.UpperQuantile,
            Warnings = [profile.Notice, "Approved 僅是審核狀態；來源真實性、整案完整性及同工法仍須人工核對。",
                "本結果為歷史概算及偏離提示，尚未經獨立 Holdout 校準，不代表工程不合格或採購核准量。"]
        };
        var scoped = candidates.Where(x => !x.IsDeleted && x.HistoryStatus == "Approved" &&
            x.ProjectId != request.ExcludeProjectId && x.ProjectType == t.ProjectType && x.StructureType == t.StructureType &&
            x.FloorArea > 0 && x.SiteArea > 0 && x.FloorCount > 0 && x.BasementFloorCount >= 0 &&
            x.ProjectDate != default && double.IsFinite(x.SimilarityScore) && x.SimilarityScore > 0 &&
            x.SimilarityScore >= (double)settings.MinimumSimilarityScore &&
            (profile.Key != "earthwork" || x.BasementFloorCount == t.BasementFloorCount)).ToArray();
        if (scoped.Select(x => x.ProjectId).Distinct().Count() != scoped.Length)
            throw new ArgumentException("候選建案重複，不能重複計算獨立樣本。");
        result.CandidateProjectCount = scoped.Length;
        var grouped = rows.GroupBy(x => x.ProjectId).ToDictionary(x => x.Key, x => x.ToArray());
        var expected = sources.GroupBy(x => x.ProjectId).ToDictionary(x => x.Key, x => x.Select(s => s.FinalBidItemId).ToHashSet());
        var available = new List<BulkMaterialReference>();
        foreach (var project in scoped)
        {
            if (!grouped.TryGetValue(project.ProjectId, out var sourceRows)) {
                result.ExcludedProjects.Add(new(project.ProjectId, "無所選標準材料的已核准決標來源。")); continue;
            }
            string? reason = null;
            decimal quantity = 0m;
            var sourceIds = new HashSet<long>();
            foreach (var row in sourceRows)
            {
                if (row.HistoryIsOutlier)
                    reason = "此材料有排除或離群紀錄，須先核對整案計量，不以其餘部分合計估算。";
                else if (!row.FinalBidItemId.HasValue || row.SourceProjectId != project.ProjectId ||
                    row.SourceMaterialId != material.MaterialId || row.IsIgnored ||
                    row.MappingStatus is not ("Manual" or "Auto"))
                    reason = "歷史缺可追溯且已對應的決標來源，或來源已忽略。";
                else if (!sourceIds.Add(row.FinalBidItemId.Value))
                    reason = "相同決標來源重複回寫，整案排除以免重複或部分計量。";
                else if ((row.HasHistoricalTransaction && (row.Quantity <= 0 ||
                    !TryNormalizeQuantity(row.Quantity, material.UnitName, profile.Unit, out _))) || row.SourceQuantity is null or <= 0 ||
                    !TryNormalizeQuantity(row.SourceQuantity.Value, row.SourceUnit, profile.Unit, out _))
                    reason = "來源數量或單位不完整；不以局部合計補足整案。";
                else if (IsServiceOnly(row.ItemName))
                    reason = "來源含單獨費用或輔助工項，須先確認材料計量範圍。";
                else {
                    TryNormalizeQuantity(row.Quantity, material.UnitName, profile.Unit, out var historical);
                    TryNormalizeQuantity(row.SourceQuantity.Value, row.SourceUnit, profile.Unit, out var source);
                    if (row.HasHistoricalTransaction && Math.Abs(historical-source) > 0.0001m)
                        reason = "歷史量與來源標單換算量不一致。";
                    else if (!IsAllowedMajor(profile.Key, row.MajorCategory))
                        reason = "來源工程大項不符，須確認工項範圍。";
                    else quantity += row.HasHistoricalTransaction ? historical : source;
                }
                if (reason != null) break;
            }
            if (reason == null && (!expected.TryGetValue(project.ProjectId, out var expectedIds) ||
                !expectedIds.SetEquals(sourceIds)))
                reason = "所選材料決標來源涵蓋不完整，不能用部分工項量代替整案。";
            if (reason != null) { result.ExcludedProjects.Add(new(project.ProjectId, reason)); continue; }
            double age = Math.Abs((t.ProjectDate-project.ProjectDate).TotalDays)/365.2425;
            decimal weight = (decimal)(Math.Pow(project.SimilarityScore/100d, (double)settings.SimilarityExponent) *
                Math.Pow(0.5, age/(double)settings.TimeHalfLifeYears));
            if (weight <= 0) continue;
            decimal basis = profile.Basis == "SiteArea" ? project.SiteArea : project.FloorArea;
            available.Add(new(project.ProjectId, project.ProjectName, project.StructureType, project.ProjectDate,
                basis, quantity, quantity/basis, sourceRows.Length, project.SimilarityScore, weight,
                sourceRows.Count(x => !x.HasHistoricalTransaction)));
        }
        result.AvailableProjectCount = available.Count;
        result.References = available.OrderByDescending(x => x.SimilarityScore).ThenByDescending(x => x.FinalWeight)
            .ThenBy(x => x.ProjectId).Take(request.TopCount ?? settings.TopK).ToList();
        result.ReferenceProjectCount = result.References.Count;
        result.RecognizableSyntheticNameProjectCount = result.References.Count(x => x.ProjectName.Contains("SYNTHETIC:", StringComparison.OrdinalIgnoreCase));
        var diagnostic = ReferenceWeightDiagnostics.Calculate(result.References.Select(x => x.FinalWeight));
        result.EffectiveSampleSize = diagnostic.EffectiveSampleSize; result.LargestWeightSharePercent = diagnostic.LargestWeightShare;
        if (result.ReferenceProjectCount < settings.MinimumSampleCount) {
            result.Message = $"資料不足：有效獨立參考 {result.ReferenceProjectCount} 案，至少需 {settings.MinimumSampleCount} 案。請查看來源排除原因或調整 Top K；不產生偏高／偏低判定。";
            return result;
        }
        decimal targetArea = profile.Basis == "SiteArea" ? t.SiteArea : t.TotalFloorArea;
        decimal lower = Quantile(result.References, settings.LowerQuantile)*targetArea;
        decimal median = Quantile(result.References, settings.MedianQuantile)*targetArea;
        decimal upper = Quantile(result.References, settings.UpperQuantile)*targetArea;
        result.LowerQuantity = Math.Round(lower, 4); result.MedianQuantity = Math.Round(median, 4); result.UpperQuantity = Math.Round(upper, 4);
        result.HasEnoughSamples = true;
        bool outside = request.ObservedQuantity.HasValue && (request.ObservedQuantity < lower || request.ObservedQuantity > upper);
        result.Status = outside ? "ManualReviewRequired" : "HistoricalReferenceAvailable";
        result.Message = outside ? "待比用量超出歷史參考區間，請核對計量、工法及設計差異；此為人工複核提示。" :
            "歷史用量概算完成；區間內也不代表工程合格或設計正確。";
        return result;
    }

    internal static bool IsAllowedMajor(string key, string? major) => key switch
    {
        "structural-steel" => major is "結構體工程" or "鋼骨鋼構工程",
        "formwork" => major == "結構體工程",
        "earthwork" => major == "土方及基礎工程",
        _ => false
    };
    private static decimal Quantile(List<BulkMaterialReference> references, decimal q)
    {
        var ordered = references.OrderBy(x => x.UsagePerArea).ToArray();
        decimal target = ordered.Sum(x => x.FinalWeight)*q, sum = 0;
        foreach (var reference in ordered) { sum += reference.FinalWeight; if (sum >= target) return reference.UsagePerArea; }
        return ordered[^1].UsagePerArea;
    }
}

