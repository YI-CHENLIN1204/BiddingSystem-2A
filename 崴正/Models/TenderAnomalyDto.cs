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

        // 對應到資料庫 material_master 的材料名稱，目前只檢測：磁磚、混凝土、鋼筋
        public string? MatchedMaterialName { get; set; }
        public string? Category { get; set; }
        // 長期 180 天歷史基準，用來讓畫面知道系統是拿歷史資料當比較標準
        public decimal? HistoricalAvgQty180Days { get; set; }
        public decimal? HistoricalStdQty180Days { get; set; }
        public decimal? HistoricalAvgUnitPrice180Days { get; set; }
        public decimal? HistoricalStdUnitPrice180Days { get; set; }

        // 原本欄位保留：現在放「加權後」Z 分數，避免前端原本欄位失效
        public decimal? QtyZScore { get; set; }
        public decimal? UnitPriceZScore { get; set; }

        // 新增：短期 30 天、長期 180 天、加權 Z 分數
        public decimal? ShortTermQtyZScore { get; set; }
        public decimal? LongTermQtyZScore { get; set; }
        public decimal? WeightedQtyZScore { get; set; }

        public decimal? ShortTermUnitPriceZScore { get; set; }
        public decimal? LongTermUnitPriceZScore { get; set; }
        public decimal? WeightedUnitPriceZScore { get; set; }

        public decimal? ExpectedAmount { get; set; }
        public decimal? AmountDifference { get; set; }

        
        public decimal? SuggestedUnitPrice { get; set; }
        public decimal? SuggestedAmount { get; set; }
    }

    public class TenderAnomalyResponseDto
    {
        public int TotalCount { get; set; }
        public int AbnormalCount { get; set; }
        public List<TenderAnomalyResultDto> Results { get; set; } = new();
    }
}

