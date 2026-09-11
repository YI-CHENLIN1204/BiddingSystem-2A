using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/standard-material")]
    public class StandardMaterialController
        : ControllerBase
    {
        private readonly StandardMaterialService
            _standardMaterialService;

        public StandardMaterialController(
            StandardMaterialService standardMaterialService)
        {
            _standardMaterialService =
                standardMaterialService;
        }

        // ==========================================
        // 新增標準材料
        // ==========================================
        [HttpPost("create")]
        public IActionResult CreateMaterial(
            [FromForm] string materialName,
            [FromForm] int categoryId,
            [FromForm] string baseUnit)
        {
            var result =
                _standardMaterialService
                    .CreateMaterial(
                        materialName,
                        categoryId,
                        baseUnit
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // 查詢標準材料
        // ==========================================
        [HttpGet]
        public IActionResult GetMaterials(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] string? baseUnit,
            [FromQuery] bool? isActive)
        {
            var result =
                _standardMaterialService
                    .GetMaterials(
                        search,
                        categoryId,
                        baseUnit,
                        isActive
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // 取得篩選選項
        // ==========================================
        [HttpGet("filter-options")]
        public IActionResult GetFilterOptions()
        {
            var result =
                _standardMaterialService
                    .GetFilterOptions();

            return ToActionResult(result);
        }


        // ==========================================
        // 修改標準材料
        // ==========================================
        [HttpPost("update")]
        public IActionResult UpdateMaterial(
            [FromForm] int materialId,
            [FromForm] string materialName,
            [FromForm] int categoryId,
            [FromForm] string baseUnit)
        {
            var result =
                _standardMaterialService
                    .UpdateMaterial(
                        materialId,
                        materialName,
                        categoryId,
                        baseUnit
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // 啟用 / 停用標準材料
        // ==========================================
        [HttpPost("set-active")]
        public IActionResult SetMaterialActive(
            [FromForm] int materialId,
            [FromForm] bool isActive)
        {
            var result =
                _standardMaterialService
                    .SetMaterialActive(
                        materialId,
                        isActive
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // ServiceResult → HTTP Response
        // ==========================================
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