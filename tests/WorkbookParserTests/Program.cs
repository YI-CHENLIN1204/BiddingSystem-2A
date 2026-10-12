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
Check(selected.Count == 2, "只解析選定工作表的有效工程明細");
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
Console.WriteLine("WorkbookParserTests: all checks passed.");
