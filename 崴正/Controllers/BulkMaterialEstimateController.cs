using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/bulk-material-estimates")]
public class BulkMaterialEstimateController(BulkMaterialEstimateService service) : ControllerBase
{
    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct) => Ok(await service.GetOptionsAsync(ct));

    [HttpPost("range")]
    public async Task<IActionResult> Estimate([FromBody] BulkMaterialEstimateRequest request, CancellationToken ct)
    {
        try { return Ok(await service.EstimateAsync(request, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

