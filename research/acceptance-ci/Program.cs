using Microsoft.EntityFrameworkCore;
using Npgsql;
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
var project = new ProjectMaster { ProjectName = "CI synthetic draft", TotalFloorArea = 100,
    SiteArea = 100, StartDate = new DateTime(2026, 1, 1), HistoryStatus = "Draft" };
var other = new ProjectMaster { ProjectName = "CI second draft", HistoryStatus = "Draft" };
var item = new FinalBidItem { ProjectMaster = project, SourceFileName = "synthetic.xlsx",
    ItemName = "鋼筋", Category1 = "結構體工程", Unit = "t", Qty = 2, UnitPrice = 5000,
    Amount = 10000, MappingStatus = "Unmapped", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
db.AddRange(kg, tonne, malformed, steel, project, other, item);
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
        item = item.FinalBidItemId, steel = steel.MaterialId }));
