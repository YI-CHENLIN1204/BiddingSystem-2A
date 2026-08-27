namespace 崴正.Models
{
    public class ResolvedEstimationTarget
    {
        public EstimationTargetMode SourceMode { get; set; }

        public int? HistoricalProjectId { get; set; }

        public string DisplayName { get; set; } =
            string.Empty;

        public ProjectSimilarityTargetDto SimilarityTarget
        {
            get;
            set;
        } = new();
    }
}