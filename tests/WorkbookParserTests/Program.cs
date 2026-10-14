using ClosedXML.Excel;
using 崴正.Services;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    Console.WriteLine("PASS: " + message);
}

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

Console.WriteLine("WorkbookParserTests: all checks passed.");
