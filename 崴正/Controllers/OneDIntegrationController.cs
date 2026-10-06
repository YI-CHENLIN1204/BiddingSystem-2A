using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
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

        private readonly DetectionCategoryService
            _detectionCategoryService;

        public OneDIntegrationController(
            OneDIntegrationService oneDIntegrationService,
            DetectionCategoryService detectionCategoryService)
        {
            _oneDIntegrationService =
                oneDIntegrationService;

            _detectionCategoryService =
                detectionCategoryService;
        }

        /// <summary>
        /// 確認 2A 是否已設定 1D API。
        /// </summary>
        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(
                _oneDIntegrationService
                    .GetStatus()
            );
        }

        /// <summary>
        /// 唯讀取得 1D 已完成並儲存的整個建案分類結果。
        /// </summary>
        [HttpGet(
            "projects/{projectId:int}/result"
        )]
        public async Task<IActionResult>
            GetResult(
                int projectId,
                CancellationToken cancellationToken)
        {
            var result =
                await _oneDIntegrationService
                    .GetSavedResultAsync(
                        projectId,
                        cancellationToken
                    );

            return ToActionResult(
                result
            );
        }

        /// <summary>
        /// 以建案為單位取得 1D 全部工程大項，
        /// 再預覽 2A 已建立的偵錯細項分類。
        /// </summary>
        [HttpGet(
            "projects/{projectId:int}/detection-preview"
        )]
        public async Task<IActionResult>
            GetDetectionPreview(
                int projectId,
                CancellationToken cancellationToken)
        {
            var result =
                await _oneDIntegrationService
                    .GetDetectionPreviewAsync(
                        projectId,
                        cancellationToken
                    );

            return ToActionResult(
                result
            );
        }

        /// <summary>
        /// 不連線 1D 的本地建案分類預覽。
        /// 測試資料可以同時包含多個 1D 工程大項；
        /// 2A 只對已建立規則的大項往下做偵錯細項分類。
        /// </summary>
        [HttpPost("detection-preview")]
        public IActionResult
            PreviewDetectionCategory(
                [FromBody]
                DetectionCategoryPreviewRequest request)
        {
            if (
                request == null ||
                request.Items == null ||
                request.Items.Count == 0
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "請至少提供一筆 1D 建案工項"
                    }
                );
            }

            return Ok(
                _detectionCategoryService
                    .BuildPreview(
                        request.Items
                    )
            );
        }

        private IActionResult ToActionResult(
            ServiceResult result)
        {
            if (result.Success)
            {
                return Ok(
                    result.Data
                );
            }

            return result.ErrorType switch
            {
                ServiceErrorType.NotFound =>
                    NotFound(
                        new
                        {
                            message =
                                result.ErrorMessage
                        }
                    ),

                ServiceErrorType.Conflict =>
                    Conflict(
                        new
                        {
                            message =
                                result.ErrorMessage
                        }
                    ),

                _ =>
                    BadRequest(
                        new
                        {
                            message =
                                result.ErrorMessage
                        }
                    )
            };
        }
    }
}
