using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using 崴正.Services;

namespace 崴正.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstimationController : ControllerBase
    {
        private readonly FinalBidService _finalBidService;
        private readonly MaterialMappingService _materialMappingService;
        private readonly FinalBidImportService _finalBidImportService;
        private readonly FinalBidQueryService _finalBidQueryService;

        public EstimationController(
            FinalBidService finalBidService,
            MaterialMappingService materialMappingService,
            FinalBidImportService finalBidImportService,
            FinalBidQueryService finalBidQueryService)
        {
            _finalBidService = finalBidService;
            _materialMappingService = materialMappingService;
            _finalBidImportService = finalBidImportService;
            _finalBidQueryService = finalBidQueryService;
        }

        
        [HttpPost("upload-final-bid")]
        public IActionResult UploadFinalBid(
            IFormFile excelFile,
            [FromForm] string projectName,
            [FromForm] string projectType,
            [FromForm] string structureType,
            [FromForm] string city,
            [FromForm] decimal? totalFloorArea,
            [FromForm] decimal? siteArea,
            [FromForm] int? floorCount,
            [FromForm] int? basementFloorCount,
            [FromForm] string projectMonth,
            [FromForm] string? includedSheetNames = null)
        {
            if (excelFile == null ||
                excelFile.Length == 0)
            {
                return BadRequest(new
                {
                    message =
                        "請先選擇 Excel 檔案"
                });
            }

            IReadOnlyCollection<string>? selectedSheets = null;
            if (includedSheetNames != null)
            {
                try
                {
                    selectedSheets = JsonSerializer.Deserialize<List<string>>(includedSheetNames);
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "工作表選擇格式錯誤" });
                }
            }

            using var buffer =
                new MemoryStream();

            excelFile.CopyTo(
                buffer
            );

            var request =
                new FinalBidImportRequest
                {
                    ExcelBytes =
                        buffer.ToArray(),

                    FileName =
                        excelFile.FileName,

                    IncludedSheetNames =
                        selectedSheets,

                    ProjectName =
                        projectName,

                    ProjectType =
                        projectType,

                    StructureType =
                        structureType,

                    City =
                        city,

                    TotalFloorArea =
                        totalFloorArea,

                    SiteArea =
                        siteArea,

                    FloorCount =
                        floorCount,

                    BasementFloorCount =
                        basementFloorCount,

                    ProjectMonth =
                        projectMonth
                };

            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _finalBidImportService
                    .Import(
                        request,
                        actionUser
                    );

            return ToActionResult(result);
        }






        [HttpGet("mapping-items")]
        public IActionResult GetMappingItems(
            [FromQuery] int? projectId = null,
            [FromQuery] string status = "Pending",
            [FromQuery] string? search = null,
            [FromQuery] string? issueCode = null,
            [FromQuery] string? majorCategory = null,
            [FromQuery] string? category = null,
            [FromQuery] string? unit = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result =
                _finalBidQueryService
                    .GetMappingItems(
                        projectId,
                        status,
                        search,
                        issueCode,
                        majorCategory,
                        category,
                        unit,
                        page,
                        pageSize
                    );

            return ToActionResult(result);
        }

        // ==========================================
        // 建案材料對應統計摘要
        // 直接依 final_bid_item 即時計算
        // ==========================================
        [HttpGet("mapping-summary")]
        public IActionResult GetMappingSummary(
            [FromQuery] int? projectId = null)
        {
            var result =
                _finalBidQueryService
                    .GetMappingSummary(
                        projectId
                    );

            return ToActionResult(result);
        }

        [HttpGet("mapping-filter-options")]
        public IActionResult GetMappingFilterOptions(
            [FromQuery] int? projectId = null)
        {
            var result =
                _finalBidQueryService
                    .GetMappingFilterOptions(
                        projectId
                    );

            return ToActionResult(result);
        }
        
        // ==========================================
        // 取得標準材料清單
        // 供人工對應下拉選單使用
        // ==========================================
        [HttpGet("material-options")]
        public IActionResult GetMaterialOptions(
            [FromQuery] string? search = null)
        {
            var result =
                _finalBidQueryService
                    .GetMaterialOptions(
                        search
                    );

            return ToActionResult(result);
        }

        // ==========================================
        // 人工指定 / 修改材料對應
        //
        // 支援：
        // Unmapped → Manual
        // Auto     → Manual
        // Manual   → Manual
        // ==========================================
        [HttpPost("update-material-mapping")]
        public IActionResult UpdateMaterialMapping(
            [FromForm] long finalBidItemId,
            [FromForm] int materialId)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _materialMappingService
                    .UpdateMaterialMapping(
                        finalBidItemId,
                        materialId,
                        actionUser
                    );

            return ToActionResult(result);
        }

        [HttpPost("update-major-category")]
        public IActionResult UpdateMajorCategory(
            [FromForm] long finalBidItemId,
            [FromForm] string? majorCategory)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _finalBidService
                    .UpdateMajorCategoryOverride(
                        finalBidItemId,
                        majorCategory,
                        actionUser
                    );

            return ToActionResult(result);
        }


        [HttpPost("update-bid-item-data")]
        public IActionResult UpdateBidItemData(
            [FromForm] long finalBidItemId,
            [FromForm] string? itemName,
            [FromForm] decimal? qty,
            [FromForm] decimal? unitPrice,
            [FromForm] string? unit)
        {

            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _finalBidService
                    .UpdateBidItemData(
                        finalBidItemId,
                        itemName,
                        qty,
                        unitPrice,
                        unit,
                        actionUser
                    );

            return ToActionResult(result);
        }

        [HttpPost("ignore-bid-item")]
        public IActionResult IgnoreBidItem(
            [FromForm] long finalBidItemId)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _finalBidService
                    .IgnoreBidItem(
                        finalBidItemId,
                        actionUser
                    );

            return ToActionResult(result);
        }

        [HttpPost("unignore-bid-item")]
        public IActionResult UnignoreBidItem(
            [FromForm] long finalBidItemId)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _finalBidService
                    .UnignoreBidItem(
                        finalBidItemId,
                        actionUser
                    );
                    
            return ToActionResult(result);
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
