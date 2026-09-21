using 崴正.Data;
using 崴正.Models;
using 崴正.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;


namespace 崴正.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly CoreMaterialService _coreMaterialService;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context ,CoreMaterialService coreMaterialService)
    {
        _logger = logger;
        _context = context;
        _coreMaterialService = coreMaterialService;
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

    public async Task<IActionResult> EstimationUI()
    {
        // ==========================================
        // 1. 指定材料估算需要的全部標準材料
        // ==========================================

        var materials =
            await _context.MaterialMasters
                .AsNoTracking()
                .OrderBy(x =>
                    x.MaterialCategory.CategoryName
                )
                .ThenBy(x =>
                    x.MaterialName
                )
                .Select(x =>
                    new EstimationMaterialOptionDto
                    {
                        MaterialId =
                            x.MaterialId,

                        MaterialName =
                            x.MaterialName,

                        UnitName =
                            x.UnitMaster.UnitName,

                        Category =
                            x.MaterialCategory.CategoryName
                    }
                )
                .ToListAsync();


        // ==========================================
        // 2. 核心材料快速估算
        //
        // 只讀核心材料設定，
        // 不統計 HistoricalTransaction。
        // ==========================================

        var coreSettings =
            await _coreMaterialService
                .GetCoreMaterialOptionsAsync();


        var coreMaterials =
            coreSettings
                .Select(x =>
                    new EstimationCoreMaterialOptionDto
                    {
                        MaterialId =
                            x.MaterialId,

                        MaterialName =
                            x.MaterialName,

                        UnitName =
                            x.UnitName,

                        Category =
                            x.Category,

                        ProjectType =
                            x.ProjectType ?? "",

                        StructureType =
                            x.StructureType ?? ""
                    }
                )
                .ToList();


        var viewModel =
            new EstimationPageViewModel
            {
                Materials =
                    materials,

                CoreMaterials =
                    coreMaterials
            };


        return View(
            viewModel
        );
    }

    public IActionResult UploadFinalBid()
    {
        return View();
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
