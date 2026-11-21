using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services;

public class MaterialCapabilityService(ApplicationDbContext context)
{
    public static IReadOnlyList<MaterialCandidateDefinition> Definitions { get; } =
    [
        new("rebar", "鋼筋", "kg", ["鋼筋"], "現行相似建案用量模型；仍受結構配置及設計影響。", [], true),
        new("concrete", "混凝土", "m3", ["混凝土"], "現行相似建案用量模型；仍受構件尺寸及設計影響。", [], true),
        new("curing-compound", "混凝土養護劑", "kg", ["養護劑"], "指定產品單位用量 × 實際待養護面積。",
            ["實際待養護面積", "核定養護工法與產品", "現場表面及氣候條件", "同產品完整歷史用量與對應面積"], false, ["antisol-e"]),
        new("release-agent", "模板脫模劑", "L", ["脫模劑", "脫膜劑"], "實際模板施作面積 ÷ 指定產品覆蓋率；需分模板材質。",
            ["實際模板接觸面積", "模板材質與表面狀況", "指定產品及工法", "同條件歷史用量與面積"], false, ["separol-wood", "separol-metal-plastic"]),
        new("structural-steel", "鋼骨", "kg", ["鋼骨", "H型結構鋼"], "同結構與同標準材料重量／樓地板面積歷史概算；跨距、載重與接頭範圍仍須核對。",
            ["鋼骨結構系統與構件範圍", "跨距／載重條件", "接頭及施工工法", "經確認的材料與勞務分拆"]),
        new("formwork", "模板", "m2", ["模板"], "同標準模板工項面積／樓地板面積歷史概算；精算仍需實際接觸面積、系統及周轉方式。",
            ["模板實際接觸面積", "模板系統與材質", "周轉次數及計量口徑"]),
        new("earthwork", "土方開挖", "m3", ["土方開挖", "基坑", "開挖土方"], "同標準開挖工項、同地下層數之體積／基地面積歷史概算；精算仍需開挖幾何與土質。",
            ["開挖範圍及深度", "土質／地質", "支撐工法及計量口徑"]),
        new("waterproofing", "防水材料", "m2", ["防水"], "指定施作面積、材料系統及層厚後才有可比消耗規律。",
            ["實際防水施作面積", "材料系統及層數／厚度", "同工法完整歷史用量"])
    ];

    public async Task<MaterialCapabilityResponse> GetAsync(string? structureType, CancellationToken cancellationToken = default)
    {
        structureType = NormalizeStructure(structureType);
        int minimum = await context.EstimationSettings.AsNoTracking().Where(x => x.SettingsId == 1)
            .Select(x => x.MinimumSampleCount).FirstOrDefaultAsync(cancellationToken);
        if (minimum <= 0) minimum = 5;
        var materials = await context.MaterialMasters.AsNoTracking()
            .Where(x => x.IsActive && x.UnitMaster.IsActive)
            .Select(x => new MaterialCapabilityMaterial(x.MaterialId, x.MaterialName, x.UnitMaster.UnitName))
            .ToListAsync(cancellationToken);
        var candidateIds = materials.Where(m => Definitions.Any(d => Matches(d, m)))
            .Select(m => m.MaterialId).Distinct().ToArray();
        var mappings = await context.DetectionCategoryMaterialMaps.AsNoTracking()
            .Where(x => x.IsActive && x.DetectionCategory.IsActive && candidateIds.Contains(x.MaterialId))
            .Select(x => new MaterialCapabilityMapping(x.MaterialId, x.DetectionCategory.DetectionCategoryName,
                x.DetectionCategory.MajorCategoryName, x.DetectionCategory.UnitMaster.UnitName))
            .ToListAsync(cancellationToken);
        // No transaction rows are counted as independent cases. Approved is a
        // workflow state, not a certification of source or engineering scope.
        var history = await context.HistoricalTransactions.AsNoTracking()
            .Where(x => candidateIds.Contains(x.MaterialId) && !x.IsDeleted && !x.IsOutlier && x.ActualQuantity > 0 &&
                x.ProjectMasterId.HasValue && x.ProjectMaster != null && !x.ProjectMaster.IsDeleted &&
                x.ProjectMaster.HistoryStatus == "Approved" && x.ProjectMaster.TotalFloorArea > 0 &&
                x.ProjectMaster.SiteArea > 0 && x.ProjectMaster.FloorCount > 0 &&
                x.ProjectMaster.BasementFloorCount >= 0 && x.ProjectMaster.StartDate.HasValue &&
                (structureType == null || x.ProjectMaster.StructureType == structureType))
            .Select(x => new MaterialCapabilityHistory(x.MaterialId, x.ProjectMasterId!.Value,
                x.ProjectMaster!.StructureType!, x.ProjectMaster.ProjectName))
            .Distinct().ToListAsync(cancellationToken);
        return new(structureType,
            "名稱命中只是材料候選；筆數足夠不代表規律已驗證。Approved不等於真實來源認證，新工項缺工程條件時不產生正式異常紅燈。",
            Definitions.Select(d => Evaluate(d, materials, mappings, history, minimum)).ToArray());
    }

    public static string? NormalizeStructure(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim().ToUpperInvariant();
        return value is "RC" or "SC" or "SRC" ? value : throw new ArgumentException("結構類型須為RC、SC或SRC。");
    }

    private static string NormalizeUnit(string value) => value.Trim().ToUpperInvariant()
        .Replace("²", "2").Replace("³", "3").Replace("^", "") switch
        {
            "公斤" or "KG" => "kg", "公升" or "L" => "L",
            "平方公尺" or "M2" => "m2", "立方公尺" or "M3" => "m3", var other => other
        };

    private static bool Matches(MaterialCandidateDefinition definition, MaterialCapabilityMaterial material) =>
        !TwoAScopePolicy.IsExplicitNonMaterialCharge(material.Name) &&
        definition.NameHints.Any(hint => material.Name.Contains(hint, StringComparison.OrdinalIgnoreCase));

    public static MaterialCapabilityItem Evaluate(MaterialCandidateDefinition definition,
        IReadOnlyList<MaterialCapabilityMaterial> materials, IReadOnlyList<MaterialCapabilityMapping> mappings,
        IReadOnlyList<MaterialCapabilityHistory> history, int minimum)
    {
        if (minimum <= 0) throw new ArgumentOutOfRangeException(nameof(minimum));
        var candidates = materials.Where(m => Matches(definition, m)).ToArray();
        var compatible = candidates.Where(m => NormalizeUnit(m.Unit) == definition.Unit).Select(m => m.MaterialId).ToHashSet();
        var official = mappings.Where(m => compatible.Contains(m.MaterialId) && m.CategoryName == definition.Name &&
            m.MajorCategory == "結構體工程" && NormalizeUnit(m.Unit) == definition.Unit).Select(m => m.MaterialId).ToHashSet();
        var applicableHistory = history.Where(h => compatible.Contains(h.MaterialId)).ToArray();
        int count = applicableHistory.Select(h => h.ProjectId).Distinct().Count();
        int mappedCount = history.Where(h => official.Contains(h.MaterialId)).Select(h => h.ProjectId).Distinct().Count();
        bool supported = definition.ExistingModel && TwoAScopePolicy.IsQuantityDetectionCategory(definition.Name);
        bool ready = supported && official.Count > 0 && mappedCount >= minimum;
        bool spec = (definition.SpecificationKeys?.Length ?? 0) > 0;
        bool historical = BulkMaterialRangeCalculator.Profiles.Any(x => x.Key == definition.Key);
        var reasons = new List<string>();
        if (candidates.Length == 0) reasons.Add("尚無名稱可辨識的材料候選，不能以名稱猜測正式對應。");
        if (candidates.Length > compatible.Count) reasons.Add("部分材料單位不符，未自動假定換算係數。");
        if (official.Count == 0) reasons.Add("尚無此工項的正式材料對應與標準單位設定。");
        if (count < minimum) reasons.Add($"目前可比歷史建案{count}件，低於最低{minimum}件；不以交易列數補足。");
        if (!supported) reasons.Add("尚未具備經核定的歷史比較口徑，不開放正式數量異常判定。");
        if (definition.MissingEngineeringConditions.Length > 0)
            reasons.Add("精算與正式判定仍須核對：" + string.Join("、", definition.MissingEngineeringConditions) + "。");
        if (historical) reasons.Add("已有獨立標準材料歷史概算入口；實際可用樣本、來源與結構條件由每次估算檢查。此表的正式對應數是既有偵錯分類對應，不代表新概算入口不能選材料。");
        if (spec) reasons.Add("可另依指定產品資料及使用者提供的實際施工面積試算；不等於歷史模型已通過。");
        string status = ready ? "CurrentModelAvailable" : supported ? "InsufficientEvidence" : spec ? "SpecificationOnly" : "EngineeringConditionsRequired";
        string label = ready ? "現行模型支援，仍須逐案檢查" : supported ? "資料條件不足" : spec ? "可依規格試算／未開放異常判定" : "待補工程條件";
        if (historical) { status = "HistoricalEstimateSupported"; label = "支援歷史概算／須逐案檢查"; }
        return new(definition.Key, definition.Name, definition.Unit, status, label, supported, ready,
            spec, definition.PatternBasis, candidates.Length, official.Count, candidates.Length-compatible.Count,
            count, applicableHistory.Where(h => h.ProjectName.Contains("SYNTHETIC:", StringComparison.OrdinalIgnoreCase))
                .Select(h => h.ProjectId).Distinct().Count(), minimum, definition.MissingEngineeringConditions,
            reasons.ToArray(), definition.SpecificationKeys ?? [], historical);
    }
}
