using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/one-d")]
    public class OneDIntegrationController
        : ControllerBase
    {
        private readonly OneDIntegrationService
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
                    _oneDIntegrationService.IsConfigured,

                message =
                    _oneDIntegrationService.IsConfigured
                        ? "1D API 已設定。正式 2A 流程會先把 1D 結果匯入 AnalysisCase 快照，再從快照反覆偵錯。"
                        : "1D API 尚未設定；2A 仍可使用自行上傳方式建立待偵錯案件。"
            });
        }


        // 僅供介接檢查 / 預覽。
        // 正式偵錯流程不可直接依賴這支 API，
        // 必須先透過 /api/analysis-cases/from-1d/{projectId}
        // 建立 2A 自己的版本快照。
        [HttpGet("projects/{projectId:int}/result")]
        public async Task<IActionResult> GetProjectResult(
            int projectId,
            CancellationToken cancellationToken)
        {
            var result =
                await _oneDIntegrationService
                    .GetProjectResultAsync(
                        projectId,
                        cancellationToken);

            if (!result.Success)
            {
                return StatusCode(
                    result.StatusCode,
                    new
                    {
                        message =
                            result.Message
                    });
            }

            return Ok(new
            {
                source = "1D",
                snapshotRequired = true,
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
