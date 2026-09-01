using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/range-estimation")]
    public class RangeEstimationController : ControllerBase
    {
        private readonly RangeEstimationService
            _rangeEstimationService;

        private readonly BatchEstimationService
            _batchEstimationService;

        public RangeEstimationController(
            RangeEstimationService rangeEstimationService,
            BatchEstimationService batchEstimationService)
        {
            _rangeEstimationService =
                rangeEstimationService;

            _batchEstimationService =
                batchEstimationService;
        }


        [HttpGet("material")]
        public async Task<IActionResult>
            EstimateMaterial(
                [FromQuery] int targetProjectId,
                [FromQuery] int materialId)
        {
            var result =
                await _rangeEstimationService
                    .EstimateMaterialRangeAsync(
                        targetProjectId,
                        materialId
                    );

            if (result.Success)
            {
                return Ok(result.Data);
            }

            return result.ErrorType switch
            {
                ServiceErrorType.NotFound =>
                    NotFound(new
                    {
                        message =
                            result.ErrorMessage
                    }),

                ServiceErrorType.Conflict =>
                    Conflict(new
                    {
                        message =
                            result.ErrorMessage
                    }),

                _ =>
                    BadRequest(new
                    {
                        message =
                            result.ErrorMessage
                    })
            };
        }

        [HttpPost("batch")]
        public async Task<IActionResult>
            EstimateBatch(
                [FromBody]
                BatchRangeEstimationRequest request)
        {
            var result =
                await _batchEstimationService
                    .EstimateMaterialsAsync(
                        request.Target,
                        request.MaterialIds
                    );

            if (result.Success)
            {
                return Ok(result.Data);
            }

            return result.ErrorType switch
            {
                ServiceErrorType.NotFound =>
                    NotFound(new
                    {
                        message =
                            result.ErrorMessage
                    }),

                ServiceErrorType.Conflict =>
                    Conflict(new
                    {
                        message =
                            result.ErrorMessage
                    }),

                _ =>
                    BadRequest(new
                    {
                        message =
                            result.ErrorMessage
                    })
            };
        }

        [HttpPost("core")]
        public async Task<IActionResult> EstimateCore(
            [FromBody] EstimationTargetRequest target)
        {
            var result =
                await _batchEstimationService
                    .EstimateCoreMaterialsAsync(target);

            if (result.Success)
            {
                return Ok(result.Data);
            }

            return result.ErrorType switch
            {
                ServiceErrorType.NotFound =>
                    NotFound(new
                    {
                        message = result.ErrorMessage
                    }),

                ServiceErrorType.Conflict =>
                    Conflict(new
                    {
                        message = result.ErrorMessage
                    }),

                _ =>
                    BadRequest(new
                    {
                        message = result.ErrorMessage
                    })
            };
        }
    }
}