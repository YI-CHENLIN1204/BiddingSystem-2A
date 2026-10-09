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
        private readonly
            OneDIntegrationService
                _oneDIntegrationService;

        private readonly
            TwoAPreprocessService
                _twoAPreprocessService;

        private readonly
            ProjectSimilarityService
                _projectSimilarityService;


        public OneDIntegrationController(
            OneDIntegrationService
                oneDIntegrationService,
            TwoAPreprocessService
                twoAPreprocessService,
            ProjectSimilarityService
                projectSimilarityService)
        {
            _oneDIntegrationService =
                oneDIntegrationService;

            _twoAPreprocessService =
                twoAPreprocessService;

            _projectSimilarityService =
                projectSimilarityService;
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


        [HttpPost(
            "projects/{projectId:int}/analysis-preview")]
        public async Task<IActionResult>
            GetAnalysisPreview(
                int projectId,
                [FromBody]
                TwoAIntegratedAnalysisPreviewRequest
                    request,
                CancellationToken
                    cancellationToken)
        {
            if (request == null ||
                request.Target == null)
            {
                return BadRequest(new
                {
                    message =
                        "請提供 2A 相似建案分析所需的建案條件。"
                });
            }

            if (request.TopCount.HasValue &&
                request.TopCount.Value <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "TopCount 必須大於 0。"
                });
            }

            var source =
                await _oneDIntegrationService
                    .GetProjectResultAsync(
                        projectId,
                        cancellationToken
                    );

            if (!source.Success)
            {
                return StatusCode(
                    source.StatusCode,
                    new
                    {
                        message =
                            source.Message
                    }
                );
            }

            try
            {
                var preprocess =
                    _twoAPreprocessService
                        .Build(
                            projectId,
                            source.Data!.Items
                        );

                var similarProjects =
                    await _projectSimilarityService
                        .FindSimilarProjectsAsync(
                            request.Target,
                            request.TopCount
                        );

                return Ok(
                    new
                    TwoAIntegratedAnalysisPreviewResponse
                    {
                        OneDProjectId =
                            projectId,

                        Target =
                            request.Target,

                        Preprocess =
                            preprocess,

                        SimilarProjects =
                            similarProjects
                    }
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }


        [HttpGet(
            "projects/{projectId:int}/preprocess")]
        public async Task<IActionResult>
            GetProjectPreprocess(
                int projectId,
                CancellationToken
                    cancellationToken)
        {
            var source =
                await _oneDIntegrationService
                    .GetProjectResultAsync(
                        projectId,
                        cancellationToken
                    );

            if (!source.Success)
            {
                return StatusCode(
                    source.StatusCode,
                    new
                    {
                        message =
                            source.Message
                    }
                );
            }

            var result =
                _twoAPreprocessService
                    .Build(
                        projectId,
                        source.Data!.Items
                    );

            return Ok(result);
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
