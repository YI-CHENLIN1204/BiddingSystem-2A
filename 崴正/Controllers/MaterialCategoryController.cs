using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/material-category")]
    public class MaterialCategoryController
        : ControllerBase
    {
        private readonly MaterialCategoryService
            _materialCategoryService;

        public MaterialCategoryController(
            MaterialCategoryService
                materialCategoryService)
        {
            _materialCategoryService =
                materialCategoryService;
        }


        // ==========================================
        // 取得所有材料分類
        // ==========================================
        [HttpGet]
        public IActionResult GetCategories()
        {
            var result =
                _materialCategoryService
                    .GetCategories();

            return ToActionResult(result);
        }


        // ==========================================
        // 新增材料分類
        // ==========================================
        [HttpPost("create")]
        public IActionResult CreateCategory(
            [FromForm] string categoryName)
        {
            var result =
                _materialCategoryService
                    .CreateCategory(
                        categoryName
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // 修改材料分類名稱
        // ==========================================
        [HttpPost("update")]
        public IActionResult UpdateCategory(
            [FromForm] int categoryId,
            [FromForm] string categoryName)
        {
            var result =
                _materialCategoryService
                    .UpdateCategory(
                        categoryId,
                        categoryName
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // 啟用 / 停用材料分類
        // ==========================================
        [HttpPost("set-active")]
        public IActionResult SetCategoryActive(
            [FromForm] int categoryId,
            [FromForm] bool isActive)
        {
            var result =
                _materialCategoryService
                    .SetCategoryActive(
                        categoryId,
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