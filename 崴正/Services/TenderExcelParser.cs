using ClosedXML.Excel;
using System.Text.RegularExpressions;
using 崴正.Models;

namespace 崴正.Services
{
    public class TenderExcelParser
    {
        public List<TenderImportRowDto> Parse(Stream excelStream, string fileName)
        {
            var result = new List<TenderImportRowDto>();

            using var workbook = new XLWorkbook(excelStream);

            foreach (var ws in workbook.Worksheets)
            {
                string currentCategory1 = "";
                string currentCategory2 = "";
                string currentCategory3 = "";

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
                int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

                for (int row = 1; row <= lastRow; row++)
                {
                    var values = new List<string>();

                    for (int col = 1; col <= lastCol; col++)
                    {
                        string text = ws.Cell(row, col).GetFormattedString().Trim();
                        values.Add(text);
                    }

                    if (values.All(string.IsNullOrWhiteSpace))
                        continue;

                    string col1 = GetCell(values, 0);
                    string col2 = GetCell(values, 1);
                    string rowText = string.Join("", values);

                    // 過濾常見表頭 / 公司資訊 / 總表說明列
                    if (rowText.Contains("項次") ||
                        rowText.Contains("單位") ||
                        rowText.Contains("數量") ||
                        rowText.Contains("單價") ||
                        rowText.Contains("複價") ||
                        rowText.Contains("股份有限公司") ||
                        rowText.Contains("工程名稱") ||
                        rowText.Contains("總表"))
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

                    bool hasCommonUnit = values.Any(v =>
                        v.Equals("M", StringComparison.OrdinalIgnoreCase) ||
                        v.Equals("M2", StringComparison.OrdinalIgnoreCase) ||
                        v.Equals("M3", StringComparison.OrdinalIgnoreCase) ||
                        v == "式" || v == "座" || v == "個" || v == "只" || v == "組" ||
                        v == "支" || v == "樘" || v == "處" || v == "套" || v == "kg" || v == "KG");

                    if (!hasCommonUnit)
                        continue;

                    decimal? qty = null;
                    decimal? price = null;
                    decimal? amount = null;

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

                    string itemName = GetCell(values, 1);
                    string unit = GetCell(values, 3);

                    if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(unit) || qty == null)
                        continue;

                    if (Regex.IsMatch(col1, @"^[壹貳參肆伍陸柒捌玖拾一二三四五六七八九十]+$"))
                        continue;

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
                        Spec = GetCell(values, 2),
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

        private static string GetCell(List<string> values, int index)
        {
            return index >= 0 && index < values.Count ? values[index] : "";
        }
    }
}
