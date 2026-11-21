using 崴正.Models;

namespace 崴正.Services;

// Published, product-specific theoretical consumption ranges, not historical
// P15/P85, generic engineering constants, or automatic anomaly thresholds.
public static class SpecificationQuantityService
{
    private static readonly MaterialSpecificationProfile[] Profiles =
    [
        new("antisol-e", "curing-compound", "Sika Antisol E 混凝土養護劑", "實際待養護混凝土表面面積", "kg",
            "QuantityPerArea", 0.15m, 0.20m, "kg/m²",
            "https://twn.sika.com/dam/dms/tw01/s/sika-antisol-e.pdf", "2022-03 / 02.01；2026-11-22查核",
            ["僅適用此指定產品，不可套用其他養護劑。", "用量受風速、溫度、濕度及表面條件影響。", "養護工法及產品仍須符合本案核定施工規格。"]),
        new("separol-wood", "release-agent", "Sika Separol（木製模板）", "實際模板接觸施作面積", "L",
            "AreaPerQuantity", 13m, 21m, "m²/L",
            "https://twn.sika.com/dam/dms/tw01/3/sika-separol.pdf", "2023-03 / 03.01；2026-11-22查核",
            ["僅適用此指定即用型產品及木製模板。", "模板粗糙度、吸水性、表面狀況與施工耗損會改變實際用量。", "計算面積不是總樓地板面積。"]),
        new("separol-metal-plastic", "release-agent", "Sika Separol（金屬／塑膠模板）", "實際模板接觸施作面積", "L",
            "AreaPerQuantity", 27m, 42m, "m²/L",
            "https://twn.sika.com/dam/dms/tw01/3/sika-separol.pdf", "2023-03 / 03.01；2026-11-22查核",
            ["僅適用此指定即用型產品及金屬／塑膠模板。", "表面孔隙、平整度及施工耗損會改變實際用量。", "不可把公升換成公斤；未提供產品密度。"])
    ];

    public static IReadOnlyList<MaterialSpecificationProfile> GetProfiles() => Profiles;

    public static MaterialSpecificationEstimate Calculate(MaterialSpecificationEstimateRequest request)
    {
        var profile = Profiles.SingleOrDefault(x => x.Key == request.ProfileKey)
            ?? throw new ArgumentException("請選擇資料來源明確的產品與施工條件。");
        if (request.WorkAreaM2 <= 0 || request.WorkAreaM2 > 1_000_000_000m)
            throw new ArgumentException("請輸入有效的實際施工面積。");
        if (request.ObservedQuantity < 0)
            throw new ArgumentException("實際用量不可小於零。");

        decimal lower = profile.RateKind == "QuantityPerArea"
            ? request.WorkAreaM2 * profile.LowerRate : request.WorkAreaM2 / profile.UpperRate;
        decimal upper = profile.RateKind == "QuantityPerArea"
            ? request.WorkAreaM2 * profile.UpperRate : request.WorkAreaM2 / profile.LowerRate;
        bool outside = request.ObservedQuantity.HasValue &&
            (request.ObservedQuantity.Value < lower || request.ObservedQuantity.Value > upper);
        return new(profile.Key, profile.ProductName, request.WorkAreaM2, profile.BasisLabel, profile.OutputUnit,
            Math.Round(lower, 4), Math.Round(upper, 4), "ManufacturerTheoreticalRange", false,
            outside ? "ManualReviewRequired" : "SpecificationEstimateOnly",
            outside
                ? "實際用量超出指定產品的理論區間，請人工核對面積、工法及現場條件；此結果不是正式異常紅燈。"
                : "依指定產品資料及實際施工面積試算；不是歷史P15/P85、工程合格判定或採購核准量。",
            profile.SourceUrl, profile.SourceVersion, profile.Conditions);
    }
}
