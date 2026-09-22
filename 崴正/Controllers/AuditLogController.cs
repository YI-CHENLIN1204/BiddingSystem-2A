using Microsoft.AspNetCore.Mvc;
using 崴正.Services;

namespace 崴正.Controllers
{
    public class AuditLogController
        : Controller
    {
        private readonly AuditLogService
            _auditLogService;


        public AuditLogController(
            AuditLogService auditLogService)
        {
            _auditLogService =
                auditLogService;
        }


        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] string? search = null,
            [FromQuery] string? module = null,
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            ViewData["Title"] =
                "操作紀錄";


            var model =
                await _auditLogService
                    .GetPageAsync(
                        search,
                        module,
                        actionType,
                        startDate,
                        endDate,
                        page,
                        pageSize
                    );


            return View(
                model
            );
        }
    }
}
