using Microsoft.AspNetCore.Mvc;
using 崴正.Models;
using 崴正.Services;

namespace 崴正.Controllers
{
    public class ProjectManagementController : Controller
    {
        private readonly ProjectManagementService
            _projectManagementService;

        public ProjectManagementController(
            ProjectManagementService projectManagementService)
        {
            _projectManagementService =
                projectManagementService;
        }


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Detail(
            [FromQuery] int projectId)
        {
            ViewBag.ProjectId =
                projectId;

            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Projects(
            [FromQuery] string? search = null)
        {
            var result =
                await _projectManagementService
                    .GetProjectsAsync(search);

            return Ok(new
            {
                items = result
            });
        }


        [HttpGet]
        public async Task<IActionResult> Project(
            [FromQuery] int projectId)
        {
            var result =
                await _projectManagementService
                    .GetProjectAsync(projectId);

            return ToActionResult(result);
        }


        [HttpPost]
        public async Task<IActionResult> Update(
            [FromQuery] int projectId,
            [FromBody] ProjectSaveRequest request)
        {
            var result =
                await _projectManagementService
                    .UpdateProjectAsync(
                        projectId,
                        request
                    );

            return ToActionResult(result);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(
            [FromQuery] int projectId)
        {
            var result =
                await _projectManagementService
                    .DeleteProjectAsync(
                        projectId
                    );

            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> FinalBidItems(
            [FromQuery] int projectId,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result =
                await _projectManagementService
                    .GetFinalBidItemsAsync(
                        projectId,
                        search,
                        page,
                        pageSize
                    );

            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> HistoricalData(
            [FromQuery] int projectId)
        {
            var result =
                await _projectManagementService
                    .GetHistoricalDataAsync(
                        projectId
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