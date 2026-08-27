using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using 崴正.Data;
using 崴正.Models;
using 崴正.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace 崴正.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstimationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FinalBidService _finalBidService;
        private readonly MaterialMappingService _materialMappingService;
        private readonly FinalBidImportService _finalBidImportService;
        private readonly FinalBidQueryService _finalBidQueryService;
        private readonly AnomalyDetectionService _anomalyDetectionService;

        public EstimationController(
            ApplicationDbContext context,
            FinalBidService finalBidService,
            MaterialMappingService materialMappingService,
            FinalBidImportService finalBidImportService,
            FinalBidQueryService finalBidQueryService,
            AnomalyDetectionService anomalyDetectionService)
        {
            _context = context;
            _finalBidService = finalBidService;
            _materialMappingService = materialMappingService;
            _finalBidImportService = finalBidImportService;
            _finalBidQueryService = finalBidQueryService;
            _anomalyDetectionService = anomalyDetectionService;
        }

        // 保留原本的 JSON 測試 API，避免其他地方若有用到會壞掉。
        [HttpPost("analyze")]
        public IActionResult AnalyzeBid(
            [FromBody] BidRequestDto request)
        {
            var result =
                _anomalyDetectionService
                    .AnalyzeBid(request);

            return Ok(result);
        }

        // 新版：直接上傳 Excel，不儲存本次標單；使用資料庫歷史資料計算時間權重 Z 分數。
        [HttpPost("analyze-excel")]
        public IActionResult AnalyzeExcel(
            IFormFile excelFile)
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

            string extension =
                Path.GetExtension(
                    excelFile.FileName
                ).ToLowerInvariant();

            if (extension != ".xlsx")
            {
                return BadRequest(new
                {
                    message =
                        "目前請先上傳 .xlsx 檔案；.xls 舊格式請先另存新檔為 .xlsx"
                });
            }

            using var stream =
                excelFile.OpenReadStream();

            var result =
                _anomalyDetectionService
                    .AnalyzeExcel(
                        stream,
                        excelFile.FileName
                    );

            return ToActionResult(result);
        }


        // ==========================================
        // UC-04：人工推翻異常判定並寫入 Audit Log
        // ==========================================
        [HttpPost("override")]
        public IActionResult OverrideAnomaly(
            [FromBody] OverrideRequestDto request)
        {
            string actionUser =
                User?.Identity?.Name
                ?? "Estimator_Staff";

            var result =
                _anomalyDetectionService
                    .OverrideAnomaly(
                        request,
                        actionUser
                    );

            return ToActionResult(result);
        }

        
        [HttpPost("upload-final-bid")]
        public IActionResult UploadFinalBid(
            IFormFile excelFile,
            [FromForm] string projectName,
            [FromForm] string projectType,
            [FromForm] string structureType,
            [FromForm] string city,
            [FromForm] decimal? totalFloorArea,
            [FromForm] int? floorCount,
            [FromForm] int? basementFloorCount,
            [FromForm] string projectMonth)
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

                    FloorCount =
                        floorCount,

                    BasementFloorCount =
                        basementFloorCount,

                    ProjectMonth =
                        projectMonth
                };

            var result =
                _finalBidImportService
                    .Import(request);

            return ToActionResult(result);
        }






        [HttpGet("mapping-items")]
        public IActionResult GetMappingItems(
            [FromQuery] int? projectId = null,
            [FromQuery] string status = "Pending",
            [FromQuery] string? search = null,
            [FromQuery] string? issueCode = null,
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
        // 新增標準材料
        // ==========================================
        [HttpPost("create-material")]
        public IActionResult CreateMaterial(
            [FromForm] string materialName,
            [FromForm] string category,
            [FromForm] string baseUnit)
        {
            var result =
                _materialMappingService
                    .CreateMaterial(
                        materialName,
                        category,
                        baseUnit
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

        [HttpPost("update-bid-item-data")]
        public IActionResult UpdateBidItemData(
            [FromForm] long finalBidItemId,
            [FromForm] decimal? qty,
            [FromForm] decimal? unitPrice,
            [FromForm] string? unit)
        {
            var result =
                _finalBidService
                    .UpdateBidItemData(
                        finalBidItemId,
                        qty,
                        unitPrice,
                        unit
                    );

            return ToActionResult(result);
        }

        [HttpPost("ignore-bid-item")]
        public IActionResult IgnoreBidItem(
            [FromForm] long finalBidItemId)
        {
            var result =
                _finalBidService
                    .IgnoreBidItem(
                        finalBidItemId
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
