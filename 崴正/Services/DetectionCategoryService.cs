using Microsoft.EntityFrameworkCore;
using 崴正.Data;
using 崴正.Models;

namespace 崴正.Services
{
    public class DetectionCategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly UnitConversionService _unitConversionService;
        private readonly AuditLogService _auditLogService;

        public DetectionCategoryService(
            ApplicationDbContext context,
            UnitConversionService unitConversionService,
            AuditLogService auditLogService)
        {
            _context = context;
            _unitConversionService = unitConversionService;
            _auditLogService = auditLogService;
        }
    }
}
