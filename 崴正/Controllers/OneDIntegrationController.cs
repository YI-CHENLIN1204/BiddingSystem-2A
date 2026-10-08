using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/one-d")]
    public class OneDIntegrationController
        : ControllerBase
    {
        private readonly
            OneDIntegrationService
                _oneDIntegrationService;


        public OneDIntegrationController(
            OneDIntegrationService
                oneDIntegrationService)
        {
            _oneDIntegrationService =
                oneDIntegrationService;
        }


        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                configured =
                    _oneDIntegrationService
                        .IsConfigured,

                message =
                    _oneDIntegrationService
                        .IsConfigured
                        ? "1D API 已設定，可進行正式介接測試。"
                        : "1D API 尚未設定；目前不影響既有 2A 功能。"
            });
        }


        [HttpGet(
            "projects/{projectId:int}/result")]
        public async Task<IActionResult>
            GetProjectResult(
                int projectId,
                CancellationToken
                    cancellationToken)
        {
            var result =
                await _oneDIntegrationService
                    .GetProjectResultAsync(
                        projectId,
                        cancellationToken
                    );

            if (!result.Success)
            {
                return StatusCode(
                    result.StatusCode,
                    new
                    {
                        message =
                            result.Message
                    }
                );
            }

            return Ok(new
            {
                source =
                    "1D",

                projectId =
                    result.Data!.ProjectId,

                count =
                    result.Data.Count,

                items =
                    result.Data.Items
            });
        }
    }
}
