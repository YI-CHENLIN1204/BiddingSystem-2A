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
                        "1D 工程章節，例如：結構體工程、鋼骨鋼構工程。",
                    detectionCategory =
                        "2A 數量偵錯細項，例如：鋼筋、混凝土、模板、鋼骨。"
                },

                quantityEstimation = new
                {
                    estimableMajorCategories =
                        TwoAScopePolicy
                            .GetEstimableMajorCategories(),
                    rule =
                        "僅對目前可由歷史建案建立合理材料用量基準的大項進行細項數量異常；其他大項保留資料但不強制估算。"
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
            [FromForm] int floorCount,
            [FromForm] int basementFloorCount,
            [FromForm] DateTime projectDate,
            [FromForm] string? note,
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
                        cancellationToken
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
                        cancellationToken
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
