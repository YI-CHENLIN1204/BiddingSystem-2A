namespace 崴正.Models
{
    public class OneDApiResponse<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public T? Data { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public class OneDSavedResultData
    {
        public int ProjectId { get; set; }

        public int Count { get; set; }

        public List<OneDSavedResultItem> Items { get; set; }
            = new();
    }

    public class OneDSavedResultItem
    {
        public string? Category { get; set; }

        public string? Chapter { get; set; }

        public int? SeqInChapter { get; set; }

        public string? Code { get; set; }

        public string? Name { get; set; }

        public string? Unit { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? Confidence { get; set; }
    }

    public class DetectionCategoryPreviewRequest
    {
        public List<OneDSavedResultItem> Items { get; set; }
            = new();
    }

    public class DetectionCategoryPreviewResponse
    {
        public int TotalItems { get; set; }

        public int MajorCategoryCount { get; set; }

        public int ClassifiedItems { get; set; }

        public int PendingItems { get; set; }

        public List<MajorCategorySummaryDto> MajorCategories { get; set; }
            = new();

        public List<DetectionCategoryGroupDto> Groups { get; set; }
            = new();

        public List<DetectionCategoryPreviewItem> Items { get; set; }
            = new();
    }

    public class MajorCategorySummaryDto
    {
        public string MajorCategory { get; set; }
            = string.Empty;

        public int ItemCount { get; set; }

        public int ClassifiedItemCount { get; set; }

        public int PendingItemCount { get; set; }

        public bool HasDetectionRules { get; set; }

        public List<string> DetectionCategories { get; set; }
            = new();
    }

    public class DetectionCategoryGroupDto
    {
        public string MajorCategory { get; set; }
            = string.Empty;

        public string DetectionCategory { get; set; }
            = string.Empty;

        public int ItemCount { get; set; }

        public List<string> Units { get; set; }
            = new();

        public decimal? TotalQuantity { get; set; }

        public string? AggregateUnit { get; set; }

        public string AggregationStatus { get; set; }
            = string.Empty;

        public string AggregationMessage { get; set; }
            = string.Empty;
    }

    public class DetectionCategoryPreviewItem
    {
        public string? SourceCategory { get; set; }

        public string MajorCategory { get; set; }
            = string.Empty;

        public string? DetectionCategory { get; set; }

        public string OriginalItemName { get; set; }
            = string.Empty;

        public string? ItemCode { get; set; }

        public string? Unit { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? OneDConfidence { get; set; }

        public string ClassificationStatus { get; set; }
            = string.Empty;

        public string MatchedRule { get; set; }
            = string.Empty;
    }
}
