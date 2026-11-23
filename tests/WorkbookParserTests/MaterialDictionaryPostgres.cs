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
        var steel = new MaterialMaster {MaterialName = "鋼筋 SD420", UnitMaster = kg, MaterialCategory = category};
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
        var firstRow = await db.FinalBidItems.SingleAsync();
        int projectId = firstRow.ProjectMasterId;
        Check(firstRow.MappingStatus == "Pending" && firstRow.Amount == 4000, "legacy alias pending; original price retained");
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
        Check(importer.Import(Request("wrong-unit", 4, "m3"), actor).Success, "incompatible unit import retained");
        var badRow = await db.FinalBidItems.OrderBy(x => x.FinalBidItemId).LastAsync();
        Check(badRow.MappingIssueCode == "MAPPING_UNIT_MISMATCH" && badRow.Amount == 8000 &&
            !history.CanBecomeHistoricalTransaction(badRow), "unit mismatch blocks quantity history and retains amount");
        Check(mapping.UpdateMaterialMapping(firstRow.FinalBidItemId, alternate.MaterialId, actor).Success, "record conflicting manual review");
        Check(await db.ItemDictionaries.CountAsync(x => x.Keyword.Contains("|")) == 2,
            "conflict keeps both material IDs");
        var freshHistory = new FinalBidService(db, new UnitConversionService(db), audit);
        Check(!freshHistory.CanBecomeHistoricalTransaction(secondRow), "existing Draft Auto becomes ineligible after conflict");
        Check(importer.Import(Request("conflict", 5, "kg"), actor).Success, "import after conflicting review");
        var conflict = await db.FinalBidItems.OrderBy(x => x.FinalBidItemId).LastAsync();
        Check(conflict.MappingStatus == "Pending" && conflict.MappingIssueCode == "DICTIONARY_CONFLICT",
            "conflicting same alias requests review");
        var actions = await db.AuditLogs.Select(x => x.ActionType).ToListAsync();
        Check(actions.Count(x => x == "PublishHistoryBaseline") == 1 &&
              actions.Count(x => x == "ReopenHistoryReview") == 1 &&
              actions.Count(x => x == "UpdateMapping") == 2, "audit records review, publish and reopen exactly once");
    }

    private static FinalBidImportRequest Request(string name, int qty, string unit)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("結構體工程");
        string[] headers = ["項次", "工項名稱", "規格", "單位", "數量", "單價", "複價"];
        for (int i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(2, 1).Value = "1";
        sheet.Cell(2, 2).Value = "竹節鋼筋";
        sheet.Cell(2, 3).Value = "SD420";
        sheet.Cell(2, 4).Value = unit;
        sheet.Cell(2, 5).Value = qty;
        sheet.Cell(2, 6).Value = 2000;
        sheet.Cell(2, 7).Value = qty * 2000;
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
