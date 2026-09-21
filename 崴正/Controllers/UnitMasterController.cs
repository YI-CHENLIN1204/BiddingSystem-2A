using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/unit-master")]
    public class UnitMasterController
        : ControllerBase
    {
        private readonly UnitMasterService
            _unitMasterService;


        public UnitMasterController(
            UnitMasterService unitMasterService)
        {
            _unitMasterService =
                unitMasterService;
        }


        // ==========================================
        // 新增標準單位
        // ==========================================
        [HttpPost("create")]
        public IActionResult CreateUnit(
            [FromForm] string unitName,
            [FromForm] int unitCategoryId,
            [FromForm] decimal? toBaseFactor)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            var result =
                _unitMasterService
                    .CreateUnit(
                        unitName,
                        unitCategoryId,
                        toBaseFactor,
                        actionUser
                    );

            return ToActionResult(
                result
            );
        }

        [HttpGet]
        public IActionResult GetUnits(
            [FromQuery] string? search,
            [FromQuery] int? unitCategoryId,
            [FromQuery] bool? isActive)
        {
            var result =
                _unitMasterService
                    .GetUnits(
                        search,
                        unitCategoryId,
                        isActive
                    );

            return ToActionResult(
                result
            );
        }

        [HttpPost("update")]
        public IActionResult UpdateUnit(
            [FromForm] int unitId,
            [FromForm] string unitName,
            [FromForm] int unitCategoryId,
            [FromForm] decimal? toBaseFactor)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            var result =
                _unitMasterService
                    .UpdateUnit(
                        unitId,
                        unitName,
                        unitCategoryId,
                        toBaseFactor,
                        actionUser
                    );

            return ToActionResult(
                result
            );
        }

        [HttpPost("set-active")]
        public IActionResult SetUnitActive(
            [FromForm] int unitId,
            [FromForm] bool isActive)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            var result =
                _unitMasterService
                    .SetUnitActive(
                        unitId,
                        isActive,
                        actionUser
                    );

            return ToActionResult(
                result
            );
        }


        // ==========================================
        // ServiceResult → HTTP Response
        // ==========================================
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
