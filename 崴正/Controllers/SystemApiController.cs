using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/system")]
public class SystemApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CoreMaterialService _coreMaterialService;

    public SystemApiController(
        ApplicationDbContext context,
        CoreMaterialService coreMaterialService)
    {
        _context = context;
        _coreMaterialService = coreMaterialService;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpGet("options")]
    public async Task<IActionResult> Options()
    {
        var materials = await _context.MaterialMasters
            .AsNoTracking()
            .OrderBy(x => x.MaterialCategory.CategoryName)
            .ThenBy(x => x.MaterialName)
            .Select(x => new EstimationMaterialOptionDto
            {
                MaterialId = x.MaterialId,
                MaterialName = x.MaterialName,
                UnitName = x.UnitMaster.UnitName,
                Category = x.MaterialCategory.CategoryName
            })
            .ToListAsync();

        var coreMaterials = await _coreMaterialService
            .GetCoreMaterialOptionsAsync();

        return Ok(new
        {
            projectTypes = new[] { "住宅", "廠房" },
            structureTypes = new[] { "RC", "SC", "SRC" },
            cities = new[]
            {
                "基隆市", "臺北市", "新北市", "桃園市", "新竹市", "新竹縣",
                "苗栗縣", "臺中市", "彰化縣", "南投縣", "雲林縣", "嘉義市",
                "嘉義縣", "臺南市", "高雄市", "屏東縣", "宜蘭縣", "花蓮縣",
                "臺東縣", "澎湖縣", "金門縣", "連江縣"
            },
            materials,
            coreMaterials
        });
    }
}
