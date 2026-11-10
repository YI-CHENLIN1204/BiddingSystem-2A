namespace 崴正.Services
{
    // Read-only diagnostics. These numbers are not calibrated confidence probabilities.
    public static class ReferenceWeightDiagnostics
    {
        public static (decimal EffectiveSampleSize, decimal LargestWeightShare)
            Calculate(IEnumerable<decimal> weights)
        {
            var positive = weights.Where(w => w > 0m).ToArray();
            if (positive.Length == 0)
                return (0m, 0m);

            decimal total = positive.Sum();
            decimal squared = positive.Sum(w => w * w);
            if (total <= 0m || squared <= 0m)
                return (0m, 0m);

            decimal ess = total * total / squared;
            decimal largestShare = positive.Max() / total;
            return (Math.Round(ess, 2), Math.Round(largestShare * 100m, 2));
        }
    }
}
