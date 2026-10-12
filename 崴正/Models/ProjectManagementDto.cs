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

        public decimal? SiteArea { get; set; }

        public int? FloorCount { get; set; }

        public int? BasementFloorCount { get; set; }

        public DateTime? StartDate { get; set; }

        public bool HasFinalBid { get; set; }

        public int FinalBidItemCount { get; set; }

        public int HistoricalTransactionCount { get; set; }
    }


    public class ProjectTwoASummaryDto
    {
        public int ProjectId { get; set; }

        public string MajorCategory { get; set; } =
            "結構體工程";

        public int StructuralItemCount { get; set; }

        public int StructuralPricedItemCount { get; set; }

        public decimal StructuralAmount { get; set; }

        public decimal? StructuralPricePerPing { get; set; }

        public decimal? RebarQuantity { get; set; }

        public string RebarUnit { get; set; } =
            "kg";

        public int RebarTransactionCount { get; set; }

        public bool ContributesRebarHistory { get; set; }

        public decimal? ConcreteQuantity { get; set; }

        public string ConcreteUnit { get; set; } =
            "立方公尺";

        public int ConcreteTransactionCount { get; set; }

        public bool ContributesConcreteHistory { get; set; }

        public int OtherStructuralItemCount { get; set; }

        public string Message { get; set; } =
            string.Empty;
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

        public decimal? SiteArea { get; set; }

        public int? FloorCount { get; set; }

        public int? BasementFloorCount { get; set; }

        public string ProjectMonth { get; set; } =
            string.Empty;
    }
}