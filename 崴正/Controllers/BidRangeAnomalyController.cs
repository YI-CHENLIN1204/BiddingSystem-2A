using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/bid-anomaly")]
    public class BidRangeAnomalyController : ControllerBase
    {
        private readonly BidRangeAnomalyAnalysisService
            _analysisService;


        public BidRangeAnomalyController(
            BidRangeAnomalyAnalysisService analysisService)
        {
            _analysisService =
                analysisService;
        }


        // ==========================================
        // 新版標單異常偵測
        //
        // POST /api/bid-anomaly/analyze
        //
        // FormData:
        // excelFile
        // projectType
        // structureType
        // city
        // totalFloorArea
        // floorCount
        // basementFloorCount
        // projectMonth
        // ==========================================
        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(
            IFormFile excelFile,

            [FromForm]
            string projectType,

            [FromForm]
            string structureType,

            [FromForm]
            string city,

            [FromForm]
            decimal? totalFloorArea,

            [FromForm]
            int? floorCount,

            [FromForm]
            int? basementFloorCount,

            [FromForm]
            string projectMonth)
        {
            // ======================================
            // 1. Excel 基本驗證
            // ======================================

            if (
                excelFile == null ||
                excelFile.Length == 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "請先選擇待分析的 Excel 標單"
                });
            }


            string extension =
                Path.GetExtension(
                    excelFile.FileName
                )
                .ToLowerInvariant();


            string[] allowedExtensions =
            {
                ".xlsx",
                ".xls",
                ".xlsm",
                ".xlsb",
                ".csv"
            };

            if (!allowedExtensions.Contains(
                    extension))
            {
                return BadRequest(new
                {
                    message =
                        "支援格式：.xlsx、.xls、.xlsm、.xlsb、.csv"
                });
            }


            // ======================================
            // 2. 建案條件基本驗證
            //
            // 更完整的驗證仍會由
            // EstimationTargetService 負責。
            // ======================================

            if (
                string.IsNullOrWhiteSpace(
                    projectType
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "請選擇建案類型"
                });
            }


            if (
                string.IsNullOrWhiteSpace(
                    structureType
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "請選擇結構類型"
                });
            }


            if (
                string.IsNullOrWhiteSpace(
                    city
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "請選擇縣市"
                });
            }


            if (
                !totalFloorArea.HasValue ||
                totalFloorArea.Value <= 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "請輸入正確的總樓地板面積"
                });
            }


            if (
                !floorCount.HasValue ||
                floorCount.Value <= 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "請輸入正確的地上樓層數"
                });
            }


            if (
                !basementFloorCount.HasValue ||
                basementFloorCount.Value < 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "請輸入正確的地下樓層數"
                });
            }


            if (
                string.IsNullOrWhiteSpace(
                    projectMonth
                )
            )
            {
                return BadRequest(new
                {
                    message =
                        "請選擇建案年月"
                });
            }


            // ======================================
            // 3. Excel 讀進記憶體
            //
            // 只供本次分析使用，
            // 不儲存 FinalBidItem。
            // ======================================

            using var buffer =
                new MemoryStream();




            await excelFile.CopyToAsync(
                buffer
            );


            // ======================================
            // 4. 建立 Manual Target
            // ======================================

            var target =
                new EstimationTargetRequest
                {
                    TargetMode =
                        EstimationTargetMode
                            .ManualInput,

                    ProjectType =
                        projectType,

                    StructureType =
                        structureType,

                    City =
                        city,

                    TotalFloorArea =
                        totalFloorArea.Value,

                    FloorCount =
                        floorCount.Value,

                    BasementFloorCount =
                        basementFloorCount.Value,

                    ProjectMonth =
                        projectMonth
                };


            // ======================================
            // 5. 建立新版異常分析 Request
            // ======================================

            var request =
                new BidRangeAnomalyAnalysisRequest
                {
                    Target =
                        target,

                    ExcelBytes =
                        buffer.ToArray(),

                    FileName =
                        excelFile.FileName
                };


            // ======================================
            // 6. 執行新版異常分析
            // ======================================


            var result =
                await _analysisService
                    .AnalyzeAsync(
                        request
                    );


            // ======================================
            // 7. Response
            // ======================================

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