using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsApiController : ControllerBase
{
    private readonly AuditLogService _service;

    public AuditLogsApiController(AuditLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? search = null,
        [FromQuery] string? module = null,
        [FromQuery] string? actionType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await _service.GetPageAsync(
            search,
            module,
            actionType,
            startDate,
            endDate,
            page,
            pageSize);

        return Ok(result);
    }
}
