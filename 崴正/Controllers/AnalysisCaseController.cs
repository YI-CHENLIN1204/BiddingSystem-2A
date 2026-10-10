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
