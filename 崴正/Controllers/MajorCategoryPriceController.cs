using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/major-category-price")]
    public class MajorCategoryPriceController
        : ControllerBase
    {
        private readonly MajorCategoryPriceRangeService
            _majorCategoryPriceRangeService;


        public MajorCategoryPriceController(
            MajorCategoryPriceRangeService
                majorCategoryPriceRangeService)
        {
            _majorCategoryPriceRangeService =
                majorCategoryPriceRangeService;
        }


        [HttpPost("range")]
        public async Task<IActionResult> EstimateRange(
            [FromBody]
            MajorCategoryPriceRangeRequest request)
        {
            var result =
                await _majorCategoryPriceRangeService
                    .EstimateAsync(request);

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
