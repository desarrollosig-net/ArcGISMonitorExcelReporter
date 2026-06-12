namespace ArcGISMonitorExcelReporterLib.Reporting
{
    /// <summary>
    /// Provides statistical calculation methods for metric data analysis.
    /// </summary>
    public static class StatisticsCalculator
    {
        /// <summary>
        /// Exact z-score for the 95th percentile in a normal distribution.
        /// </summary>
        /// <remarks>
        /// This is the value z such that P(Z ≤ z) = 0.95 in a standard normal distribution.
        /// Calculated using the inverse cumulative distribution function (probit) for 95%.
        /// </remarks>
        private const double ZScore95 = 1.6448536269514722;

        /// <summary>
        /// Calculates the 95th percentile value using the mean and standard deviation,
        /// constrained by the maximum observed value.
        /// </summary>
        /// <param name="avgValue">The average (mean) value.</param>
        /// <param name="stdDevValue">The standard deviation.</param>
        /// <param name="maxValue">The maximum observed value (optional constraint).</param>
        /// <returns>
        /// The calculated 95th percentile, or null if calculation is not possible.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method assumes a normal distribution and calculates the 95th percentile using the formula:
        /// <c>P95 = μ + (z₀.₉₅ × σ)</c>
        /// where:
        /// <list type="bullet">
        /// <item><description>μ (mu) = mean/average value</description></item>
        /// <item><description>σ (sigma) = standard deviation</description></item>
        /// <item><description>z₀.₉₅ = 1.6448536269514722 (exact z-score for 95th percentile)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <b>Constraint:</b> If <paramref name="maxValue"/> is provided, the result will not exceed it.
        /// This prevents unrealistic percentile values when the data distribution has a hard upper bound.
        /// </para>
        /// <para>
        /// Returns null if:
        /// <list type="bullet">
        /// <item><description><paramref name="avgValue"/> is null</description></item>
        /// <item><description><paramref name="stdDevValue"/> is null or ≤ 0</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic calculation
        /// var p95 = StatisticsCalculator.CalculatePercentile95(100.0, 15.0, null);
        /// // p95 ≈ 124.67
        /// 
        /// // With max constraint
        /// var p95Constrained = StatisticsCalculator.CalculatePercentile95(100.0, 15.0, 120.0);
        /// // p95Constrained = 120.0 (constrained by maxValue)
        /// 
        /// // No standard deviation
        /// var p95NoStdDev = StatisticsCalculator.CalculatePercentile95(100.0, 0.0, null);
        /// // p95NoStdDev = null
        /// </code>
        /// </example>
        public static double? CalculatePercentile95(double? avgValue, double? stdDevValue, double? maxValue)
        {
            // Validate inputs
            if(!avgValue.HasValue || !stdDevValue.HasValue || stdDevValue.Value <= 0)
            {
                return null;
            }

            // Calculate theoretical P95 using normal distribution
            var theoreticalP95 = avgValue.Value + (ZScore95 * stdDevValue.Value);

            // Constraint: P95 cannot exceed the maximum observed value
            return maxValue.HasValue ? Math.Min(theoreticalP95, maxValue.Value) : theoreticalP95;
        }
    }
}
