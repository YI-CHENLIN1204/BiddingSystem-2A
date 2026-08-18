using 崴正.Data;
using 崴正.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;


namespace 崴正.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }

    // 課程版暫不開放工程估算功能。
    // 產學版未來可將此頁作為 quantity_rule_base 規則表計算引擎入口。
    public IActionResult EstimationUI()
    {
        return View();
    }

    public IActionResult UploadFinalBid()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetSimilarProjects(
        string? projectType,
        string? structureType,
        int? yearStart,
        int? yearEnd,
        string? city,
        decimal floorArea = 0)
    {
        if (floorArea < 0)
            return BadRequest(new { message = "總樓地板面積不可小於 0" });

        if (yearStart.HasValue && yearEnd.HasValue && yearStart.Value > yearEnd.Value)
            return BadRequest(new { message = "估算年度起不可大於估算年度迄" });

        projectType = NormalizeFilter(projectType);
        structureType = NormalizeFilter(structureType);
        city = NormalizeFilter(city);

        var query = _context.ProjectMasters
            .Where(p => p.IsDeleted == false);

        if (!string.IsNullOrWhiteSpace(projectType))
            query = query.Where(p => p.ProjectType == projectType);

        if (!string.IsNullOrWhiteSpace(structureType))
            query = query.Where(p => p.StructureType == structureType);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City == city);

        if (yearStart.HasValue)
            query = query.Where(p => p.StartDate.HasValue && p.StartDate.Value.Year >= yearStart.Value);

        if (yearEnd.HasValue)
            query = query.Where(p => p.StartDate.HasValue && p.StartDate.Value.Year <= yearEnd.Value);

        var projects = await query
            .Select(p => new
            {
                p.ProjectId,
                p.ProjectName,
                p.ProjectType,
                p.StructureType,
                p.City,
                p.TotalFloorArea,
                p.StartDate
            })
            .ToListAsync();

        var projectIds = projects.Select(p => p.ProjectId).ToList();
        var transactionCounts = await _context.HistoricalTransactions
            .Where(h => h.ProjectMasterId.HasValue && projectIds.Contains(h.ProjectMasterId.Value))
            .Where(h => h.IsDeleted == false)
            .GroupBy(h => h.ProjectMasterId ?? 0)
            .Select(g => new
            {
                projectId = g.Key,
                count = g.Count()
            })
            .ToDictionaryAsync(x => x.projectId, x => x.count);

        var result = projects
            .Select(p =>
            {
                decimal? areaDifference = floorArea > 0 && p.TotalFloorArea.HasValue
                    ? Math.Abs(p.TotalFloorArea.Value - floorArea)
                    : null;

                decimal? areaDifferencePercent = areaDifference.HasValue && floorArea > 0
                    ? Math.Round(areaDifference.Value / floorArea * 100m, 1)
                    : null;

                return new
                {
                    projectId = p.ProjectId,
                    projectName = p.ProjectName,
                    projectType = string.IsNullOrWhiteSpace(p.ProjectType) ? "未設定" : p.ProjectType,
                    structureType = string.IsNullOrWhiteSpace(p.StructureType) ? "未設定" : p.StructureType,
                    city = string.IsNullOrWhiteSpace(p.City) ? "未設定" : p.City,
                    totalFloorArea = p.TotalFloorArea,
                    startDate = p.StartDate?.ToString("yyyy/MM/dd"),
                    transactionCount = transactionCounts.TryGetValue(p.ProjectId, out var count) ? count : 0,
                    areaDifference,
                    areaDifferencePercent
                };
            })
            .OrderBy(x => x.areaDifference ?? decimal.MaxValue)
            .ThenByDescending(x => x.transactionCount)
            .ThenByDescending(x => x.startDate)
            .Take(10)
            .ToList();

        return Json(new
        {
            totalCount = result.Count,
            items = result
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetEstimateRules(
        string? projectType,
        string? structureType,
        int? yearStart,
        int? yearEnd,
        string? city,
        decimal floorArea = 0)
    {
        if (floorArea < 0)
            return BadRequest(new { message = "總樓地板面積不可小於 0" });

        if (yearStart.HasValue && yearEnd.HasValue && yearStart.Value > yearEnd.Value)
            return BadRequest(new { message = "估算年度起不可大於估算年度迄" });

        projectType = NormalizeFilter(projectType);
        structureType = NormalizeFilter(structureType);
        city = NormalizeFilter(city);

        var rules = await (
            from r in _context.QuantityRuleBases
            join m in _context.MaterialMasters
                on r.Material_ID equals m.MaterialId
            select new
            {
                ruleId = r.RuleID,
                materialId = m.MaterialId,
                materialName = m.MaterialName,
                materialCategory = m.Category,
                unit = m.BaseUnit,
                projectType = r.Project_Type,
                structureType = r.Structure_Type,
                floorZone = r.Floor_Zone,
                unitConsumption = r.Unit_Consumption,
                wasteRatePercent = r.Waste_Rate_Percent
            }).ToListAsync();

        var matchedRules = rules
            .Where(x => IsFilterMatch(x.projectType, projectType))
            .Where(x => IsFilterMatch(x.structureType, structureType))
            .GroupBy(x => new { x.materialId, floorZone = x.floorZone ?? "" })
            .Select(g => g
                .OrderByDescending(x => GetSpecificScore(x.projectType, projectType) + GetSpecificScore(x.structureType, structureType))
                .ThenByDescending(x => x.ruleId)
                .First())
            .OrderBy(x => x.materialName)
            .ThenBy(x => x.floorZone)
            .ToList();

        var materialIds = matchedRules.Select(x => x.materialId).Distinct().ToList();

        var historyQuery = _context.HistoricalTransactions
            .Where(h => materialIds.Contains(h.MaterialId))
            .Where(h => h.IsDeleted == false)
            .Where(h => h.IsOutlier == false);

        if (!string.IsNullOrWhiteSpace(projectType) ||
            !string.IsNullOrWhiteSpace(structureType) ||
            !string.IsNullOrWhiteSpace(city))
        {
            historyQuery =
                from h in historyQuery
                join p in _context.ProjectMasters
                    on h.ProjectMasterId equals (int?)p.ProjectId
                where p.IsDeleted == false
                where string.IsNullOrWhiteSpace(projectType) || p.ProjectType == projectType
                where string.IsNullOrWhiteSpace(structureType) || p.StructureType == structureType
                where string.IsNullOrWhiteSpace(city) || p.City == city
                select h;
        }

        if (yearStart.HasValue)
            historyQuery = historyQuery.Where(h => h.TransactionDate.Year >= yearStart.Value);

        if (yearEnd.HasValue)
            historyQuery = historyQuery.Where(h => h.TransactionDate.Year <= yearEnd.Value);

        var historyDict = (await historyQuery.ToListAsync())
            .GroupBy(h => h.MaterialId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = matchedRules.Select(x =>
        {
            var wasteRate = x.wasteRatePercent / 100m;
            var baseQty = floorArea > 0 ? floorArea * x.unitConsumption : 0m;
            var estimatedQty = Math.Round(baseQty * (1 + wasteRate), 2);
            var minSuggestedQty = Math.Round(baseQty, 2);
            var maxSuggestedQty = estimatedQty;

            historyDict.TryGetValue(x.materialId, out var histories);
            histories ??= new List<HistoricalTransaction>();

            decimal? avgUnitPrice = histories.Count > 0
                ? Math.Round(histories.Average(h => h.UnitPrice), 2)
                : null;

            decimal? estimatedPrice = avgUnitPrice.HasValue && estimatedQty > 0
                ? Math.Round(avgUnitPrice.Value * estimatedQty, 0)
                : null;

            var statusMessage = GetEstimateStatus(floorArea, histories.Count, avgUnitPrice);

            return new
            {
                x.materialId,
                x.materialName,
                estimateGroup = ResolveEstimateGroup(x.materialCategory, x.materialName, x.floorZone),
                displayOrder = ResolveEstimateDisplayOrder(x.materialCategory, x.materialName, x.floorZone),
                floorZone = string.IsNullOrWhiteSpace(x.floorZone) ? "通用" : x.floorZone,
                projectType = string.IsNullOrWhiteSpace(x.projectType) ? "通用" : x.projectType,
                structureType = string.IsNullOrWhiteSpace(x.structureType) ? "通用" : x.structureType,
                x.unit,
                unitConsumption = Math.Round(x.unitConsumption, 4),
                wasteRatePercent = Math.Round(x.wasteRatePercent, 2),
                estimatedQty,
                minSuggestedQty,
                maxSuggestedQty,
                suggestedRange = $"{minSuggestedQty:N2} ~ {maxSuggestedQty:N2}",
                historicalAvgUnitPrice = avgUnitPrice,
                estimatedPrice,
                sampleCount = histories.Count,
                statusMessage
            };
        })
        .OrderBy(x => x.displayOrder)
        .ThenBy(x => x.materialName)
        .ToList();

        return Json(new
        {
            totalCount = result.Count,
            floorArea,
            cityApplied = false,
            message = null as string,
            items = result
        });
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsFilterMatch(string? ruleValue, string? selectedValue)
    {
        if (string.IsNullOrWhiteSpace(selectedValue))
            return true;

        return string.IsNullOrWhiteSpace(ruleValue) || ruleValue.Trim() == selectedValue;
    }

    private static int GetSpecificScore(string? ruleValue, string? selectedValue)
    {
        if (string.IsNullOrWhiteSpace(selectedValue))
            return 0;

        return ruleValue?.Trim() == selectedValue ? 10 : 0;
    }

    private static string GetEstimateStatus(decimal floorArea, int sampleCount, decimal? avgUnitPrice)
    {
        if (floorArea <= 0)
            return "請輸入樓地板面積";

        if (sampleCount == 0 || !avgUnitPrice.HasValue)
            return "已估數量，缺少歷史單價";

        return "已完成估算";
    }

    private static string ResolveEstimateGroup(string? category, string materialName, string? floorZone)
    {
        var text = $"{category} {materialName} {floorZone}";

        if (text.Contains("鷹架"))
            return "鷹架工程";

        if (text.Contains("牆面鋼網牆") || text.Contains("牆面鋼網") || text.Contains("鋼網牆"))
            return "牆面鋼網牆";

        return "結構體單位用量";
    }

    private static int ResolveEstimateDisplayOrder(string? category, string materialName, string? floorZone)
    {
        var group = ResolveEstimateGroup(category, materialName, floorZone);
        var name = materialName.Trim();

        if (group == "鷹架工程")
            return 100 + GetItemOrder(name, new[] { "外牆鷹架", "鷹架" });

        if (group == "牆面鋼網牆")
            return 300 + GetItemOrder(name, new[] { "鋼筋", "混凝土" });

        return 200 + GetItemOrder(name, new[]
        {
            "混凝土",
            "鋼筋",
            "模板",
            "鋼承鈑",
            "屋頂彩鋼",
            "鋼骨",
            "版面點",
            "鋼絲網"
        });
    }

    private static int GetItemOrder(string materialName, string[] keywords)
    {
        for (var index = 0; index < keywords.Length; index++)
        {
            if (materialName.Contains(keywords[index]))
                return index;
        }

        return 99;
    }
}
