using System.Globalization;
using System.Text.Json;
using 崴正.Services;

static class FactoryWorkbookTests
{
    public static void Run(string workbookPath, string truthPath)
    {
        using var truth = JsonDocument.Parse(File.ReadAllText(truthPath));
        if (truth.RootElement.GetProperty("intentional_defects_enabled").GetBoolean())
            throw new Exception("Factory verification requires complete workbook mode.");
        var parser = new TenderExcelParser();
        var bytes = File.ReadAllBytes(workbookPath);
        var names = parser.GetSheetNames(new MemoryStream(bytes), Path.GetFileName(workbookPath))
            .Where(x => x != "工程總表").ToArray();
        var parsed = parser.Parse(new MemoryStream(bytes), Path.GetFileName(workbookPath), names);
        var expected = truth.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        if (parsed.Count != expected.Length || parsed.Any(x => x.Qty is null or <= 0 || x.UnitPrice is null or <= 0 ||
            x.Amount is null or <= 0 || string.IsNullOrWhiteSpace(x.Unit)))
            throw new Exception("Factory row count or complete numeric fields do not match.");
        foreach (var row in expected)
        {
            var sheet = row.GetProperty("sheet").GetString();
            var rowNumber = row.GetProperty("excel_row").GetInt32();
            var actual = parsed.Single(x => x.SheetName == sheet && x.RowNumber == rowNumber);
            static decimal Number(JsonElement x) => x.ValueKind == JsonValueKind.String
                ? decimal.Parse(x.GetString()!, CultureInfo.InvariantCulture) : x.GetDecimal();
            if (actual.Qty != Number(row.GetProperty("expected_quantity")) ||
                actual.UnitPrice != Number(row.GetProperty("expected_unit_price")) ||
                actual.Amount != Number(row.GetProperty("expected_amount")) ||
                actual.Category1 != row.GetProperty("analysis_major_category").GetString())
                throw new Exception($"Factory parser numeric/category mismatch at {sheet}/{rowNumber}.");
        }
        Console.WriteLine($"PASS: factory XLSX parser source roundtrip, {parsed.Count} rows, {names.Length} detail sheets, all quantity/price/amount/category values matched.");
    }
}
