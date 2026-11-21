using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using 崴正.Data;
using 崴正.Models;

// ONLY disposable localhost Postgres. Never accept a production Supabase URL.
if (args.Length != 2 || args[0] != "--seed-json")
    throw new ArgumentException("Usage: dotnet run --project tools/graduation-synthetic/GraduationSeed -- --seed-json <train-seed.json>");

string connectionString = Environment.GetEnvironmentVariable("GRADUATION_DB_CONNECTION") ?? "";
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("GRADUATION_DB_CONNECTION is required. Never pass production credentials.");
var connection = new NpgsqlConnectionStringBuilder(connectionString);
if (connection.Host is not ("127.0.0.1" or "localhost") ||
    connection.Port != 55432 ||
    connection.Database != "bidding2a_graduation" ||
    connection.Username != "graduation")
    throw new InvalidOperationException("REFUSED: only localhost:55432 / bidding2a_graduation / graduation is permitted.");

using var document = JsonDocument.Parse(await File.ReadAllTextAsync(args[1]));
var root = document.RootElement;
string ReadString(JsonElement row, string key) => row.GetProperty(key).GetString() ??
    throw new InvalidDataException("missing " + key);
int ReadInt(JsonElement row, string key) => row.GetProperty(key).GetInt32();
decimal ReadDecimal(JsonElement row, string key) => decimal.Parse(ReadString(row, key), CultureInfo.InvariantCulture);
DateTime ReadDate(JsonElement row, string key) => DateTime.ParseExact(
    ReadString(row, key), "yyyy-MM-dd", CultureInfo.InvariantCulture);

if (ReadString(root, "data_origin") != "SYNTHETIC" ||
    ReadString(root, "dataset_version") != "2.0.0" ||
    ReadString(root, "split") != "train" ||
    ReadString(root, "target_database") != "bidding2a_graduation")
    throw new InvalidDataException("SYNTHETIC v2 TRAIN seed required.");

var projects = root.GetProperty("project_rows").EnumerateArray().ToArray();
var materials = root.GetProperty("material_rows").EnumerateArray().ToArray();
var boqRows = root.GetProperty("boq_rows").EnumerateArray().ToArray();
var transactions = root.GetProperty("transaction_rows").EnumerateArray().ToArray();
if (projects.Length != ReadInt(root, "train_project_count") ||
    transactions.Length != ReadInt(root, "train_transaction_count") ||
    boqRows.Length != transactions.Length ||
    projects.Length < 1 || materials.Length < 1 || transactions.Length < 1)
    throw new InvalidDataException("Seed record counts mismatch.");
if (projects.Any(x => ReadString(x, "split") != "train"))
    throw new InvalidDataException("Holdout project present in train seed.");

var codeIds = materials.Select(x => ReadString(x, "code")).ToArray();
if (codeIds.Distinct(StringComparer.Ordinal).Count() != codeIds.Length)
    throw new InvalidDataException("Duplicate material codes");
var projectIds = projects.Select(x => ReadString(x, "synthetic_project_id")).ToArray();
if (projectIds.Distinct(StringComparer.Ordinal).Count() != projectIds.Length)
    throw new InvalidDataException("Duplicate project IDs");
var projectIdSet = projectIds.ToHashSet(StringComparer.Ordinal);
var materialCodeSet = codeIds.ToHashSet(StringComparer.Ordinal);
var transactionIdSet = new HashSet<string>(StringComparer.Ordinal);
var sourceByTx = boqRows.ToDictionary(x=>ReadString(x,"synthetic_transaction_id"),x=>x,StringComparer.Ordinal);
if (sourceByTx.Count != boqRows.Length) throw new InvalidDataException("Duplicate BOQ transactions");
foreach (var tx in transactions)
{
    var transactionId = ReadString(tx,"synthetic_transaction_id");
    if (!transactionIdSet.Add(transactionId) ||
        !sourceByTx.TryGetValue(transactionId, out var source) ||
        ReadString(source,"quantity") != ReadString(tx,"quantity") ||
        ReadString(source,"unit_price") == "" || ReadString(source,"synthetic_project_id") != ReadString(tx,"synthetic_project_id") || ReadString(source,"material_code") != ReadString(tx,"material_code") ||
        !projectIdSet.Contains(ReadString(tx,"synthetic_project_id")) ||
        !materialCodeSet.Contains(ReadString(tx,"material_code")) ||
        ReadDecimal(tx,"quantity") <= 0)
        throw new InvalidDataException("Bad transaction lineage or quantity");
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options;
await using var db = new ApplicationDbContext(options);
await db.Database.EnsureCreatedAsync();
// A previous partial run is never merged. Keep rollback straightforward by
// deleting the entire disposable container volume *only with user approval*.
if (await db.ProjectMasters.AnyAsync() ||
    await db.HistoricalTransactions.AnyAsync() ||
    await db.MaterialMasters.AnyAsync() ||
    await db.EstimationSettings.AnyAsync())
    throw new InvalidOperationException("REFUSED: non-empty graduation DB; use a NEW isolated volume/database.");

await using var transaction = await db.Database.BeginTransactionAsync();

var categories = materials.Select(m => ReadString(m, "category"))
    .Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
var cats = new Dictionary<string, int>(StringComparer.Ordinal);
foreach (var cat in categories)
    db.MaterialCategories.Add(new MaterialCategory { CategoryName = cat, IsActive = true, SortOrder = 0 });
await db.SaveChangesAsync();
foreach (var item in await db.MaterialCategories.AsNoTracking().ToListAsync())
    cats[item.CategoryName] = item.CategoryId;

var unitStrings = materials.Select(m => ReadString(m, "unit"))
    .Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
foreach (var unit in unitStrings)
    db.UnitCategories.Add(new UnitCategory { CategoryName = "SYN-" + unit, AllowConversion = false, IsActive = true });
await db.SaveChangesAsync();
var unitCategories = await db.UnitCategories.AsNoTracking().ToDictionaryAsync(x=>x.CategoryName,x=>x.UnitCategoryId);
foreach (var unit in unitStrings)
    db.UnitMasters.Add(new UnitMaster {
        UnitName = unit, UnitCategoryId = unitCategories["SYN-" + unit],
        IsBaseUnit = true, ToBaseFactor = 1, IsActive = true
    });
await db.SaveChangesAsync();
var units = await db.UnitMasters.AsNoTracking().ToDictionaryAsync(x=>x.UnitName,x=>x.UnitId);

var materialIds = new Dictionary<string,int>(StringComparer.Ordinal);
foreach (var row in materials)
    db.MaterialMasters.Add(new MaterialMaster {
        MaterialName = ReadString(row,"name"),
        CategoryId = cats[ReadString(row,"category")],
        UnitId = units[ReadString(row,"unit")],
        IsActive = true
    });
await db.SaveChangesAsync();
var masterRows = await db.MaterialMasters.AsNoTracking()
    .OrderBy(x=>x.MaterialId).ToListAsync();
var catalog = materials.Select(m => (
    Code: ReadString(m,"code"), Name: ReadString(m,"name"),
    Category: ReadString(m,"category"), Unit: ReadString(m,"unit"))).ToArray();
// Resolve by category/name/unit; material name can repeat in separate categories.
foreach (var mat in catalog)
{
    var match = masterRows.Single(x => x.MaterialName == mat.Name &&
        x.CategoryId == cats[mat.Category] && x.UnitId == units[mat.Unit]);
    materialIds[mat.Code] = match.MaterialId;
}

foreach (var (code, name, unit) in new [] {
    ("SYN-REBAR","鋼筋","kg"), ("SYN-CONCRETE","混凝土","m3")
})
{
    if (!materialIds.ContainsKey(code)) continue;
    db.DetectionCategories.Add(new DetectionCategory {
        MajorCategoryName = "結構體工程", DetectionCategoryName = name,
        UnitId = units[unit], IsActive = true, SortOrder = 0
    });
}
await db.SaveChangesAsync();
var detected = await db.DetectionCategories.AsNoTracking().ToListAsync();
foreach (var category in detected)
{
    string code = category.DetectionCategoryName == "鋼筋" ? "SYN-REBAR" : "SYN-CONCRETE";
    db.DetectionCategoryMaterialMaps.Add(new DetectionCategoryMaterialMap {
        DetectionCategoryId = category.DetectionCategoryId,
        MaterialId = materialIds[code], IsActive = true,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    });
}
db.EstimationSettings.Add(new EstimationSetting {
    SettingsId = 1, TopK = 8, MinimumSampleCount = 5,
    LowerQuantile = .15m, MedianQuantile = .50m, UpperQuantile = .85m,
    SimilarityExponent = 1m, TimeHalfLifeYears = 3m,
    MinimumSimilarityScore = 0m,
    StructureWeight = 25m, AreaWeight = 30m, SiteAreaWeight = 10m,
    FloorWeight = 15m, YearWeight = 10m, CityWeight = 10m,
    AreaDifferenceCutoffRatio = 1m, SiteAreaDifferenceCutoffRatio = 1m,
    GroundFloorPenaltyRatio = .10m, BasementFloorPenaltyRatio = .15m,
    YearDifferenceCutoffYears = 20, UpdatedAt = DateTime.UtcNow,
    UpdatedBy = "SYNTHETIC_GRADUATION_SEED"
});
await db.SaveChangesAsync();

foreach (var row in projects)
{
    var project = new ProjectMaster {
        ProjectName = "SYNTHETIC:" + ReadString(row,"synthetic_project_id") + " " +
                      ReadString(row,"project_name"),
        ProjectType = ReadString(row,"project_type"),
        StructureType = ReadString(row,"structure_type"),
        City = ReadString(row,"city"),
        TotalFloorArea = ReadDecimal(row,"total_floor_area"),
        SiteArea = ReadDecimal(row,"site_area"),
        FloorCount = int.Parse(ReadString(row,"floor_count"),CultureInfo.InvariantCulture),
        BasementFloorCount = int.Parse(ReadString(row,"basement_floor_count"),CultureInfo.InvariantCulture),
        StartDate = ReadDate(row,"start_date"),
        HistoryStatus = "Approved", HistoryApprovedAt = DateTime.UtcNow,
        HistoryApprovedBy = "SYNTHETIC_ONLY",
        SourceFileName = "SYNTHETIC_V2_PROJECTS.csv",
        SourceFileHash = ReadString(root,"source_manifest_sha256"),
        IsDeleted = false
    };
    db.ProjectMasters.Add(project);
}
await db.SaveChangesAsync();
var dbProjectIds = (await db.ProjectMasters.AsNoTracking()
    .Select(x=>new {x.ProjectName,x.ProjectId}).ToListAsync())
    .ToDictionary(x=>x.ProjectName.Split(' ')[0]["SYNTHETIC:".Length..],x=>x.ProjectId);

// Preserve full multi-trade synthetic final-bid rows for price reporting and
// auditability, not merely the 2A quantity history. Only TRAIN is seeded.
var finalItems = new List<FinalBidItem>(500);
foreach (var row in boqRows)
{
    if (!projectIdSet.Contains(ReadString(row,"synthetic_project_id")))
        throw new InvalidDataException("TEST holdout BOQ row cannot be seeded");
    var now = DateTime.UtcNow;
    finalItems.Add(new FinalBidItem {
        ProjectMasterId = dbProjectIds[ReadString(row,"synthetic_project_id")],
        SourceFileName = "SYNTHETIC_V2_TRAIN.csv",
        SheetName = ReadString(row,"major_category"),
        ItemCode = ReadString(row,"synthetic_line_id"),
        Category1 = ReadString(row,"major_category"),
        ItemName = ReadString(row,"item_name"),
        Spec = ReadString(row,"spec"),
        Unit = ReadString(row,"unit"),
        Qty = ReadDecimal(row,"quantity"),
        UnitPrice = ReadDecimal(row,"unit_price"),
        Amount = ReadDecimal(row,"amount"),
        MaterialId = materialIds[ReadString(row,"material_code")],
        MappingStatus = "Manual",
        Note = "SYNTHETIC_ONLY; seeded locally for graduation demo",
        IsIgnored = false,
        CreatedAt = now, UpdatedAt = now
    });
    if (finalItems.Count >= 500) {
        db.FinalBidItems.AddRange(finalItems);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        finalItems.Clear();
    }
}
if (finalItems.Count>0) {
    db.FinalBidItems.AddRange(finalItems);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
}
var persistedFinalItems = await db.FinalBidItems.AsNoTracking()
    .Select(x=>new {x.ItemCode,x.FinalBidItemId}).ToListAsync();
var bidItemIdByLine = persistedFinalItems
    .ToDictionary(x=>x.ItemCode ?? "",x=>x.FinalBidItemId,StringComparer.Ordinal);
if (bidItemIdByLine.Count!=boqRows.Length)
    throw new InvalidDataException("Final-bid line count mismatch");

var buffer = new List<HistoricalTransaction>(500);
int inserted = 0;
foreach (var row in transactions)
{
    buffer.Add(new HistoricalTransaction {
        ProjectMasterId = dbProjectIds[ReadString(row,"synthetic_project_id")],
        MaterialId = materialIds[ReadString(row,"material_code")],
        ActualQuantity = ReadDecimal(row,"quantity"),
        TransactionDate = ReadDate(row,"transaction_date"),
        IsOutlier = row.GetProperty("is_outlier").GetBoolean(),
        FinalBidItemId = bidItemIdByLine[ReadString(sourceByTx[ReadString(row,"synthetic_transaction_id")],"synthetic_line_id")],
        IsDeleted = false
    });
    if (buffer.Count >= 500)
    {
        db.HistoricalTransactions.AddRange(buffer);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        inserted += buffer.Count;
        buffer.Clear();
    }
}
if (buffer.Count > 0)
{
    db.HistoricalTransactions.AddRange(buffer);
    await db.SaveChangesAsync();
    inserted += buffer.Count;
}
await transaction.CommitAsync();
Console.WriteLine(JsonSerializer.Serialize(new {
    Database = connection.Database,
    DataOrigin = "SYNTHETIC",
    TrainingProjects = projects.Length,
    MaterialMasters = materials.Length,
    HistoricalTransactions = inserted,
    FinalBidRows = bidItemIdByLine.Count,
    ExcludedHoldoutProjects = ReadInt(root,"excluded_holdout_projects"),
    TrainingOnly = true
}));
