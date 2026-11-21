using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;

static class BulkMaterialTests
{
    public static void Run()
    {
        static void Check(bool condition, string message) {
            if (!condition) throw new Exception("FAIL: " + message);
            Console.WriteLine("PASS: " + message);
        }
        var request = new BulkMaterialEstimateRequest {
            ProfileKey = "structural-steel", MaterialId = 3,
            Target = new() { ProjectType = "住宅", StructureType = "SRC", City = "臺中市",
                TotalFloorArea = 100, SiteArea = 20, FloorCount = 5, BasementFloorCount = 2,
                ProjectDate = new DateTime(2026, 1, 1) }
        };
        var material = new BulkMaterialOption(3, "結構鋼骨", "kg", "結構體工程");
        var settings = new EstimationSetting { TopK = 8, MinimumSampleCount = 5, LowerQuantile = .15m,
            MedianQuantile = .5m, UpperQuantile = .85m, SimilarityExponent = 1, TimeHalfLifeYears = 3 };
        var candidates = Enumerable.Range(1, 5).Select(i => new BulkMaterialCandidate(i, "SYNTHETIC:case"+i,
            "住宅", "SRC", 10, 2, 5, 2, new DateTime(2026, 1, 1), "Approved", false, 100)).ToArray();
        var rows = Enumerable.Range(1, 5).Select(i => new BulkMaterialHistoricalRow(i, i, i, i*100m,
            i, 3, "Manual", false, "SRC 鋼骨構材", "T", i*.1m, "結構體工程")).ToArray();
        var sources = Enumerable.Range(1, 5).Select(i => new BulkMaterialSource(i, i)).ToArray();
        BulkMaterialEstimateResult Calculate(BulkMaterialCandidate[]? c = null, BulkMaterialHistoricalRow[]? r = null) =>
            BulkMaterialRangeCalculator.Calculate(request, material, settings, c ?? candidates, r ?? rows, sources);
        var baseline = Calculate();
        Check(baseline.HasEnoughSamples && baseline.ReferenceProjectCount == 5 && baseline.LowerQuantity == 1000 &&
            baseline.MedianQuantity == 3000 && baseline.UpperQuantity == 5000,
            "鋼骨同結構來源先換算T/kg，依五個獨立建案加權分位再換算目標量");
        Check(baseline.RecognizableSyntheticNameProjectCount == 5 && !baseline.FormalAnomalyEnabled &&
            !baseline.PriceComparisonSupported && baseline.EffectiveSampleSize == 5,
            "大宗歷史概算保留合成提示及ESS，不開啟正式紅燈或混合計價單價");
        var mixed = candidates.Concat(new[] {
            candidates[0] with { ProjectId = 11, StructureType = "SC" },
            candidates[0] with { ProjectId = 12, StructureType = "RC" },
            candidates[0] with { ProjectId = 13, HistoryStatus = "Draft" },
            candidates[0] with { ProjectId = 14, IsDeleted = true },
            candidates[0] with { ProjectId = 15, ProjectType = "廠房" }
        }).ToArray();
        Check(Calculate(mixed).ReferenceProjectCount == 5, "SC/RC、Draft、刪除或不同用途不能混入SRC鋼骨參考");
        request.Target.StructureType = "SC";
        var scAvailable = Calculate(candidates.Select(x => x with { StructureType = "SC" }).ToArray());
        Check(scAvailable.HasEnoughSamples && scAvailable.References.All(x => x.StructureType == "SC"),
            "SC鋼骨有同結構來源時可以估算，不因舊流程只支援SRC而被擋住");
        var sc = Calculate(mixed);
        Check(!sc.HasEnoughSamples && sc.ReferenceProjectCount == 0 && sc.LowerQuantity == null,
            "SC目標同樣不能借SRC湊樣本，資料不足不產生界限");
        request.Target.StructureType = "SRC";
        request.ObservedQuantity = 99999;
        Check(Calculate().Status == "ManualReviewRequired" && !Calculate().FormalAnomalyEnabled,
            "超出大宗歷史區間只提示人工複核");
        request.ObservedQuantity = 2000;
        Check(Calculate().Status == "HistoricalReferenceAvailable", "區間內顯示歷史參考可用，不宣稱工程合格");
        request.ObservedQuantity = 99999;
        foreach (var invalidRow in new[] {
            rows[0] with { SourceUnit = "式" },
            rows[0] with { SourceQuantity = 20 },
            rows[0] with { SourceMaterialId = 99 },
            rows[0] with { SourceProjectId = 99 },
            rows[0] with { FinalBidItemId = null },
            rows[0] with { MappingStatus = "Pending" },
            rows[0] with { IsIgnored = true },
            rows[0] with { ItemName = "鋼骨吊車費" },
            rows[0] with { MajorCategory = "門窗工程" },
            rows[0] with { Quantity = 0 }
            ,rows[0] with { HistoryIsOutlier = true }
        }) {
            var result = Calculate(r: rows.Skip(1).Prepend(invalidRow).ToArray());
            Check(!result.HasEnoughSamples && result.LowerQuantity == null && result.Status == "InsufficientSamples" &&
                result.ExcludedProjects.Any(x => x.ProjectId == 1),
                "來源單位、量、對應、追溯或範圍有疑慮時整案排除且不產生誤導判定");
        }
        var duplicate = Calculate(r: rows.Append(rows[0] with { TransactionId = 99 }).ToArray());
        Check(duplicate.ReferenceProjectCount == 4 && duplicate.ExcludedProjects.Any(x => x.Reason.Contains("重複")),
            "相同決標來源重複回寫不能膨脹數量或樣本");
        var partial = BulkMaterialRangeCalculator.Calculate(request, material, settings, candidates, rows,
            sources.Append(new BulkMaterialSource(1, 99)).ToArray());
        Check(!partial.HasEnoughSamples && partial.ExcludedProjects.Any(x => x.Reason.Contains("涵蓋")),
            "有標準材料來源未讀取時，不能用其餘部分量代替整案");
        var directRows = rows.Select(x => x with { HasHistoricalTransaction = false, Quantity = 0 }).ToArray();
        var direct = BulkMaterialRangeCalculator.Calculate(request, material, settings, candidates, directRows, sources);
        Check(direct.HasEnoughSamples && direct.MedianQuantity == 3000 && direct.References.All(x => x.FinalBidOnlyRowCount == 1),
            "鋼骨未經舊回寫白名單仍可直接採已核准決標來源，且明確標示來源種類");
        var blockedDirect = BulkMaterialRangeCalculator.Calculate(request, material, settings, candidates,
            directRows.Skip(1).Prepend(directRows[0] with { HistoryIsOutlier = true }).ToArray(), sources);
        Check(!blockedDirect.HasEnoughSamples, "已刪除或排除的歷史不得藉由決標來源回退重新啟用");
        request.ExcludeProjectId = 5;
        Check(Calculate().ReferenceProjectCount == 4, "目標若已存在歷史庫，可依ID排除自身");
        request.ExcludeProjectId = null; request.TopCount = 3;
        Check(Calculate().ReferenceProjectCount == 3 && !Calculate().HasEnoughSamples,
            "Top K小於最低樣本時保留不足提示，不自動擴大或補樣本");
        request.TopCount = null; request.ObservedQuantity = null;
        Check(BulkMaterialRangeCalculator.TryNormalizeQuantity(2, "公噸", "kg", out var kg) && kg == 2000 &&
            !BulkMaterialRangeCalculator.TryNormalizeQuantity(2, "L", "kg", out _),
            "僅允許明確重量/面積/體積換算，不猜測密度或跨維度轉換");
        var formRequest = new BulkMaterialEstimateRequest { ProfileKey = "formwork", MaterialId = 6, Target = request.Target };
        var formMaterial = new BulkMaterialOption(6, "普通模板組立及拆除", "m2", "結構體工程");
        var formRows = rows.Select(x => x with { SourceMaterialId = 6, SourceUnit = "m²", SourceQuantity = x.Quantity }).ToArray();
        var form = BulkMaterialRangeCalculator.Calculate(formRequest, formMaterial, settings, candidates, formRows, sources);
        Check(form.HasEnoughSamples && form.MedianQuantity == 3000 && form.Unit == "m2",
            "模板依同標準工項面積建立歷史比例，不混入其他模板系統");
        var earthRequest = new BulkMaterialEstimateRequest { ProfileKey = "earthwork", MaterialId = 5,
            Target = new() { ProjectType = "住宅", StructureType = "SRC", City = "臺中市", TotalFloorArea = 100,
                SiteArea = 40, FloorCount = 5, BasementFloorCount = 2, ProjectDate = request.Target.ProjectDate } };
        var earthMaterial = new BulkMaterialOption(5, "基坑土方開挖與運棄", "m3", "土方工程");
        var earthRows = rows.Select(x => x with { SourceMaterialId = 5, SourceUnit = "m3", SourceQuantity = x.Quantity,
            MajorCategory = "土方及基礎工程" }).ToArray();
        var earth = BulkMaterialRangeCalculator.Calculate(earthRequest, earthMaterial, settings, candidates, earthRows, sources);
        Check(earth.HasEnoughSamples && earth.MedianQuantity == 6000 && earth.BasisLabel.Contains("基地"),
            "土方使用基地面積作分母而非樓地板面積");
        var wrongBasement = candidates.Select(x => x with { BasementFloorCount = 1 }).ToArray();
        Check(!BulkMaterialRangeCalculator.Calculate(earthRequest, earthMaterial, settings, wrongBasement, earthRows, sources).HasEnoughSamples,
            "土方不得跨地下層數混樣本");
        foreach (var bad in new[] {
            new BulkMaterialEstimateRequest { ProfileKey = "unknown", MaterialId = 3, Target = request.Target },
            new BulkMaterialEstimateRequest { ProfileKey = "structural-steel", MaterialId = 0, Target = request.Target },
            new BulkMaterialEstimateRequest { ProfileKey = "structural-steel", MaterialId = 3, Target = request.Target, TopCount = 101 },
            new BulkMaterialEstimateRequest { ProfileKey = "structural-steel", MaterialId = 3, Target = request.Target, ObservedQuantity = -1 },
        }) {
            bool refused = false;
            try { BulkMaterialRangeCalculator.ValidateRequest(bad); } catch (ArgumentException) { refused = true; }
            Check(refused, "大宗工項的未知類別、未選材料、不合理TopK與負量須在查DB前拒絕");
        }
        Check(!TwoAScopePolicy.IsQuantityDetectionCategory("鋼骨") && !TwoAScopePolicy.IsQuantityDetectionCategory("模板"),
            "新歷史概算不偷偷改動既有Run或正式數量白名單");
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused").Options);
        var service = new BulkMaterialEstimateService(context, null!, null!);
        var sql = service.BuildHistoryQuery(3, [1, 2, 3]).ToQueryString();
        Check(sql.Contains("LEFT JOIN") && sql.Contains("final_bid_item") && sql.Contains("historical_transaction"),
            "新增歷史追溯查詢已由Npgsql轉譯SQL；不開啟DB連線");
        Check(service.BuildCoverageQuery(3, [1, 2, 3]).ToQueryString().Contains("final_bid_item"),
            "決標來源涵蓋查詢已由Npgsql轉譯SQL；不開啟DB連線");
        Check(service.BuildSourceQuery(3, [1, 2, 3]).ToQueryString().Contains("final_bid_item") &&
            service.BuildBlockedSourcesQuery(3, [1, 2, 3]).ToQueryString().Contains("is_deleted"),
            "決標直接讀取與已排除歷史查詢均已轉譯SQL；不開啟DB連線");
    }

    // Optional read-only replay of an isolated synthetic API snapshot.
    // No import/write endpoint is called; snapshots are never production training.
    public static void ReplaySnapshot(string path)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), options) ?? throw new Exception("Missing snapshot");
        if (snapshot.ScoreFromProjectMetadata) {
            var projects = snapshot.Candidates.Select(c => new ProjectMaster { ProjectId = c.ProjectId,
                ProjectName = c.ProjectName, ProjectType = c.ProjectType, StructureType = c.StructureType, City = c.City,
                TotalFloorArea = c.FloorArea, SiteArea = c.SiteArea, FloorCount = c.FloorCount,
                BasementFloorCount = c.BasementFloorCount, StartDate = c.ProjectDate }).ToList();
            var scores = ProjectSimilarityService.ScoreCandidates(snapshot.Request.Target, projects, snapshot.Settings)
                .ToDictionary(x => x.ProjectId, x => x.SimilarityScore);
            snapshot.Candidates = snapshot.Candidates.Select(c => c with {
                SimilarityScore = scores.GetValueOrDefault(c.ProjectId) }).ToArray();
        }
        var result = BulkMaterialRangeCalculator.Calculate(snapshot.Request, snapshot.Material, snapshot.Settings,
            snapshot.Candidates, snapshot.Rows, snapshot.Sources);
        File.WriteAllText(path+".result.json", JsonSerializer.Serialize(result, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
        if (!result.HasEnoughSamples || result.References.Any(x => x.StructureType != snapshot.Request.Target.StructureType))
            throw new Exception("Synthetic source replay failed: no interval or mixed structures");
        Console.WriteLine($"PASS: isolated synthetic snapshot replay {snapshot.Request.ProfileKey}/{snapshot.Request.Target.StructureType}, refs={result.ReferenceProjectCount}");
    }
    public class Snapshot {
        public BulkMaterialEstimateRequest Request { get; set; } = new();
        public BulkMaterialOption Material { get; set; } = null!;
        public EstimationSetting Settings { get; set; } = new();
        public BulkMaterialCandidate[] Candidates { get; set; } = [];
        public BulkMaterialHistoricalRow[] Rows { get; set; } = [];
        public BulkMaterialSource[] Sources { get; set; } = [];
        public bool ScoreFromProjectMetadata { get; set; }
    }
}

