namespace 崴正.Models
{
    public class ProjectManagementDto
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } =
            string.Empty;

        public string? ProjectType { get; set; }

        public string? StructureType { get; set; }

        public string? City { get; set; }

        public decimal? TotalFloorArea { get; set; }

        public int? FloorCount { get; set; }

        public int? BasementFloorCount { get; set; }

        public DateTime? StartDate { get; set; }

        public bool HasFinalBid { get; set; }

        public int FinalBidItemCount { get; set; }

        public int HistoricalTransactionCount { get; set; }
    }


    public class ProjectSaveRequest
    {
        public string ProjectName { get; set; } =
            string.Empty;

        public string ProjectType { get; set; } =
            string.Empty;

        public string StructureType { get; set; } =
            string.Empty;

        public string City { get; set; } =
            string.Empty;

        public decimal? TotalFloorArea { get; set; }

        public int? FloorCount { get; set; }

        public int? BasementFloorCount { get; set; }

        public string ProjectMonth { get; set; } =
            string.Empty;
    }
}