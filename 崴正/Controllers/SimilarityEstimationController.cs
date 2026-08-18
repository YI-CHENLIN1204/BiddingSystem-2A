using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/similarity-estimation")]
    public class SimilarityEstimationController : ControllerBase
    {
        private readonly SimilarityEstimationService _estimationService;

        public SimilarityEstimationController(
            SimilarityEstimationService estimationService)
        {
            _estimationService = estimationService;
        }

        [HttpGet("{projectId:int}/{materialId:int}")]
        public async Task<IActionResult> Estimate(
            int projectId,
            int materialId,
            [FromQuery] int top = 5)
        {
            if (top <= 0)
            {
                top = 5;
            }

            if (top > 20)
            {
                top = 20;
            }

            var result =
                await _estimationService.EstimateMaterialQuantityAsync(
                    projectId,
                    materialId,
                    top);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "無法取得建案或有效估算資料"
                });
            }

            return Ok(result);
        }
    }
}