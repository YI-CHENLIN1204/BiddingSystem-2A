namespace 崴正.Models
{
    // ==========================================
    // 新版標單異常分析
    //
    // Target 建案條件
    // +
    // Excel 標單
    //
    // ExcelBytes 不直接由 Controller 綁定，
    // Controller 收 IFormFile 後再轉成 byte[]
    // ==========================================
    public class BidRangeAnomalyAnalysisRequest
    {
        public EstimationTargetRequest Target { get; set; }
            = new();

        public byte[] ExcelBytes { get; set; }
            = Array.Empty<byte>();

        public string FileName { get; set; }
            = string.Empty;
    }


    // ==========================================
    // 每一個「標準材料」的異常分析結果
    // ==========================================
    public class BidRangeAnomalyMaterialResultDto
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }
            = string.Empty;

        public string BaseUnit { get; set; }
            = string.Empty;


        // --------------------------------------
        // 目標標單中的實際數量
        //
        // 同一 MaterialId 的多個工項
        // 最後會彙總成一筆
        // --------------------------------------
        public decimal ActualQuantity { get; set; }

        public int SourceRowCount { get; set; }


        // --------------------------------------
        // 合理區間模型資訊
        // --------------------------------------
        public int ReferenceProjectCount { get; set; }

        public int MinimumSampleCount { get; set; }

        public bool HasEnoughSamples { get; set; }


        // --------------------------------------
        // P15 / P50 / P85
        // --------------------------------------
        public decimal? LowerQuantity { get; set; }

        public decimal? MedianQuantity { get; set; }

        public decimal? UpperQuantity { get; set; }


        // --------------------------------------
        // Low
        // Normal
        // High
        // InsufficientSamples
        // --------------------------------------
        public string AnomalyStatus { get; set; }
            = string.Empty;

        public string StatusMessage { get; set; }
            = string.Empty;
    }


    // ==========================================
    // 無法進入異常分析的原始工項
    // ==========================================
    public class BidRangeAnomalyUnmappedItemDto
    {
        public string? SheetName { get; set; }

        public int? RowNumber { get; set; }

        public string ItemName { get; set; }
            = string.Empty;

        public string? Spec { get; set; }

        public string? Unit { get; set; }

        public decimal? Quantity { get; set; }

        public string Reason { get; set; }
            = string.Empty;
    }


    // ==========================================
    // 整份標單分析結果
    // ==========================================
    public class BidRangeAnomalyAnalysisResponseDto
    {
        public string FileName { get; set; }
            = string.Empty;


        // --------------------------------------
        // Excel / Mapping 摘要
        // --------------------------------------
        public int ParsedRowCount { get; set; }

        public int MappedRowCount { get; set; }

        public int UnmappedRowCount { get; set; }

        public int InvalidRowCount { get; set; }

        public int MaterialCount { get; set; }


        // --------------------------------------
        // 模型摘要
        // --------------------------------------
        public int CandidateProjectCount { get; set; }

        public int NormalCount { get; set; }

        public int HighCount { get; set; }

        public int LowCount { get; set; }

        public int InsufficientSampleCount { get; set; }


        public List<BidRangeAnomalyMaterialResultDto>
            Results { get; set; }
                = new();


        public List<BidRangeAnomalyUnmappedItemDto>
            UnmappedItems { get; set; }
                = new();
    }
}