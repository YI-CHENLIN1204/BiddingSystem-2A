namespace 崴正.Models
{
    public class OneDApiResponse<T>
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }
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

        public string Chapter { get; set; }
            = string.Empty;

        public int? SeqInChapter { get; set; }

        public string? Code { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public string? Unit { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? Confidence { get; set; }
    }


    public class OneDProjectReadResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public OneDSavedResultData? Data { get; set; }
    }
}


namespace 崴正.Models
{
    public class TwoAProjectPreprocessResponse
    {
        public int ProjectId { get; set; }

        public int SourceItemCount { get; set; }

        public int MajorCategoryCount { get; set; }

        public int IncludedItemCount { get; set; }

        public int ExcludedItemCount { get; set; }

        public int PendingItemCount { get; set; }

        public List<TwoAMajorCategorySummaryDto>
            MajorCategories { get; set; }
                = new();

        public List<TwoADetectionGroupDto>
            Groups { get; set; }
                = new();

        public List<TwoAPreprocessItemDto>
            Items { get; set; }
                = new();
    }


    public class TwoAMajorCategorySummaryDto
    {
        public string MajorCategory { get; set; }
            = string.Empty;

        public int ItemCount { get; set; }

        public bool IsEstimable { get; set; }

        public string Reason { get; set; }
            = string.Empty;

        public List<string> DetectionCategories
        {
            get;
            set;
        } = new();
    }


    public class TwoADetectionGroupDto
    {
        public string MajorCategory { get; set; }
            = string.Empty;

        public string DetectionCategory { get; set; }
            = string.Empty;

        public int ItemCount { get; set; }

        public List<string> Units { get; set; }
            = new();

        public string AggregationStatus { get; set; }
            = string.Empty;

        public string? StandardUnit { get; set; }

        public bool NormalizationApplied { get; set; }

        public decimal? TotalQuantity { get; set; }
    }


    public class TwoAPreprocessItemDto
    {
        public string MajorCategory { get; set; }
            = string.Empty;

        public string? DetectionCategory
        {
            get;
            set;
        }

        public string? Code { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public string? Unit { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? NormalizedQuantity { get; set; }

        public string? NormalizedUnit { get; set; }

        public decimal? UnitConversionFactor { get; set; }

        public string UnitNormalizationStatus { get; set; }
            = "NotApplicable";

        public decimal? Confidence { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string Reason { get; set; }
            = string.Empty;
    }
}


namespace 崴正.Models
{
    public class TwoAIntegratedAnalysisPreviewRequest
    {
        public ProjectSimilarityTargetDto Target
        {
            get;
            set;
        } = new();

        public int? TopCount { get; set; }
    }


    public class TwoAIntegratedAnalysisPreviewResponse
    {
        public int OneDProjectId { get; set; }

        public ProjectSimilarityTargetDto Target
        {
            get;
            set;
        } = new();

        public TwoAProjectPreprocessResponse
            Preprocess { get; set; }
                = new();

        public List<ProjectSimilarityDto>
            SimilarProjects { get; set; }
                = new();
    }
}
