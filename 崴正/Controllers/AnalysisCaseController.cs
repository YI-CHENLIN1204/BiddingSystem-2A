using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/analysis-cases")]
    public class AnalysisCaseController
        : ControllerBase
    {
        private readonly AnalysisCaseService
            _analysisCaseService;


        public AnalysisCaseController(
            AnalysisCaseService analysisCaseService)
        {
            _analysisCaseService =
                analysisCaseService;
        }


        [HttpGet("policy")]
        public IActionResult GetCurrentPolicy()
        {
            return Ok(new
            {
                provisional = true,

                targetSources = new[]
                {
                    "Upload",
                    "OneD"
                },

                sourcePolicy = new
                {
                    standaloneFirst = true,
                    oneDOptional = true,
                    rule =
                        "2A 必須可在沒有 1D 的環境獨立運作；若有設定 1D API，才額外開放 1D 匯入入口。"
                },

                oneDUsage =
                    "1D 僅作為待偵錯案件的初始資料來源；匯入後建立 2A 快照，後續偵錯不需重新呼叫 1D。",

                selfUpload = new
                {
                    enabled = true,
                    mode = "StructuredOnly",
                    requirement =
                        "直接上傳暫定只接受已具 1D 同層級工程大項分類的結構化標單，不接受尚未分類的原始文件。",
                    requiredForQuantity = new[]
                    {
                        "工程大項",
                        "工項名稱或規格",
                        "單位",
                        "數量"
                    },
                    priceFields =
                        "大項價格異常另需單價或金額；沒有價格時仍可建立案件，但不能執行價格異常。"
                },

                hierarchy = new
                {
                    majorCategory =
                        "目前大項估算單位暫定為「結構體工程」。",
                    detectionCategory =
                        "目前細項數量偵錯單位暫定為「鋼筋、混凝土」。"
                },

                quantityEstimation = new
                {
                    estimableMajorCategories =
                        TwoAScopePolicy
                            .GetQuantityEstimableMajorCategories(),
                    detectionCategories =
                        TwoAScopePolicy
                            .GetQuantityDetectionCategories(),
                    rule =
                        "目前只在結構體工程下，將鋼筋與混凝土作為數量偵錯細項；其他工項保留原始資料但暫不進入本階段數量判定。"
                },

                priceEstimation = new
                {
                    majorCategories =
                        TwoAScopePolicy
                            .GetPriceEstimationMajorCategories(),
                    rule =
                        "目前大項價格合理範圍與每坪單價估算先以結構體工程為單位。"
                },

                knownMajorCategories =
                    TwoAScopePolicy
                        .GetKnownMajorCategories(),

                historyRule =
                    "AnalysisCase / AnalysisBidVersion 永遠不直接寫入 HistoricalTransaction。只有正式已決標最終標單才進入歷史基準資料。"
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetCases()
        {
            return ToActionResult(
                await _analysisCaseService
                    .GetCasesAsync()
            );
        }


        [HttpGet("{analysisCaseId:int}")]
        public async Task<IActionResult> GetCase(
            int analysisCaseId)
        {
            return ToActionResult(
                await _analysisCaseService
                    .GetCaseAsync(
                        analysisCaseId
                    )
            );
        }


        [HttpGet(
            "{analysisCaseId:int}/versions/{versionId:long}")]
        public async Task<IActionResult> GetVersion(
            int analysisCaseId,
            long versionId)
        {
            return ToActionResult(
                await _analysisCaseService
                    .GetVersionAsync(
                        analysisCaseId,
                        versionId
                    )
            );
        }


        [HttpGet("{analysisCaseId:int}/runs")]
        public async Task<IActionResult> GetRuns(
            int analysisCaseId)
        {
            return ToActionResult(
                await _analysisCaseService
                    .GetRunsAsync(
                        analysisCaseId
                    )
            );
        }


        [HttpGet(
            "{analysisCaseId:int}/runs/{analysisRunId:long}")]
        public async Task<IActionResult> GetRun(
            int analysisCaseId,
            long analysisRunId)
        {
            return ToActionResult(
                await _analysisCaseService
                    .GetRunAsync(
                        analysisCaseId,
                        analysisRunId
                    )
            );
        }


        [HttpPost("from-1d/{oneDProjectId:int}")]
        public async Task<IActionResult> CreateFromOneD(
            int oneDProjectId,
            [FromBody]
            AnalysisCaseMetadataRequest metadata,
            [FromQuery]
            string? note,
            CancellationToken cancellationToken)
        {
            return ToActionResult(
                await _analysisCaseService
                    .CreateFromOneDAsync(
                        oneDProjectId,
                        metadata,
                        note,
                        GetActionUser(),
                        cancellationToken
                    )
            );
        }


        [HttpPost("from-upload")]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> CreateFromUpload(
            IFormFile excelFile,
            [FromForm] string caseName,
            [FromForm] string projectType,
            [FromForm] string structureType,
            [FromForm] string city,
            [FromForm] decimal totalFloorArea,
            [FromForm] decimal siteArea,
            [FromForm] int floorCount,
            [FromForm] int basementFloorCount,
            [FromForm] DateTime projectDate,
            [FromForm] string? note,
            [FromForm] string? includedSheetNames,
            CancellationToken cancellationToken)
        {
            if (excelFile == null ||
                excelFile.Length == 0)
            {
                return BadRequest(new
                {
                    message =
                        "請先選擇待偵錯標單"
                });
            }

            IReadOnlyCollection<string>? selectedSheets = null;
            if (includedSheetNames != null)
            {
                try { selectedSheets = JsonSerializer.Deserialize<List<string>>(includedSheetNames); }
                catch (JsonException) { return BadRequest(new { message = "工作表選擇格式錯誤" }); }
            }

            using var buffer =
                new MemoryStream();

            await excelFile.CopyToAsync(
                buffer,
                cancellationToken
            );

            var metadata =
                new AnalysisCaseMetadataRequest
                {
                    CaseName = caseName,
                    ProjectType = projectType,
                    StructureType = structureType,
                    City = city,
                    TotalFloorArea =
                        totalFloorArea,
                    SiteArea =
                        siteArea,
                    FloorCount = floorCount,
                    BasementFloorCount =
                        basementFloorCount,
                    ProjectDate = projectDate
                };

            return ToActionResult(
                await _analysisCaseService
                    .CreateFromUploadAsync(
                        metadata,
                        buffer.ToArray(),
                        excelFile.FileName,
                        note,
                        GetActionUser(),
                        cancellationToken,
                        selectedSheets
                    )
            );
        }


        [HttpPost(
            "{analysisCaseId:int}/versions/from-1d/{oneDProjectId:int}")]
        public async Task<IActionResult>
            AddVersionFromOneD(
                int analysisCaseId,
                int oneDProjectId,
                [FromBody]
                AnalysisCaseVersionNoteRequest?
                    request,
                CancellationToken cancellationToken)
        {
            return ToActionResult(
                await _analysisCaseService
                    .AddVersionFromOneDAsync(
                        analysisCaseId,
                        oneDProjectId,
                        request?.Note,
                        GetActionUser(),
                        cancellationToken
                    )
            );
        }


        [HttpPost(
            "{analysisCaseId:int}/versions/upload")]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult>
            AddVersionFromUpload(
                int analysisCaseId,
                IFormFile excelFile,
                [FromForm] string? note,
                [FromForm] string? includedSheetNames,
                CancellationToken cancellationToken)
        {
            if (excelFile == null ||
                excelFile.Length == 0)
            {
                return BadRequest(new
                {
                    message =
                        "請先選擇待偵錯標單"
                });
            }

            IReadOnlyCollection<string>? selectedSheets = null;
            if (includedSheetNames != null)
            {
                try { selectedSheets = JsonSerializer.Deserialize<List<string>>(includedSheetNames); }
                catch (JsonException) { return BadRequest(new { message = "工作表選擇格式錯誤" }); }
            }

            using var buffer =
                new MemoryStream();

            await excelFile.CopyToAsync(
                buffer,
                cancellationToken
            );

            return ToActionResult(
                await _analysisCaseService
                    .AddVersionFromUploadAsync(
                        analysisCaseId,
                        buffer.ToArray(),
                        excelFile.FileName,
                        note,
                        GetActionUser(),
                        cancellationToken,
                        selectedSheets
                    )
            );
        }


        [HttpPost(
            "{analysisCaseId:int}/versions/{versionId:long}/quantity-analysis")]
        public async Task<IActionResult>
            RunQuantityAnalysis(
                int analysisCaseId,
                long versionId,
                [FromBody]
                AnalysisCaseQuantityRunRequest?
                    request)
        {
            return ToActionResult(
                await _analysisCaseService
                    .RunQuantityAnalysisAsync(
                        analysisCaseId,
                        versionId,
                        request?.TopCount
                    )
            );
        }


        [HttpPost(
            "{analysisCaseId:int}/versions/{versionId:long}/major-price-analysis")]
        public async Task<IActionResult>
            RunMajorPriceAnalysis(
                int analysisCaseId,
                long versionId,
                [FromBody]
                AnalysisCasePriceRunRequest
                    request)
        {
            return ToActionResult(
                await _analysisCaseService
                    .RunMajorPriceAnalysisAsync(
                        analysisCaseId,
                        versionId,
                        request.MajorCategory,
                        request.TopCount
                    )
            );
        }


        private string GetActionUser()
        {
            return User?.Identity?.Name
                ?? "System";
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
