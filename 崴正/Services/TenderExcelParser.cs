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
        public List<TenderImportRowDto> Parse(Stream excelStream, string fileName, IReadOnlyCollection<string>? includedSheetNames = null)
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
                            convertedFileName,
                            includedSheetNames
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

            var worksheetNames = workbook.Worksheets.Select(x => x.Name).ToList();
            if (includedSheetNames == null && worksheetNames.Count > 1)
                throw new InvalidDataException("此檔案包含多個工作表，請先預覽並明確選擇要匯入的工作表。");
            if (includedSheetNames != null &&
                (includedSheetNames.Count == 0 ||
                 includedSheetNames.Any(x => !worksheetNames.Contains(x, StringComparer.OrdinalIgnoreCase))))
                throw new InvalidDataException("工作表選擇無效，請重新預覽並至少選擇一張有效的工作表。");

            foreach (var ws in workbook.Worksheets)
            {
                if (includedSheetNames != null &&
                    !includedSheetNames.Contains(ws.Name, StringComparer.OrdinalIgnoreCase))
                    continue;

                
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
                int explicitMajorCategoryCol = -1; // 來源表中的逐列工程大項/工種分類
                bool headerDetected = false;

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
                        headerDetected = true;
                   

                        explicitMajorCategoryCol = values.FindIndex(v =>
                            NormalizeText(v) is "工種分類" or "工程大項" or "大項分類");

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
                    if (Regex.IsMatch(col1, @"^[0-9]+\.[0-9]+$") && !string.IsNullOrWhiteSpace(col2) &&
                        !TryGetDecimal(values, qtyCol).HasValue &&
                        !TryGetDecimal(values, priceCol).HasValue &&
                        !TryGetDecimal(values, amountCol).HasValue)
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

                    // 不再猜測「最後三個數字」。右側欄位可能是其他廠商報價。
                    if (!headerDetected || (qtyCol < 0 && priceCol < 0 && amountCol < 0))
                        continue;

                    string itemName = GetCell(values, itemNameCol);
                    string spec = GetCell(values, specCol);
                    string unit = GetCell(values, unitCol);

                    if (string.IsNullOrWhiteSpace(itemName))
                    {
                    
                        continue;
                    }

                    // 無工程量或價格的說明／分類列不寫入明細。
                    if (!qty.HasValue && !price.HasValue && !amount.HasValue)
                        continue;

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
                        Category1 = !string.IsNullOrWhiteSpace(GetCell(values, explicitMajorCategoryCol))
                            ? GetCell(values, explicitMajorCategoryCol)
                            : currentCategory1,
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

        public List<string> GetSheetNames(Stream sourceStream, string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension == ".xls" || extension == ".xlsb" || extension == ".csv")
            {
                using var converted = ConvertToXlsx(sourceStream, extension);
                return GetSheetNames(converted, Path.ChangeExtension(fileName, ".xlsx"));
            }
            if (extension != ".xlsx" && extension != ".xlsm")
                throw new InvalidDataException("不支援的標單格式");
            if (sourceStream.CanSeek) sourceStream.Position = 0;
            using var workbook = new XLWorkbook(sourceStream);
            return workbook.Worksheets.Select(x => x.Name).ToList();
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
            var cells = values.Select(NormalizeText).ToList();
            bool hasName = cells.Any(x => x.Contains("項目名稱") || x.Contains("項目及說明")
                || x.Contains("名稱與說明") || x.Contains("工項名稱") || x.Contains("工作項目")
                || x.Contains("工程項目") || x.Contains("品名") || x.Contains("工料名稱"));
            bool hasQty = cells.Any(IsQuantityHeader);
            bool hasUnit = cells.Any(x => x == "單位" || x == "計量單位");
            bool hasPrice = cells.Any(x => IsUnitPriceHeader(x) || IsAmountHeader(x));
            bool hasCode = cells.Any(x => x == "項次" || x == "編號" || x == "序號");
            return hasName && (hasQty || hasPrice) && (hasUnit || hasCode || (hasQty && hasPrice));
        }

        private static bool IsQuoteHeader(string text) =>
            text.Contains("廠商") || text.Contains("詢價") ||
            text.Contains("比價") || text.Contains("報價") ||
            text.Contains("優惠") || text.Contains("議價");

        private static bool IsQuantityHeader(string text) =>
            !IsQuoteHeader(text) && (
                text == "數量" || text == "工程數量" ||
                text == "自估數量" || text == "原數量" ||
                text == "合約數量" || text == "決標數量" ||
                text == "核定數量" || text == "計價數量");

        private static bool IsUnitPriceHeader(string text) =>
            !IsQuoteHeader(text) && (
                text == "單價" || text == "自估單價" ||
                text == "決標單價" || text == "合約單價" ||
                text == "核定單價" || text == "單價(元)" ||
                text == "單價（元）");

        private static bool IsAmountHeader(string text) =>
            !IsQuoteHeader(text) && (
                text == "複價" || text == "自估複價" ||
                text == "決標複價" || text == "合約複價" ||
                text == "金額" || text == "總價" ||
                text == "工程金額");

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
            // 同名欄位取左側第一欄；右側廠商欄不能覆蓋主標單的基準價格。
            itemCodeCol = -1;
            itemNameCol = -1;
            specCol = -1;
            unitCol = -1;
            qtyCol = -1;
            priceCol = -1;
            amountCol = -1;
            for (int i = 0; i < values.Count; i++)
            {
                string text = NormalizeText(values[i]);
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (itemCodeCol < 0 && (text == "項次" || text == "項目" || text == "編號" || text == "序號"))
                    itemCodeCol = i;
                if (itemNameCol < 0 && (text.Contains("名稱與說明") || text.Contains("項目及說明") ||
                    text.Contains("品名") || text.Contains("工項名稱") || text.Contains("項目名稱") ||
                    text.Contains("工作項目") || text.Contains("工程項目") || text.Contains("工料名稱") || text == "名稱"))
                    itemNameCol = i;
                if (specCol < 0 && (text.Contains("規格") || text.Contains("工料編號") || text.Contains("材料編號")))
                    specCol = i;
                if (unitCol < 0 && (text == "單位" || text == "計量單位"))
                    unitCol = i;
                if (qtyCol < 0 && IsQuantityHeader(text)) qtyCol = i;
                if (priceCol < 0 && IsUnitPriceHeader(text)) priceCol = i;
                if (amountCol < 0 && IsAmountHeader(text)) amountCol = i;
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
