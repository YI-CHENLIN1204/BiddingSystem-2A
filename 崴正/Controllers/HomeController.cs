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
    public async Task<IActionResult> GetEstimateRules(string? projectType, string? structureType, decimal floorArea = 0)
    {
        var query =
            from r in _context.QuantityRuleBases
            join m in _context.MaterialMasters
                on r.Material_ID equals m.MaterialId
            select new
            {
                materialName = m.MaterialName,
                unit = m.BaseUnit,
                projectType = r.Project_Type,
                structureType = r.Structure_Type,
                floorZone = r.Floor_Zone,
                unitConsumption = r.Unit_Consumption,
                wasteRatePercent = r.Waste_Rate_Percent
            };

        if (!string.IsNullOrWhiteSpace(projectType))
            query = query.Where(x => x.projectType == projectType);

        if (!string.IsNullOrWhiteSpace(structureType))
            query = query.Where(x => x.structureType == structureType);

        var data = await query.ToListAsync();

        var result = data.Select(x =>
        {
            var estimatedQty = floorArea > 0
                ? Math.Round(floorArea * x.unitConsumption * (1 + x.wasteRatePercent / 100), 2)
                : 0;

            return new
            {
                x.materialName,
                x.unit,
                average = x.unitConsumption,
                median = x.unitConsumption,
                estimatedQty,
                suggestedRange = $"±{x.wasteRatePercent}%",
                max = Math.Round(x.unitConsumption * 1.15m, 2),
                min = Math.Round(x.unitConsumption * 0.85m, 2),
                sampleCount = 1
            };
        });

        return Json(result);
    }
}