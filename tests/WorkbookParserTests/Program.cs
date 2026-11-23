using ClosedXML.Excel;
using 崴正.Services;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    Console.WriteLine("PASS: " + message);
}

if (args is ["--material-replay", var input, var output])
{
    MaterialDictionaryChecks.Replay(input, output);
    return;
}

if (args is ["--material-postgres"])
{
    await MaterialDictionaryPostgres.RunAsync();
    return;
}

MaterialDictionaryChecks.Run();

using var wb = new XLWorkbook();
var work = wb.Worksheets.Add("建築工程-預算單");
string[] headers = { "項次", "工作項目", "單位", "自估數量", "自估單價", "自估複價", "廠商單價", "詢價單價", "比價單價" };
for (int i = 0; i < headers.Length; i++) work.Cell(1, i + 1).Value = headers[i];
work.Cell(2, 1).Value = "1";
work.Cell(2, 2).Value = "3000 PSI 預拌混凝土";
work.Cell(2, 3).Value = "M3";
work.Cell(2, 4).Value = 214;
work.Cell(2, 5).Value = 2350;
work.Cell(2, 6).Value = 502900;
work.Cell(2, 7).Value = 2100;
work.Cell(2, 8).Value = 2100;
work.Cell(2, 9).Value = 250;
// 未填工程量時，不可再使用供應商數字補上。
work.Cell(4, 1).Value = "2.01";
work.Cell(4, 2).Value = "編號含小數點的預拌混凝土";
work.Cell(4, 3).Value = "M3";
work.Cell(4, 4).Value = 214;
work.Cell(4, 5).Value = 2350;
work.Cell(4, 6).Value = 502900;
work.Cell(3, 1).Value = "2";
work.Cell(3, 2).Value = "待確認價款工項";
work.Cell(3, 3).Value = "M3";
work.Cell(3, 5).Value = 1500;
work.Cell(3, 7).Value = 9999;
var quote = wb.Worksheets.Add("比較表");
for (int i = 0; i < headers.Length; i++) quote.Cell(1, i + 1).Value = headers[i];
quote.Cell(2, 1).Value = "1";
quote.Cell(2, 2).Value = "比較表報價";
quote.Cell(2, 3).Value = "式";
quote.Cell(2, 4).Value = 1;
quote.Cell(2, 5).Value = 999;
quote.Cell(2, 6).Value = 999;
wb.Worksheets.Add("估價總表").Cell(1,1).Value = "總計";

using var file = new MemoryStream();
wb.SaveAs(file);
byte[] content = file.ToArray();
var parser = new TenderExcelParser();

bool blocked = false;
try
{
    parser.Parse(new MemoryStream(content), "test.xlsx");
}
catch (InvalidDataException) { blocked = true; }
Check(blocked, "多工作表且未選取時必須拒絕匯入");

var names = parser.GetSheetNames(new MemoryStream(content), "test.xlsx");
Check(names.Count == 3, "預覽保留全部工作表供確認");
var selected = parser.Parse(new MemoryStream(content), "test.xlsx", new[] { "建築工程-預算單" });
Check(selected.Count == 3, "只解析選定工作表的有效工程明細，含小數點項次");
Check(selected.Any(x => x.ItemCode == "2.01" && x.Qty == 214m && x.Amount == 502900m),
    "2.01 等小數點項次若有數量與價格，必須保留為工項而非細分類標題");
// 擬真標單常用右側「工種分類」逐列標示工程大項，不能忽略該欄。
var classifiedSheet = wb.Worksheets.Add("分類欄測試");
string[] classifiedHeaders = { "項次", "工項名稱", "規格／說明", "單位", "數量", "單價", "複價", "工種分類" };
for (int i = 0; i < classifiedHeaders.Length; i++) classifiedSheet.Cell(1, i + 1).Value = classifiedHeaders[i];
classifiedSheet.Cell(2, 1).Value = "2.01";
classifiedSheet.Cell(2, 2).Value = "預拌混凝土";
classifiedSheet.Cell(2, 4).Value = "M3";
classifiedSheet.Cell(2, 5).Value = 214;
classifiedSheet.Cell(2, 6).Value = 2350;
classifiedSheet.Cell(2, 7).Value = 502900;
classifiedSheet.Cell(2, 8).Value = "結構體工程";
using var classifiedStream = new MemoryStream();
wb.SaveAs(classifiedStream);
var classifiedRows = parser.Parse(new MemoryStream(classifiedStream.ToArray()), "test.xlsx", new[] { "分類欄測試" });
Check(classifiedRows.Count == 1 && classifiedRows[0].Category1 == "結構體工程",
    "逐列工種分類可正確成為工程大項，不再全部變成未分類");
Check(selected[0].Qty == 214, "混凝土數量應為214，不應取到廠商比較欄");
Check(selected[0].UnitPrice == 2350, "混凝土單價應為2350");
Check(selected[0].Amount == 502900, "混凝土複價應為502900");
Check(selected[0].SheetName == "建築工程-預算單" && selected[0].RowNumber == 2,
    "保留工作表與來源列號");
Check(selected[1].Qty == null && selected[1].UnitPrice == 1500,
    "數量空白必須保留空白，禁止從其他報價欄猜數量");

bool rejected = false;
try { parser.Parse(new MemoryStream(content), "test.xlsx", new[] { "不存在的工作表" }); }
catch (InvalidDataException) { rejected = true; }
Check(rejected, "無效工作表選擇必須拒絕");
// 唯讀候選量檢查：與實際歷史發布或相似案件偵錯完全隔離。
var inspection = TenderImportInspectionService.Build(new[]
{
    new 崴正.Models.TenderImportRowDto {
        SheetName = "建築工程-預算單", RowNumber = 70,
        Category1 = "結構體工程", ItemName = "SD280 鋼筋",
        Unit = "T", Qty = 501m
    },
    new 崴正.Models.TenderImportRowDto {
        SheetName = "建築工程-預算單", RowNumber = 71,
        Category1 = "結構體工程", ItemName = "鋼筋續接器",
        Unit = "個", Qty = 120m
    },
    new 崴正.Models.TenderImportRowDto {
        SheetName = "建築工程-預算單", RowNumber = 72,
        Category1 = "結構體工程", ItemName = "預拌混凝土 2000 PSI",
        Unit = "M3", Qty = 214m, UnitPrice = null
    },
    new 崴正.Models.TenderImportRowDto {
        SheetName = "建築工程-預算單", RowNumber = 73,
        Category1 = "結構體工程", ItemName = "預拌混凝土 3000 PSI",
        Unit = "坪", Qty = 9m
    },
    new 崴正.Models.TenderImportRowDto {
        SheetName = "建築工程-預算單", RowNumber = 74,
        Category1 = "結構體工程", ItemName = "SD420W 鋼筋",
        Unit = "T", Qty = null
    }
});
Check(inspection.TotalRows == 5, "預匯入保留來源五筆候選明細");
Check(inspection.Rows[0].RowNumber == 70, "預匯入保留 Excel 原始列號");
Check(inspection.QuantitySummaries.Single(x => x.CandidateType == "Rebar").CandidateQuantity == 501000m,
    "預匯入鋼筋噸數安全轉換為 kg");
Check(inspection.Rows[1].CandidateType == "Excluded" && inspection.Rows[1].StandardQuantity == null,
    "鋼筋續接器不得計入鋼筋重量");
Check(inspection.QuantitySummaries.Single(x => x.CandidateType == "Concrete").CandidateQuantity == 214m,
    "混凝土以 m³ 計，無單價不妨礙純數量預檢");
Check(inspection.Rows[3].StandardQuantity == null && inspection.Rows[3].Warnings.Length > 0,
    "未知混凝土單位不得自動轉換");
Check(inspection.Rows[4].StandardQuantity == null && inspection.Rows[4].Warnings.Length > 0,
    "空白鋼筋數量不允許推測填入");
Check(inspection.Notice.Contains("尚未驗證決標真實性"),
    "預匯入明示候選合計不等於異常判定或歷史基準");

Check(TenderImportInspectionService.ClassifyQuantityCandidate("鋼筋加工組立", "SD420W") == "Excluded",
    "鋼筋加工組立不可混入材料重量");
Check(TenderImportInspectionService.ClassifyQuantityCandidate("鋼筋綁紮工資", "") == "Excluded",
    "鋼筋綁紮工資不可混入材料重量");
Check(TenderImportInspectionService.ClassifyQuantityCandidate("預拌混凝土澆置人工", "") == "Excluded",
    "混凝土澆置人工不可混入混凝土體積");
Check(TenderImportInspectionService.ClassifyQuantityCandidate("混凝土試體試驗", "") == "Excluded",
    "混凝土試驗不可造成正式數量組換算失敗");
Check(TenderImportInspectionService.ClassifyQuantityCandidate("鋼筋材料", "SD420W") == "Rebar",
    "鋼筋材料應保留");
Check(TenderImportInspectionService.ClassifyQuantityCandidate("預拌混凝土", "3500 psi") == "Concrete",
    "預拌混凝土材料應保留");

// 永久防回歸：UI 標單的材料/勞務複合工項數量不得自動混入材料量。
// 預檢與正式 Quantity 前處理共用 ClassifyQuantityCandidate，分類需要人工審核。
var uiSmokeRows = new (string Name, string Spec, string Unit, decimal Qty, decimal Price, string Major)[]
{
    ("SD420W D19 鋼筋加工及組立", "D19", "kg", 42000m, 39m, "結構體工程"),
    ("SD420W D22 鋼筋加工及組立", "D22", "kg", 32000m, 40m, "結構體工程"),
    ("基礎鋼筋綁紮材料及工資", "D16", "kg", 18000m, 42m, "結構體工程"),
    ("柱梁主筋鋼筋施工", "D25", "kg", 38000m, 43m, "結構體工程"),
    ("預拌混凝土澆置", "280kgf/cm2", "m3", 1800m, 2600m, "結構體工程"),
    ("樓板混凝土泵送與搗實", "350kgf/cm2", "m3", 1450m, 2850m, "結構體工程"),
    ("基礎底版預拌混凝土", "280kgf/cm2", "m3", 1150m, 2750m, "結構體工程"),
    ("柱梁牆預拌混凝土", "350kgf/cm2", "m3", 1050m, 2900m, "結構體工程"),
    ("普通模板組立與拆除", "柱梁版", "m2", 10500m, 620m, "結構體工程"),
    ("樓板支撐系統租賃", "含安裝", "m2", 5400m, 450m, "結構體工程"),
    ("H型鋼構梁柱製作及吊裝", "H-300", "kg", 105000m, 91m, "鋼骨鋼構工程"),
    ("鋼骨接合板及現場組立", "SRC", "kg", 38000m, 98m, "鋼骨鋼構工程"),
    ("基礎土方開挖及外運", "一般土", "m3", 4300m, 320m, "土方及基礎工程"),
    ("地下室擋土支撐組立", "臨時支撐", "式", 2m, 175000m, "土方及基礎工程"),
    ("地下室防水塗膜施作", "PU", "m2", 2800m, 610m, "防水工程"),
};
var smokeInspection = TenderImportInspectionService.Build(
    uiSmokeRows.Select((line, index) => new 崴正.Models.TenderImportRowDto {
        SheetName = index < 12 ? "結構體明細" : "基礎及地下工程",
        RowNumber = 5 + index, Category1 = line.Major, ItemName = line.Name,
        Spec = line.Spec, Unit = line.Unit, Qty = line.Qty, UnitPrice = line.Price,
        Amount = line.Qty * line.Price
    }).ToArray());
Check(smokeInspection.TotalRows == 15 && smokeInspection.WarningRows == 4,
    "15 筆 UI 測試明細須正確保留四條複合工項排除提示");
Check(smokeInspection.Rows.Count(x => x.CandidateType == "Excluded") == 4,
    "四筆鋼筋/混凝土複合工項不得混入材料候選數量");
Check(smokeInspection.QuantitySummaries.Single(x => x.CandidateType == "Rebar")
    .CandidateQuantity == 38000m,
    "UI smoke 鋼筋候選量 38,000 kg，非原始全部 130,000 kg");
Check(smokeInspection.QuantitySummaries.Single(x => x.CandidateType == "Concrete")
    .CandidateQuantity == 3650m,
    "UI smoke 混凝土候選量 3,650 m3，非原始全部 5,450 m3");
Check(uiSmokeRows.Where(x => x.Major == "結構體工程")
    .Sum(x => x.Qty * x.Price) == 29268000m,
    "結構體大項價格仍計入複合工項，合計 29,268,000，不受數量排除影響");
Check(smokeInspection.Rows.Where(x => x.CandidateType == "Excluded")
    .All(x => x.Warnings.Contains("疑似輔助工項，未納入鋼筋／混凝土候選用量")),
    "每筆排除的材料相關工項需保留理由供人工覆核");

// Conditional material patterns are specification calculations, not an
// extension of the historical anomaly scope or a generic engineering rate.
var curing = SpecificationQuantityService.Calculate(new 崴正.Models.MaterialSpecificationEstimateRequest {
    ProfileKey = "antisol-e", WorkAreaM2 = 1000m
});
Check(curing.LowerQuantity == 150m && curing.UpperQuantity == 200m && curing.Unit == "kg",
    "指定養護劑須依實際施工面積與kg/m2計算，不使用總樓地板面積");
var woodRelease = SpecificationQuantityService.Calculate(new 崴正.Models.MaterialSpecificationEstimateRequest {
    ProfileKey = "separol-wood", WorkAreaM2 = 273m
});
Check(woodRelease.LowerQuantity == 13m && woodRelease.UpperQuantity == 21m && woodRelease.Unit == "L",
    "覆蓋率m2/L必須反向除算上下界，不能當L/m2相乘");
var metalRelease = SpecificationQuantityService.Calculate(new 崴正.Models.MaterialSpecificationEstimateRequest {
    ProfileKey = "separol-metal-plastic", WorkAreaM2 = 1134m, ObservedQuantity = 100m
});
Check(metalRelease.LowerQuantity == 27m && metalRelease.UpperQuantity == 42m,
    "金屬塑膠模板使用獨立產品條件，不混用木模板覆蓋率");
Check(!metalRelease.FormalAnomalyEnabled && metalRelease.ReviewStatus == "ManualReviewRequired" &&
    curing.RangeKind == "ManufacturerTheoreticalRange" && !curing.FormalAnomalyEnabled,
    "超出產品理論範圍僅要求人工核對，不偽裝成歷史P15P85或正式異常紅燈");
foreach (var badRequest in new[] {
    new 崴正.Models.MaterialSpecificationEstimateRequest { ProfileKey = "unknown", WorkAreaM2 = 100m },
    new 崴正.Models.MaterialSpecificationEstimateRequest { ProfileKey = "antisol-e", WorkAreaM2 = 0m },
    new 崴正.Models.MaterialSpecificationEstimateRequest { ProfileKey = "antisol-e", WorkAreaM2 = 100m, ObservedQuantity = -1m }
}) {
    bool refused = false;
    try { SpecificationQuantityService.Calculate(badRequest); }
    catch (ArgumentException) { refused = true; }
    Check(refused, "未知產品、無效面積及負用量必須拒絕，不猜預設規格");
}
var materialEvidence = new[] { new 崴正.Models.MaterialCapabilityMaterial(1, "養護劑", "kg") };
var historicalEvidence = Enumerable.Range(1, 100)
    .Select(i => new 崴正.Models.MaterialCapabilityHistory(1, i, "SRC", "SYNTHETIC:sample"))
    .ToArray();
var candidateCapability = MaterialCapabilityService.Evaluate(
    MaterialCapabilityService.Definitions.Single(x => x.Key == "curing-compound"), materialEvidence,
    new[] { new 崴正.Models.MaterialCapabilityMapping(1, "混凝土養護劑", "結構體工程", "kg") }, historicalEvidence, 5);
Check(candidateCapability.HistoricalProjectCount == 100 && !candidateCapability.FormalAnomalyEnabled &&
    candidateCapability.SpecificationEstimateAvailable && candidateCapability.MissingEngineeringConditions.Length > 0,
    "即使100個歷史案且有材料對應，缺工程口徑的新增工項仍不得自動升格正式判異常");
var duplicateHistory = Enumerable.Range(1, 30)
    .Select(_ => new 崴正.Models.MaterialCapabilityHistory(2, 1, "SRC", "SYNTHETIC:same-project")).ToArray();
var duplicateCapability = MaterialCapabilityService.Evaluate(
    MaterialCapabilityService.Definitions.Single(x => x.Key == "rebar"),
    new[] { new 崴正.Models.MaterialCapabilityMaterial(2, "鋼筋", "kg") },
    new[] { new 崴正.Models.MaterialCapabilityMapping(2, "鋼筋", "結構體工程", "kg") }, duplicateHistory, 5);
Check(duplicateCapability.HistoricalProjectCount == 1 && !duplicateCapability.FormalAnomalyEnabled,
    "30筆交易仍只能計為1個獨立歷史建案，不得補足最低5案");
var wrongUnit = MaterialCapabilityService.Evaluate(
    MaterialCapabilityService.Definitions.Single(x => x.Key == "release-agent"),
    new[] { new 崴正.Models.MaterialCapabilityMaterial(3, "脫模劑", "kg") }, [], [], 5);
Check(wrongUnit.IncompatibleUnitMaterialCount == 1 && wrongUnit.HistoricalProjectCount == 0,
    "缺密度時不可將公斤換算公升作為脫模劑歷史基準");
Check(!TwoAScopePolicy.IsQuantityDetectionCategory("混凝土養護劑") &&
    !TwoAScopePolicy.IsQuantityDetectionCategory("模板脫模劑"),
    "規格試算不會偷偷放寬既有正式異常偵錯範圍");

BulkMaterialTests.Run();
if (args.Length == 2 && args[0] == "--bulk-snapshot") BulkMaterialTests.ReplaySnapshot(args[1]);
if (args.Length == 3 && args[0] == "--factory-workbook") FactoryWorkbookTests.Run(args[1], args[2]);
Console.WriteLine("WorkbookParserTests: all checks passed.");
