using Microsoft.EntityFrameworkCore;
using Npgsql;
using ClosedXML.Excel;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;

var cs = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));
if (cs.Host != "127.0.0.1" || cs.Database != "research_acceptance" || cs.Username != "research_fixture" ||
    cs.Password != "disposable-ci-only" || cs.SslMode != SslMode.Disable ||
    Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
    throw new InvalidOperationException("Disposable Actions service required.");

await using var connection = new NpgsqlConnection(cs.ConnectionString);
await connection.OpenAsync();
await using var identity = new NpgsqlCommand("SELECT current_database() = 'research_acceptance' AND current_user = 'research_fixture' AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public')", connection);
if (!true.Equals(await identity.ExecuteScalarAsync()))
    throw new InvalidOperationException("Wrong database identity or schema is not empty.");

await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(cs.ConnectionString).Options);
if (!await db.Database.EnsureCreatedAsync())
    throw new InvalidOperationException("Expected new disposable schema.");

var weight = new UnitCategory { CategoryName = "Weight fixture", AllowConversion = true };
var kg = new UnitMaster { UnitName = "kg", IsBaseUnit = true, ToBaseFactor = 1, UnitCategory = weight };
var tonne = new UnitMaster { UnitName = "t", ToBaseFactor = 1000, UnitCategory = weight };
var malformed = new UnitMaster { UnitName = "invalid-factor", ToBaseFactor = 0, UnitCategory = weight };
var category = new MaterialCategory { CategoryName = "鋼筋" };
var steel = new MaterialMaster { MaterialName = "鋼筋", MaterialCategory = category, UnitMaster = kg };
var volume = new UnitCategory { CategoryName = "Volume fixture", AllowConversion = false };
var cubicMetre = new UnitMaster { UnitName = "m3", IsBaseUnit = true, ToBaseFactor = 1, UnitCategory = volume };
var concreteCategory = new MaterialCategory { CategoryName = "混凝土" };
var concrete = new MaterialMaster { MaterialName = "混凝土", MaterialCategory = concreteCategory, UnitMaster = cubicMetre };
var project = new ProjectMaster { ProjectName = "CI synthetic draft", TotalFloorArea = 100,
    SiteArea = 100, StartDate = new DateTime(2026, 1, 1), HistoryStatus = "Draft" };
var other = new ProjectMaster { ProjectName = "CI second draft", HistoryStatus = "Draft" };
var item = new FinalBidItem { ProjectMaster = project, SourceFileName = "synthetic.xlsx",
    ItemName = "鋼筋", Category1 = "結構體工程", Unit = "t", Qty = 2, UnitPrice = 5000,
    Amount = 10000, MappingStatus = "Unmapped", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
db.AddRange(kg, tonne, malformed, steel, concrete, project, other, item);
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
var conversion = new UnitConversionService(db);
var valid = conversion.Convert(2m, 5000m, "t", kg.UnitId);
var invalid = conversion.Convert(1m, 1m, "invalid-factor", kg.UnitId);
if (!valid.Success || valid.Quantity != 2000m || valid.UnitPrice != 5m || invalid.Success)
    throw new InvalidOperationException("EF-backed conversion contract failed.");
Console.WriteLine("PASS EF-backed conversion and invalid-factor rejection");
await File.WriteAllTextAsync("evidence/api/fixture.json",
    System.Text.Json.JsonSerializer.Serialize(new { project = project.ProjectId, other = other.ProjectId,
        item = item.FinalBidItemId, steel = steel.MaterialId, concrete = concrete.MaterialId }));

// Synthetic-only workbook: no independent or partner data is ever committed to the repository.
using (var workbook = new XLWorkbook())
{
    var sheet = workbook.Worksheets.Add("結構體明細");
    var headers = new[] { "項次", "工項名稱", "規格／說明", "單位", "數量", "單價", "複價", "工種分類" };
    for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
    sheet.Cell(2, 1).Value = "1";
    sheet.Cell(2, 2).Value = "SD420W 鋼筋材料";
    sheet.Cell(2, 3).Value = "D19";
    sheet.Cell(2, 4).Value = "kg";
    sheet.Cell(2, 5).Value = 100m;
    sheet.Cell(2, 6).Value = 40m;
    sheet.Cell(2, 7).Value = 4000m;
    sheet.Cell(2, 8).Value = "結構體工程";
    sheet.Cell(3, 1).Value = "2";
    sheet.Cell(3, 2).Value = "預拌混凝土";
    sheet.Cell(3, 3).Value = "3000 PSI";
    sheet.Cell(3, 4).Value = "m3";
    sheet.Cell(3, 5).Value = 10m;
    sheet.Cell(3, 6).Value = 2600m;
    sheet.Cell(3, 7).Value = 26000m;
    sheet.Cell(3, 8).Value = "結構體工程";
    var decoy = workbook.Worksheets.Add("比價表");
    for (var i = 0; i < headers.Length; i++) decoy.Cell(1, i + 1).Value = headers[i];
    decoy.Cell(2, 1).Value = "1";
    decoy.Cell(2, 2).Value = "不得匯入比價表";
    decoy.Cell(2, 4).Value = "式";
    decoy.Cell(2, 5).Value = 1m;
    decoy.Cell(2, 6).Value = 99999m;
    decoy.Cell(2, 7).Value = 99999m;
    decoy.Cell(2, 8).Value = "結構體工程";
    workbook.SaveAs("evidence/api/synthetic-final-bid.xlsx");
}
Console.WriteLine("PASS generated fully synthetic multi-sheet upload fixture");
