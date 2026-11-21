using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/material-capabilities")]
public class MaterialCapabilityController(MaterialCapabilityService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? structureType, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetAsync(structureType, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("specifications")]
    public IActionResult Specifications() => Ok(new { profiles = SpecificationQuantityService.GetProfiles() });

    [HttpPost("specification-estimate")]
    public IActionResult Estimate([FromBody] MaterialSpecificationEstimateRequest request)
    {
        try { return Ok(SpecificationQuantityService.Calculate(request)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
