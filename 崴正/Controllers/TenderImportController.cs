using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/tender-import")]
public class TenderImportController : ControllerBase
{
    // 預匯入數量檢視：只解析明確勾選的工作表，不建立建案或歷史交易。
    [HttpPost("inspect")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Inspect(
        IFormFile excelFile,
        [FromForm] string? includedSheetNames,
        CancellationToken cancellationToken)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest(new { message = "請選擇有效的標單檔案" });

        List<string>? names;
        try
        {
            names = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                includedSheetNames ?? "null");
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { message = "工作表選擇格式錯誤" });
        }

        if (names == null || names.Count == 0 ||
            names.Any(string.IsNullOrWhiteSpace) ||
            names.Count != names.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return BadRequest(new { message = "請明確選擇至少一張有效工作表" });

        try
        {
            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;
            var parser = new TenderExcelParser();
            var rows = parser.Parse(stream, excelFile.FileName, names);
            if (rows.Count == 0)
                return BadRequest(new { message = "選取的工作表沒有可檢查的候選明細" });
            if (rows.Count > 5000)
                return BadRequest(new { message = "候選明細超過 5000 筆，請縮小工作表選擇範圍" });

            return Ok(TenderImportInspectionService.Build(rows));
        }
        catch (Exception ex) when (
            ex is InvalidDataException || ex is ArgumentException ||
            ex is NotSupportedException || ex is IOException)
        {
            return BadRequest(new { message = "檢查標單失敗：" + ex.Message });
        }
    }

    [HttpPost("preview")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile excelFile, CancellationToken cancellationToken)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest(new { message = "請選擇有效的 Excel/CSV 檔案" });

        try
        {
            using var buffer = new MemoryStream();
            await excelFile.CopyToAsync(buffer, cancellationToken);
            var parser = new TenderExcelParser();
            using var nameStream = new MemoryStream(buffer.ToArray());
            var names = parser.GetSheetNames(nameStream, excelFile.FileName);
            using var parseStream = new MemoryStream(buffer.ToArray());
            var rows = parser.Parse(parseStream, excelFile.FileName, names);

            var sheets = names.Select(name =>
            {
                var parsed = rows.Where(x => x.SheetName == name).ToList();
                bool summary = name.Contains("總表");
                bool notDetails = summary || new[] { "詢價", "廠商", "比較表", "報價", "管銷",
                    "說明", "餘白", "備註", "漏列" }.Any(x => name.Contains(x, StringComparison.OrdinalIgnoreCase));
                return new
                {
                    name,
                    itemCount = parsed.Count,
                    sampleItems = parsed.Take(3).Select(x => x.ItemName).ToArray(),
                    recommendation = summary ? "Exclude" : notDetails ? "Review" : "Review",
                    reason = summary ? "彙總表不作為工程明細來源" :
                        notDetails ? "可能是詢價、比較或附屬資料，需人工核對是否屬本建案" :
                        "請確認是否為本建案的實際工程明細，及其價格是否已定案"
                };
            }).ToList();

            return Ok(new
            {
                fileName = excelFile.FileName,
                requiresSelection = names.Count > 1,
                sheets,
                message = "請明確勾選屬於同一建案的工程明細工作表。未勾選的工作表不會匯入；無法辨識的列不會推測數量／單價。"
            });
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is ArgumentException || ex is NotSupportedException || ex is IOException)
        {
            return BadRequest(new { message = "檔案預覽失敗：" + ex.Message });
        }
    }
}
