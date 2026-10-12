using System;

namespace 崴正.Models
{
    public class HistoricalTransaction
    {
        public int TransactionId { get; set; }

        public int MaterialId { get; set; }

        public decimal ActualQuantity { get; set; }

        public DateTime TransactionDate { get; set; }

        public bool IsDeleted { get; set; }

        // 原本的文字工程代碼

        // 連接 project_master.project_id
        public int? ProjectMasterId { get; set; }
        public long? FinalBidItemId { get; set; }
        public bool IsOutlier { get; set; }

        // 導覽屬性
        public ProjectMaster? ProjectMaster { get; set; }
    }
}
