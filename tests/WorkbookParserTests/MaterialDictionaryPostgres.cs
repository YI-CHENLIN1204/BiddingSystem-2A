using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;

internal static class MaterialDictionaryPostgres
{
    public static async Task RunAsync()
    {
        // This opt-in test can only connect to the dedicated local CI service.
        const string connection = "Host=127.0.0.1;Database=issue3_fixture;Username=issue3_fixture;Password=issue3_fixture";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
        using var db = new ApplicationDbContext(options);
        Check(await db.Database.EnsureCreatedAsync(), "fresh isolated PostgreSQL schema");

        var mass = new UnitCategory {CategoryName = "重量", AllowConversion = true};
        var kg = new UnitMaster {UnitName = "kg", UnitCategory = mass, ToBaseFactor = 1, IsBaseUnit = true};
        var t = new UnitMaster {UnitName = "t", UnitCategory = mass, ToBaseFactor = 1000};
        var category = new MaterialCategory {CategoryName = "結構體工程"};
        var steel = new MaterialMaster {MaterialName = "鋼筋", UnitMaster = kg, MaterialCategory = category};
        var alternate = new MaterialMaster {MaterialName = "鋼筋 其他", UnitMaster = kg, MaterialCategory = category};
        db.AddRange(t, steel, alternate);
        await db.SaveChangesAsync();
        db.ItemDictionaries.Add(new ItemDictionary {Keyword = "竹節鋼筋", Material_Id = steel.MaterialId});
        await db.SaveChangesAsync();

        var audit = new AuditLogService(db);
        var history = new FinalBidService(db, new UnitConversionService(db), audit);
        var mapping = new MaterialMappingService(db, history, audit);
        var importer = new FinalBidImportService(db, history, audit);
        var projects = new ProjectManagementService(db, audit, history);
        const string actor = "issue3-fixture";
        var first = Request("first", 2, "t");
        var result = importer.Import(first, actor);
        Check(result.Success, $"first import: {result.ErrorMessage}");
        var query = new FinalBidQueryService(db, new UnitConversionService(db));
        var firstRow = await db.FinalBidItems.SingleAsync();
        int projectId = firstRow.ProjectMasterId;
        Check(firstRow.MappingStatus == "Pending" && firstRow.Amount == 4000, "legacy alias pending; original price retained");
        Check(Items(query, firstRow.ProjectMasterId, "Pending", null)[0]
            .GetProperty("reviewCandidateMaterialId").GetInt32() == steel.MaterialId,
            "unverified legacy alias appears as a read-only candidate");
        Check(!importer.Import(first, actor).Success, "duplicate file is rejected");
        Check(mapping.UpdateMaterialMapping(firstRow.FinalBidItemId, steel.MaterialId, actor).Success, "manual review and alias learning");

        Check(history.UpdateMajorCategoryOverride(firstRow.FinalBidItemId, "結構體工程", actor).Success,
            "reviewer confirms the major category before publish");
        var idempotency = Guid.NewGuid();
        result = await projects.PublishHistoryAsync(projectId, actor, idempotency);
        Check(result.Success, $"publish: {result.ErrorMessage}");
        Check((await projects.PublishHistoryAsync(projectId, actor, idempotency)).Success, "idempotent publish retry");
        Check(await db.HistoricalTransactions.CountAsync(x => !x.IsDeleted && x.ActualQuantity == 2000) == 1,
            "one normalized history row after retry");
        var reopenKey = Guid.NewGuid();
        Check((await projects.ReopenHistoryReviewAsync(projectId, actor, reopenKey)).Success, "reopen");
        Check((await projects.ReopenHistoryReviewAsync(projectId, actor, reopenKey)).Success, "idempotent reopen retry");
        Check(!await db.HistoricalTransactions.AnyAsync(x => !x.IsDeleted), "reopen deactivates history");

        Check(importer.Import(Request("second", 3, "kg"), actor).Success, "reimport learned alias");
        var secondRow = await db.FinalBidItems.OrderBy(x => x.FinalBidItemId).LastAsync();
        Check(secondRow.MappingStatus == "Auto" && secondRow.MaterialId == steel.MaterialId,
            "learned exact alias is reused");
        Check(history.UpdateMajorCategoryOverride(secondRow.FinalBidItemId, "結構體工程", actor).Success &&
              history.CanBecomeHistoricalTransaction(secondRow),
            "classified Draft Auto is eligible before a conflicting confirmation");
        var badImport = importer.Import(Request("wrong-unit", 4, "m3"), actor);
        Check(badImport.Success && JsonSerializer.SerializeToElement(badImport.Data)
            .GetProperty("unitMismatchCount").GetInt32() == 1, "import reports one incompatible unit");
        var badRow = await db.FinalBidItems.OrderBy(x => x.FinalBidItemId).LastAsync();
        Check(badRow.MappingIssueCode == "MAPPING_UNIT_MISMATCH" && badRow.Amount == 8000 &&
            !history.CanBecomeHistoricalTransaction(badRow), "unit mismatch blocks quantity history and retains amount");
        var mismatches = Items(query, badRow.ProjectMasterId, "Pending", "UNIT_MISMATCH");
        Check(mismatches.GetArrayLength() == 1 &&
              mismatches[0].GetProperty("materialId").ValueKind == JsonValueKind.Null &&
              mismatches[0].GetProperty("unitMismatch").GetBoolean() &&
              JsonSerializer.SerializeToElement(query.GetMappingSummary(badRow.ProjectMasterId).Data)
                  .GetProperty("unitMismatchCount").GetInt32() == 1,
              "unit mismatch totals and filtered Pending list agree");
        Check(history.UpdateMajorCategoryOverride(badRow.FinalBidItemId, "結構體工程", actor).Success,
            "reviewer confirms the price category without confirming quantity");
        Check((await projects.PublishHistoryAsync(badRow.ProjectMasterId, actor)).Success &&
              !await db.HistoricalTransactions.AnyAsync(x => x.FinalBidItemId == badRow.FinalBidItemId && !x.IsDeleted),
            "classified price can publish without an invalid material quantity");
        Check(mapping.UpdateMaterialMapping(firstRow.FinalBidItemId, alternate.MaterialId, actor).Success, "record conflicting manual review");
        Check(await db.ItemDictionaries.CountAsync(x => x.Keyword.Contains("|")) == 2,
            "conflict keeps both material IDs");
        var review = Items(query, secondRow.ProjectMasterId, "Auto", "DRAFT_AUTO_REVIEW");
        Check(review.GetArrayLength() == 1 && review[0].GetProperty("draftAutoNeedsReview").GetBoolean() &&
              !review[0].GetProperty("contributesQuantityHistory").GetBoolean() &&
              review[0].GetProperty("reviewCandidateMaterialId").ValueKind == JsonValueKind.Null,
            "stale Draft Auto appears in review with no ambiguous candidate");
        var freshHistory = new FinalBidService(db, new UnitConversionService(db), audit);
        Check(!freshHistory.CanBecomeHistoricalTransaction(secondRow), "existing Draft Auto becomes ineligible after conflict");
        Check(importer.Import(Request("conflict", 5, "kg"), actor).Success, "import after conflicting review");
        var conflict = await db.FinalBidItems.OrderBy(x => x.FinalBidItemId).LastAsync();
        Check(conflict.MappingStatus == "Pending" && conflict.MappingIssueCode == "DICTIONARY_CONFLICT",
            "conflicting same alias requests review");
        var actions = await db.AuditLogs.Select(x => x.ActionType).ToListAsync();
        Check(actions.Count(x => x == "PublishHistoryBaseline") == 2 &&
              actions.Count(x => x == "ReopenHistoryReview") == 1 &&
              actions.Count(x => x == "UpdateMapping") == 2, "audit records each distinct review, publish and reopen");

        var supplyImport = importer.Import(Request("composite", 6, "kg", "竹節鋼筋供料及施工"), actor);
        Check(supplyImport.Success, "import explicit structural supply and install");
        var supply = await db.FinalBidItems.OrderBy(x => x.FinalBidItemId).LastAsync();
        Check(supply.MappingStatus == "Pending" && supply.MappingIssueCode == "COMPOSITE_QUANTITY_EXCLUDED",
            "visible composite cannot become Auto");
        Check(history.UpdateMajorCategoryOverride(supply.FinalBidItemId, "結構體工程", actor).Success &&
              mapping.UpdateMaterialMapping(supply.FinalBidItemId, steel.MaterialId, actor).Success,
            "manual mapping of composite leaves quantity scope excluded");
        var priceRow = Items(query, supply.ProjectMasterId, "Manual", null)[0];
        Check(priceRow.GetProperty("explicitSupplyAndInstall").GetBoolean() &&
              priceRow.GetProperty("contributesStructuralPrice").GetBoolean() &&
              !priceRow.GetProperty("contributesQuantityHistory").GetBoolean(),
            "read-only list distinguishes composite price from quantity");
        Check((await projects.PublishHistoryAsync(supply.ProjectMasterId, actor)).Success &&
              !await db.HistoricalTransactions.AnyAsync(x => x.FinalBidItemId == supply.FinalBidItemId && !x.IsDeleted),
            "composite publishes structural price without material quantity");

        db.ItemDictionaries.Add(new ItemDictionary { Keyword = "盤元鋼筋", Material_Id = steel.MaterialId });
        await db.SaveChangesAsync();
        Check(importer.Import(Request("review-group", 7, "kg", "盤元鋼筋", ["SD420", "SD420", "SD490"]), actor).Success,
            "import repeated complete keys and one different specification");
        var groupedProject = await db.ProjectMasters.SingleAsync(x => x.ProjectName == "review-group");
        var group = Items(query, groupedProject.ProjectId, "Pending", null).EnumerateArray().ToArray();
        var same = group.Where(x => x.GetProperty("spec").GetString() == "SD420").ToArray();
        Check(group.Length == 3 && same.Length == 2 && same.All(x =>
              x.GetProperty("reviewCandidateMaterialId").GetInt32() == steel.MaterialId),
            "API supplies candidates with two exact source/specification peers");
        foreach (var peer in same)
            Check(mapping.UpdateMaterialMapping(peer.GetProperty("finalBidItemId").GetInt64(), steel.MaterialId, actor).Success,
                "confirm candidate per row");
        var left = Items(query, groupedProject.ProjectId, "Pending", null);
        Check(left.GetArrayLength() == 1 && left[0].GetProperty("spec").GetString() == "SD490" &&
              await db.AuditLogs.CountAsync(x => x.ActionType == "UpdateMapping") == 5,
            "different specification remains Pending and confirmations have separate audits");

    }

    private static JsonElement Items(FinalBidQueryService query, int project, string status, string? issue) =>
        JsonSerializer.SerializeToElement(query.GetMappingItems(project, status, null, issue,
            null, null, null, 1, 50).Data).GetProperty("items");

    private static FinalBidImportRequest Request(string name, int qty, string unit,
        string itemName = "竹節鋼筋", string[]? specs = null)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("結構體工程");
        string[] headers = ["項次", "工項名稱", "規格", "單位", "數量", "單價", "複價"];
        for (int i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        specs ??= ["SD420"];
        for (int i = 0; i < specs.Length; i++)
        {
            int row = i + 2;
            sheet.Cell(row, 1).Value = (i + 1).ToString();
            sheet.Cell(row, 2).Value = itemName;
            sheet.Cell(row, 3).Value = specs[i];
            sheet.Cell(row, 4).Value = unit;
            sheet.Cell(row, 5).Value = qty;
            sheet.Cell(row, 6).Value = 2000;
            sheet.Cell(row, 7).Value = qty * 2000;
        }
        using var bytes = new MemoryStream();
        book.SaveAs(bytes);
        return new FinalBidImportRequest
        {
            ExcelBytes = bytes.ToArray(), FileName = name + ".xlsx", ProjectName = name,
            ProjectType = "住宅", StructureType = "RC", City = "臺中市", ProjectMonth = "2025-01",
            TotalFloorArea = 2000, SiteArea = 500, FloorCount = 5, BasementFloorCount = 0,
            IncludedSheetNames = ["結構體工程"]
        };
    }

    private static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException(label);
        Console.WriteLine("PASS: " + label);
    }
}
