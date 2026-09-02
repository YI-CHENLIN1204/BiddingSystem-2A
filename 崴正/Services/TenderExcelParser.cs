using ClosedXML.Excel;
using System.Text.RegularExpressions;
using ExcelDataReader;
using 崴正.Models;
using System;
using System.Globalization;
using System.Text;

namespace 崴正.Services
{
    public class TenderExcelParser
    {
        public List<TenderImportRowDto> Parse(Stream excelStream, string fileName)
        {
            string extension =
                    Path.GetExtension(fileName)
                        .ToLowerInvariant();

                if (
                    extension == ".xls" ||
                    extension == ".xlsb" ||
                    extension == ".csv"
                )
                {
                    using var convertedStream =
                        ConvertToXlsx(
                            excelStream,
                            extension
                        );

                    var convertedFileName =
                        Path.ChangeExtension(
                            fileName,
                            ".xlsx"
                        );

                    var rows =
                        Parse(
                            convertedStream,
                            convertedFileName
                        );

                    // 保留使用者真正上傳的原始檔名
                    foreach (var row in rows)
                    {
                        row.SourceFileName =
                            fileName;
                    }

                    return rows;
                }

                if (
                    extension != ".xlsx" &&
                    extension != ".xls" &&
                    extension != ".xlsm"&&
                    extension != ".xlsb"&&
                    extension != ".csv"
                )
                {
                    throw new InvalidDataException(
                        "不支援此檔案格式，請使用 .xlsx、.xls、.xlsm、.xlsb 或 .csv"
                    );
                }
            var result = new List<TenderImportRowDto>();

            using var workbook = new XLWorkbook(excelStream);

            foreach (var ws in workbook.Worksheets)
            {

                
                // 跳過總表，因為總表通常是分類彙總，不是實際明細
                if (ws.Name.Contains("總表"))
                {
                    continue;
                }

                string currentCategory1 = "";
                string currentCategory2 = "";
                string currentCategory3 = "";

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

                // 用來記錄欄位位置
                int itemCodeCol = 0;   // 項次 / 項目
                int itemNameCol = 1;   // 名稱與說明 / 項目名稱
                int specCol = 2;       // 規格 / 工料編號
                int unitCol = 3;       // 單位
                int qtyCol = -1;       // 數量
                int priceCol = -1;     // 單價
                int amountCol = -1;    // 複價 / 金額

                for (int row = 1; row <= lastRow; row++)
                {
                    var values = new List<string>();

                    for (int col = 1; col <= lastCol; col++)
                    {
                        string text = ws.Cell(row, col).GetFormattedString().Trim();
                        values.Add(text);
                    }

                    if (values.All(string.IsNullOrWhiteSpace))
                    {
                    
                        continue;
                    }

                    string rowText = string.Join("", values);
                    string normalizedRowText = NormalizeText(rowText);

                    // 如果這列是表頭，就辨識欄位位置
                    if (IsHeaderRow(values))
                    {
                   

                        DetectColumns(
                            values,
                            ref itemCodeCol,
                            ref itemNameCol,
                            ref specCol,
                            ref unitCol,
                            ref qtyCol,
                            ref priceCol,
                            ref amountCol
                        );

                        continue;
                    }

                    string col1 = GetCell(values, itemCodeCol);
                    string col2 = GetCell(values, itemNameCol);

                    // 過濾常見公司資訊 / 總表說明列
                    if (normalizedRowText.Contains("股份有限公司") ||
                        normalizedRowText.Contains("工程名稱") ||
                        normalizedRowText.Contains("總表") ||
                        normalizedRowText.Contains("小計") ||
                        normalizedRowText.Contains("合計") ||
                        normalizedRowText.Contains("總計"))
                    {
                      
                        continue;
                    }

                    // 大分類：壹、貳、參...
                    if (Regex.IsMatch(col1, @"^[壹貳參肆伍陸柒捌玖拾]+$") && !string.IsNullOrWhiteSpace(col2))
                    {
                       

                        currentCategory1 = col2;
                        currentCategory2 = "";
                        currentCategory3 = "";
                        continue;
                    }

                    // 中分類：一、二、三 或 一.
                    if (Regex.IsMatch(col1, @"^[一二三四五六七八九十]+\.?$") && !string.IsNullOrWhiteSpace(col2))
                    {
                       

                        currentCategory2 = col2;
                        currentCategory3 = "";
                        continue;
                    }

                    // 細分類：1.1、2.3...
                    if (Regex.IsMatch(col1, @"^[0-9]+\.[0-9]+$") && !string.IsNullOrWhiteSpace(col2))
                    {
                       

                        currentCategory3 = col2;
                        continue;
                    }




                    decimal? qty = null;
                    decimal? price = null;
                    decimal? amount = null;

                    // 優先依照辨識到的欄位位置抓數量、單價、複價
                    qty = TryGetDecimal(values, qtyCol);
                    price = TryGetDecimal(values, priceCol);
                    amount = TryGetDecimal(values, amountCol);

                    // 如果沒有成功辨識欄位，才使用原本的「最後三個數字」判斷
                    if (qty == null)
                    {
                        var numericValues = values
                            .Select(v => v.Replace(",", "").Trim())
                            .Where(v => decimal.TryParse(v, out _))
                            .Select(v => decimal.Parse(v))
                            .ToList();

                        // 一般標單最後三個數字通常是：數量 / 單價 / 複價
                        if (numericValues.Count >= 3)
                        {
                            qty = numericValues[^3];
                            price = numericValues[^2];
                            amount = numericValues[^1];
                        }
                        else if (numericValues.Count == 2)
                        {
                            qty = numericValues[^2];
                            price = numericValues[^1];
                        }
                    }

                    string itemName = GetCell(values, itemNameCol);
                    string spec = GetCell(values, specCol);
                    string unit = GetCell(values, unitCol);

                    if (string.IsNullOrWhiteSpace(itemName))
                    {
                    
                        continue;
                    }

                    // 只有「沒有數量、沒有單價、沒有複價」的列，才可能是分類列或說明列。
                    // 如果有任何數量或金額資訊，就保留進結果表，交給 Controller 判斷。
                    if (qty == null && price == null && amount == null &&
                        IsSummaryOrCategoryRow(itemName, spec, unit, values))
                    {
                        continue;
                    }

                    // 避免把純分類編號列加入結果
                    if (Regex.IsMatch(col1, @"^[壹貳參肆伍陸柒捌玖拾一二三四五六七八九十]+$"))
                    {
                        continue;
                    }

                    result.Add(new TenderImportRowDto
                    {
                        SourceFileName = fileName,
                        SheetName = ws.Name,
                        RowNumber = row,
                        ItemCode = col1,
                        Category1 = currentCategory1,
                        Category2 = currentCategory2,
                        Category3 = currentCategory3,
                        ItemName = itemName,
                        Spec = spec,
                        Unit = unit,
                        Qty = qty,
                        UnitPrice = price,
                        Amount = amount,
                        Note = string.Join(" | ", values)
                    });
                }
            }

            return result;
        }

        private static MemoryStream ConvertToXlsx(
            Stream sourceStream,
            string extension)
        {
            if (sourceStream.CanSeek)
            {
                sourceStream.Position = 0;
            }

            IExcelDataReader reader;

            if (extension == ".csv")
            {
                reader =
                    ExcelReaderFactory.CreateCsvReader(
                        sourceStream,
                        new ExcelReaderConfiguration
                        {
                            // 台灣常見舊 CSV / Big5
                            FallbackEncoding =
                                Encoding.GetEncoding(950),

                            AutodetectSeparators =
                                new[]
                                {
                                    ',',
                                    ';',
                                    '\t',
                                    '|'
                                }
                        }
                    );
            }
            else
            {
                reader =
                    ExcelReaderFactory.CreateReader(
                        sourceStream
                    );
            }

            using (reader)
            using (var workbook =
                new XLWorkbook())
            {
                int sheetIndex = 1;

                do
                {
                    string sheetName =
                        CreateSafeWorksheetName(
                            reader.Name,
                            sheetIndex,
                            workbook
                        );

                    var worksheet =
                        workbook.Worksheets
                            .Add(sheetName);

                    int rowNumber = 1;

                    while (reader.Read())
                    {
                        for (
                            int columnIndex = 0;
                            columnIndex <
                            reader.FieldCount;
                            columnIndex++)
                        {
                            object? value =
                                reader.GetValue(
                                    columnIndex
                                );

                            worksheet.Cell(
                                rowNumber,
                                columnIndex + 1
                            ).Value =
                                ConvertCellValueToText(
                                    value
                                );
                        }

                        rowNumber++;
                    }

                    sheetIndex++;
                }
                while (reader.NextResult());

                var output =
                    new MemoryStream();

                workbook.SaveAs(
                    output
                );

                output.Position = 0;

                return output;
            }
        }

        private static string ConvertCellValueToText(
            object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(
                    null,
                    CultureInfo.InvariantCulture
                ) ?? string.Empty;
            }

            return value.ToString()?.Trim()
                ?? string.Empty;
        }

        private static string CreateSafeWorksheetName(
            string? originalName,
            int sheetIndex,
            XLWorkbook workbook)
        {
            string name =
                string.IsNullOrWhiteSpace(
                    originalName
                )
                    ? $"工作表{sheetIndex}"
                    : originalName.Trim();

            char[] invalidCharacters =
            {
                ':',
                '\\',
                '/',
                '?',
                '*',
                '[',
                ']'
            };

            foreach (
                char invalidCharacter
                in invalidCharacters)
            {
                name =
                    name.Replace(
                        invalidCharacter,
                        '_'
                    );
            }

            if (name.Length > 31)
            {
                name =
                    name[..31];
            }

            string baseName =
                name;

            int number = 2;

            while (
                workbook.Worksheets.Any(x =>
                    string.Equals(
                        x.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                string suffix =
                    $"_{number}";

                int maxBaseLength =
                    31 - suffix.Length;

                name =
                    baseName[
                        ..Math.Min(
                            baseName.Length,
                            maxBaseLength
                        )
                    ] +
                    suffix;

                number++;
            }

            return name;
        }

        public decimal? ExtractTotalFloorArea(Stream excelStream)
        {
            using var workbook = new XLWorkbook(excelStream);

            var layoutSheets = workbook.Worksheets
                .Where(ws => NormalizeText(ws.Name).Contains("放樣"))
                .ToList();

            foreach (var ws in layoutSheets)
            {
                var area = ExtractTotalFloorAreaFromWorksheet(ws);

                if (area.HasValue)
                    return area.Value;
            }

            return null;
        }

        private static decimal? ExtractTotalFloorAreaFromWorksheet(IXLWorksheet ws)
        {
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int row = 1; row <= lastRow; row++)
            {
                for (int col = 1; col <= lastCol; col++)
                {
                    var label = NormalizeText(ws.Cell(row, col).GetFormattedString());

                    if (!IsFloorAreaLabel(label))
                        continue;

                    var inlineValue = TryExtractDecimalFromText(label);

                    if (inlineValue.HasValue)
                        return inlineValue.Value;

                    var nearbyValue = FindNearbyDecimal(ws, row, col, lastRow, lastCol);

                    if (nearbyValue.HasValue)
                        return nearbyValue.Value;
                }
            }

            return null;
        }

        private static bool IsFloorAreaLabel(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains("總樓地板面積") ||
                   text.Contains("總樓板面積") ||
                   text.Contains("樓地板總面積") ||
                   (text.Contains("樓地板") && text.Contains("面積")) ||
                   text.Contains("放樣面積");
        }

        private static decimal? FindNearbyDecimal(IXLWorksheet ws, int row, int col, int lastRow, int lastCol)
        {
            var candidates = new List<string>();

            for (int targetCol = col + 1; targetCol <= Math.Min(col + 5, lastCol); targetCol++)
            {
                candidates.Add(ws.Cell(row, targetCol).GetFormattedString());
            }

            for (int targetRow = row + 1; targetRow <= Math.Min(row + 2, lastRow); targetRow++)
            {
                for (int targetCol = col; targetCol <= Math.Min(col + 5, lastCol); targetCol++)
                {
                    candidates.Add(ws.Cell(targetRow, targetCol).GetFormattedString());
                }
            }

            foreach (var candidate in candidates)
            {
                var value = TryExtractDecimalFromText(candidate);

                if (value.HasValue && value.Value > 0)
                    return value.Value;
            }

            return null;
        }

        private static decimal? TryExtractDecimalFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var match = Regex.Match(
                text.Replace(",", ""),
                @"[-+]?\d+(?:\.\d+)?");

            if (match.Success && decimal.TryParse(match.Value, out var value))
                return value;

            return null;
        }

        private static bool IsHeaderRow(List<string> values)
        {
            string rowText = string.Join("", values)
                .Replace(" ", "")
                .Replace("　", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            bool hasItemCode = rowText.Contains("項次");
            bool hasItemName =
                rowText.Contains("項目名稱") ||
                rowText.Contains("名稱與說明") ||
                rowText.Contains("項目及說明");
            bool hasSpec = rowText.Contains("規格");
            bool hasUnit = rowText.Contains("單位");
            bool hasQty = rowText.Contains("數量");
            bool hasPrice = rowText.Contains("單價");

            int hitCount = 0;

            if (hasItemCode) hitCount++;
            if (hasItemName) hitCount++;
            if (hasSpec) hitCount++;
            if (hasUnit) hitCount++;
            if (hasQty) hitCount++;
            if (hasPrice) hitCount++;

            // 必須同時命中多個欄位名稱，才判定為真正表頭列
            return hitCount >= 4;
        }

        private static void DetectColumns(
            List<string> values,
            ref int itemCodeCol,
            ref int itemNameCol,
            ref int specCol,
            ref int unitCol,
            ref int qtyCol,
            ref int priceCol,
            ref int amountCol)
        {
            for (int i = 0; i < values.Count; i++)
            {
                string text = NormalizeText(values[i]);

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                /*
                 * 項次 / 項目編號欄
                 * 注意：
                 * 「項目及說明」不是項次欄，所以不能只用 Contains("項目")
                 */
                if (text == "項次" ||
                    text == "項目" ||
                    text == "編號" ||
                    text == "序號")
                {
                    itemCodeCol = i;
                    continue;
                }

                /*
                 * 項目名稱欄
                 * 你的測試資料是「名稱與說明」
                 * 總表常見是「項目及說明」
                 */
                if (text.Contains("名稱與說明") ||
                    text.Contains("項目及說明") ||
                    text.Contains("品名") ||
                    text.Contains("工項名稱") ||
                    text.Contains("項目名稱") ||
                    text == "名稱")
                {
                    itemNameCol = i;
                    continue;
                }

                /*
                 * 規格欄
                 * 注意：
                 * 不要把「名稱與說明」當成規格
                 */
                if (text.Contains("規格") ||
                    text.Contains("工料編號") ||
                    text.Contains("材料編號"))
                {
                    specCol = i;
                    continue;
                }

                if (text == "單位")
                {
                    unitCol = i;
                    continue;
                }

                if (text == "數量" ||
                    text == "工程數量")
                {
                    qtyCol = i;
                    continue;
                }

                if (text == "單價")
                {
                    priceCol = i;
                    continue;
                }

                if (text == "複價" ||
                    text == "金額" ||
                    text == "總價")
                {
                    amountCol = i;
                    continue;
                }
            }
        }

        private static bool IsSummaryOrCategoryRow(string itemName, string spec, string unit, List<string> values)
        {
            string text = NormalizeText(string.Join("", values));
            string normalizedItemName = NormalizeText(itemName);
            string normalizedSpec = NormalizeText(spec);
            string normalizedUnit = NormalizeText(unit);

            if (text.Contains("總表") ||
                text.Contains("小計") ||
                text.Contains("合計") ||
                text.Contains("總計"))
            {
                return true;
            }

            // 常見工程分類名稱，通常不是材料明細
            var categoryKeywords = new[]
            {
                "假設工程",
                "土方工程",
                "基礎工程",
                "安全觀測工程",
                "結構體工程",
                "裝修工程",
                "水電工程",
                "消防工程",
                "景觀工程",
                "電梯工程",
                "門窗工程",
                "防水工程",
                "金屬工程",
                "油漆工程",
                "雜項工程",
                "工程保險費",
                "工程管理費",
                "品質管理",
                "營業稅",
                "勞工安全衛生管理及設備費"
            };

            if (categoryKeywords.Any(k => normalizedItemName == k || normalizedSpec == k))
            {
                return true;
            }

            // 總表分類列常見型態：
            // 品名 = 規格，單位 = 式
            // 例如：假設工程 / 假設工程 / 式 / 1
            if (!string.IsNullOrWhiteSpace(normalizedItemName) &&
                normalizedItemName == normalizedSpec &&
                normalizedUnit == "式")
            {
                return true;
            }

            return false;
        }

        private static decimal? TryGetDecimal(List<string> values, int index)
        {
            if (index < 0 || index >= values.Count)
                return null;

            string text = values[index].Replace(",", "").Trim();

            if (decimal.TryParse(text, out decimal value))
                return value;

            return null;
        }

        private static string GetCell(List<string> values, int index)
        {
            return index >= 0 && index < values.Count ? values[index] : "";
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text
                .Replace(" ", "")
                .Replace("　", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();
        }
    }
}
