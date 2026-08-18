using System;
using System.Collections.Generic;

namespace 崴正.Models
{
    public class ProjectMaster
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public decimal? TotalFloorArea { get; set; }
        public string? FloorRange { get; set; }
        public int? FloorCount { get; set; }
        
        public int? BasementFloorCount { get; set; }
        public DateTime? StartDate { get; set; }

        public bool IsDeleted { get; set; }

        public string? ProjectType { get; set; }

        public string? StructureType { get; set; }

        public string? City { get; set; }
        public string? SourceFileName { get; set; }

        public string? SourceFileHash { get; set; }

        public DateTime? ImportedAt { get; set; }
        // 一個建案可包含多筆歷史成交紀錄
        public ICollection<HistoricalTransaction> HistoricalTransactions { get; set; }
            = new List<HistoricalTransaction>();
    }
}