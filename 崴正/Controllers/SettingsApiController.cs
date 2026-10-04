using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsApiController : ControllerBase
{
    private readonly EstimationSettingsService _settingsService;
    private readonly CoreMaterialService _coreMaterialService;

    public SettingsApiController(
        EstimationSettingsService settingsService,
        CoreMaterialService coreMaterialService)
    {
        _settingsService = settingsService;
        _coreMaterialService = coreMaterialService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _settingsService.GetCurrentAsync();
        var coreMaterials = await _coreMaterialService.GetMaterialSettingsAsync();

        return Ok(new { settings, coreMaterials });
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] EstimationSettingsUpdateRequest request)
    {
        var result = await _settingsService.UpdateAsync(
            request,
            User.Identity?.Name ?? "System");

        return ToActionResult(result);
    }

    [HttpPut("core-material")]
    public async Task<IActionResult> UpdateCoreMaterial(
        [FromBody] CoreMaterialUpdateRequest request)
    {
        var result = await _coreMaterialService.UpdateCoreStatusAsync(
            request,
            User.Identity?.Name ?? "System");

        return ToActionResult(result);
    }

    private IActionResult ToActionResult(ServiceResult result)
    {
        if (result.Success)
        {
            return Ok(result.Data);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
