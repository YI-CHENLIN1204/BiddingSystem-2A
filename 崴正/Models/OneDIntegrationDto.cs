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
