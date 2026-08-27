namespace 崴正.Models
{
    public class BatchRangeEstimationRequest
    {
        public EstimationTargetRequest Target { get; set; } =
            new();

        public List<int> MaterialIds { get; set; } =
            new();
    }
}