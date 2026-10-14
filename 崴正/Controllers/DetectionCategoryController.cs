using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/detection-category")]
    public class DetectionCategoryController
        : ControllerBase
    {
        private readonly DetectionCategoryService
            _detectionCategoryService;

        private readonly DetectionCategoryRangeService
            _detectionCategoryRangeService;

        public DetectionCategoryController(
            DetectionCategoryService
                detectionCategoryService,
            DetectionCategoryRangeService
                detectionCategoryRangeService)
        {
            _detectionCategoryService =
                detectionCategoryService;

            _detectionCategoryRangeService =
                detectionCategoryRangeService;
        }


        [HttpGet]
        public IActionResult GetCategories()
        {
            return ToActionResult(
                _detectionCategoryService
                    .GetCategories()
            );
        }


        [HttpGet("readiness")]
        public IActionResult GetReadiness()
        {
            return ToActionResult(
                _detectionCategoryService
                    .GetReadiness()
            );
        }


        [HttpGet("mappings")]
        public IActionResult GetMappings(
            [FromQuery]
            int? detectionCategoryId = null)
        {
            return ToActionResult(
                _detectionCategoryService
                    .GetMappings(
                        detectionCategoryId
                    )
            );
        }


        [HttpPost("create")]
        public IActionResult CreateCategory(
            [FromForm]
            string majorCategoryName,
            [FromForm]
            string detectionCategoryName,
            [FromForm]
            int unitId)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            return ToActionResult(
                _detectionCategoryService
                    .CreateCategory(
                        majorCategoryName,
                        detectionCategoryName,
                        unitId,
                        actionUser
                    )
            );
        }


        [HttpPost("assign-material")]
        public IActionResult AssignMaterial(
            [FromForm]
            int detectionCategoryId,
            [FromForm]
            int materialId)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            return ToActionResult(
                _detectionCategoryService
                    .AssignMaterial(
                        detectionCategoryId,
                        materialId,
                        actionUser
                    )
            );
        }


        [HttpPost("{detectionCategoryId:int}/range")]
        public async Task<IActionResult> EstimateRange(
            int detectionCategoryId,
            [FromBody]
            DetectionCategoryRangeRequest request)
        {
            var result =
                await _detectionCategoryRangeService
                    .EstimateAsync(
                        detectionCategoryId,
                        request
                    );

            return ToActionResult(result);
        }


        [HttpPost("unassign-material")]
        public IActionResult UnassignMaterial(
            [FromForm]
            int materialId)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            return ToActionResult(
                _detectionCategoryService
                    .UnassignMaterial(
                        materialId,
                        actionUser
                    )
            );
        }


        private IActionResult ToActionResult(
            ServiceResult result)
        {
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
    }
}
