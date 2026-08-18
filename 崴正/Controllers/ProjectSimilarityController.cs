using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    [ApiController]
    [Route("api/project-similarity")]
    public class ProjectSimilarityController : ControllerBase
    {
        private readonly ProjectSimilarityService _similarityService;

        public ProjectSimilarityController(
            ProjectSimilarityService similarityService)
        {
            _similarityService = similarityService;
        }

        [HttpGet("{projectId:int}")]
        public async Task<IActionResult> GetSimilarProjects(
            int projectId,
            [FromQuery] int top = 5)
        {
            if (top <= 0)
            {
                top = 5;
            }

            // 防止一次要求太多結果
            if (top > 20)
            {
                top = 20;
            }

            var results =
                await _similarityService.FindSimilarProjectsAsync(
                    projectId,
                    top);

            return Ok(results);
        }
    }
}