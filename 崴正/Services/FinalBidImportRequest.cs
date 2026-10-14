namespace 崴正.Services
{
    public class FinalBidImportRequest
    {
        public byte[] ExcelBytes { get; set; } =
            Array.Empty<byte>();

        public IReadOnlyCollection<string>? IncludedSheetNames { get; set; }

        public string FileName { get; set; } =
            string.Empty;

        public string ProjectName { get; set; } =
            string.Empty;

        public string ProjectType { get; set; } =
            string.Empty;

        public string StructureType { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public decimal? TotalFloorArea { get; set; }

        public decimal? SiteArea { get; set; }

        public int? FloorCount { get; set; }

        public int? BasementFloorCount { get; set; }

        public string ProjectMonth { get; set; } =
            string.Empty;
    }
}