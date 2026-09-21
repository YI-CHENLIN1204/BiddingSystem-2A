using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/unit-category")]
    public class UnitCategoryController
        : ControllerBase
    {
        private readonly UnitCategoryService
            _unitCategoryService;


        public UnitCategoryController(
            UnitCategoryService unitCategoryService)
        {
            _unitCategoryService =
                unitCategoryService;
        }


        // ==========================================
        // 取得可使用的單位類別
        // ==========================================
        [HttpGet]
        public IActionResult GetCategories()
        {
            var result =
                _unitCategoryService
                    .GetCategories();

            return ToActionResult(
                result
            );
        }


        // ==========================================
        // 新增單位類別
        //
        // allowConversion = true
        // → baseUnitName 必填
        //
        // allowConversion = false
        // → baseUnitName 可不傳
        // ==========================================
        [HttpPost("create")]
        public IActionResult CreateCategory(
            [FromForm] string categoryName,
            [FromForm] bool allowConversion,
            [FromForm] string? baseUnitName)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "System";

            var result =
                _unitCategoryService
                    .CreateCategory(
                        categoryName,
                        allowConversion,
                        baseUnitName,
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
