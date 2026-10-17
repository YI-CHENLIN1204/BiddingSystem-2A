using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsApiController : ControllerBase
{
    private readonly ProjectManagementService _service;

    public ProjectsApiController(ProjectManagementService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search = null)
    {
        var items = await _service.GetProjectsAsync(search);
        return Ok(new { items });
    }

    [HttpGet("{projectId:int}")]
    public async Task<IActionResult> Get(int projectId) =>
        ToActionResult(await _service.GetProjectAsync(projectId));

    [HttpPut("{projectId:int}")]
    public async Task<IActionResult> Update(
        int projectId,
        [FromBody] ProjectSaveRequest request) =>
        ToActionResult(await _service.UpdateProjectAsync(
            projectId,
            request,
            User.Identity?.Name ?? "System"));

    [HttpGet("{projectId:int}/history-operation/{operationId:guid}")]
    public async Task<IActionResult> HistoryOperation(
        int projectId, Guid operationId) =>
        ToActionResult(await _service.GetHistoryOperationReceiptAsync(
            projectId, operationId));

    [HttpPost("{projectId:int}/publish-history")]
    public async Task<IActionResult> PublishHistory(
        int projectId,
        [FromHeader(Name = "Idempotency-Key")] Guid? operationId = null) =>
        ToActionResult(
            await _service.PublishHistoryAsync(
                projectId,
                User.Identity?.Name ?? "System",
                operationId
            )
        );

    [HttpPost("{projectId:int}/reopen-history-review")]
    public async Task<IActionResult> ReopenHistoryReview(
        int projectId,
        [FromHeader(Name = "Idempotency-Key")] Guid? operationId = null) =>
        ToActionResult(
            await _service.ReopenHistoryReviewAsync(
                projectId,
                User.Identity?.Name ?? "System",
                operationId
            )
        );

    [HttpDelete("{projectId:int}")]
    public async Task<IActionResult> Delete(int projectId) =>
        ToActionResult(await _service.DeleteProjectAsync(
            projectId,
            User.Identity?.Name ?? "System"));

    [HttpGet("{projectId:int}/final-bid-items")]
    public async Task<IActionResult> FinalBidItems(
        int projectId,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        ToActionResult(await _service.GetFinalBidItemsAsync(
            projectId,
            search,
            page,
            pageSize));

    [HttpGet("{projectId:int}/2a-summary")]
    public async Task<IActionResult> TwoASummary(
        int projectId) =>
        ToActionResult(
            await _service.GetTwoASummaryAsync(
                projectId
            )
        );

    [HttpGet("{projectId:int}/historical-data")]
    public async Task<IActionResult> HistoricalData(int projectId) =>
        ToActionResult(await _service.GetHistoricalDataAsync(projectId));

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
