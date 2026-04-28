namespace 崴正.Models
{
    public class TenderImportRowDto
    {
        public string SourceFileName { get; set; } = "";
        public string SheetName { get; set; } = "";
        public int RowNumber { get; set; }

        public string ItemCode { get; set; } = "";
        public string Category1 { get; set; } = "";
        public string Category2 { get; set; } = "";
        public string Category3 { get; set; } = "";

        public string ItemName { get; set; } = "";
        public string Spec { get; set; } = "";
        public string Unit { get; set; } = "";

        public decimal? Qty { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }

        public string Note { get; set; } = "";
    }

    public class TenderAnomalyResultDto : TenderImportRowDto
    {
        public bool IsAbnormal { get; set; }
        public string StatusMessage { get; set; } = "正常";
        public decimal? QtyZScore { get; set; }
        public decimal? UnitPriceZScore { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? AmountDifference { get; set; }
    }

    public class TenderAnomalyResponseDto
    {
        public int TotalCount { get; set; }
        public int AbnormalCount { get; set; }
        public List<TenderAnomalyResultDto> Results { get; set; } = new();
    }
}
