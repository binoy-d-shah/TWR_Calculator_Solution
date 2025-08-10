using System.Linq;

public class TimeWeightedReturnCalculator : ITimeWeightedReturnCalculator
{
    /// <summary>
    /// Calculates the Time-Weighted Return for a given period.
    /// </summary>
    /// <param name="externalFlowTimeSeries">A sorted dictionary of cash flows by date.</param>
    /// <param name="navTimeSeries">A sorted dictionary of Net Asset Values by date.</param>
    /// <param name="evaluationStart">The start date of the evaluation period.</param>
    /// <param name="evaluationEnd">The end date of the evaluation period.</param>
    /// <param name="annualizeReturn">Whether to annualize the return.</param>
    /// <returns>The calculated TWR, or null if the input is invalid.</returns>
    public decimal? CalculateTimeWeightedReturn(
        SortedDictionary<DateTime, decimal> externalFlowTimeSeries,
        SortedDictionary<DateTime, decimal> navTimeSeries,
        DateTime evaluationStart,
        DateTime evaluationEnd,
        bool annualizeReturn)
    {
        // 1. Validate inputs
        if (navTimeSeries == null || !navTimeSeries.Any() || evaluationStart >= evaluationEnd)
        {
            return null;
        }

        // 2. Determine the actual start and end dates for the calculation
        // Find the last NAV on or before the evaluation start date.
        var startNavEntry = navTimeSeries.Where(kvp => kvp.Key <= evaluationStart).LastOrDefault();
        // Find the last NAV on or before the evaluation end date.
        var endNavEntry = navTimeSeries.Where(kvp => kvp.Key <= evaluationEnd).LastOrDefault();

        // Check if valid start and end NAVs were found.
        if (startNavEntry.Equals(default(KeyValuePair<DateTime, decimal>)) ||
            endNavEntry.Equals(default(KeyValuePair<DateTime, decimal>)) ||
            endNavEntry.Key <= startNavEntry.Key)
        {
            return null;
        }

        var actualStart = startNavEntry.Key;
        var actualEnd = endNavEntry.Key;

        // 3. Combine relevant dates from both series and sort them to define sub-periods
        var allDates = new SortedSet<DateTime>();
        allDates.Add(actualStart);

        // Add all NAV dates within the period
        foreach (var navDate in navTimeSeries.Keys.Where(d => d > actualStart && d <= actualEnd))
        {
            allDates.Add(navDate);
        }

        // Add all cash flow dates within the period
        foreach (var flowDate in externalFlowTimeSeries.Keys.Where(d => d > actualStart && d <= actualEnd))
        {
            allDates.Add(flowDate);
        }

        // If we don't have at least two points to define a period, we can't calculate a return.
        if (allDates.Count <= 1)
        {
            return null;
        }

        // 4. Calculate the return for each sub-period and multiply them together.
        decimal cumulativeReturnFactor = 1.0m;
        var dateArray = allDates.ToArray();

        for (int i = 0; i < dateArray.Length - 1; i++)
        {
            var periodStart = dateArray[i];
            var periodEnd = dateArray[i + 1];

            // Get NAV at the start and end of the sub-period.
            decimal navStart = navTimeSeries[periodStart];
            decimal navEnd = navTimeSeries[periodEnd];

            // Sum all cash flows within the sub-period.
            decimal cashFlowsInPeriod = 0m;
            foreach (var flow in externalFlowTimeSeries)
            {
                if (flow.Key > periodStart && flow.Key <= periodEnd)
                {
                    cashFlowsInPeriod += flow.Value;
                }
            }

            // Calculate the sub-period return
            decimal denominator = navStart + cashFlowsInPeriod;
            if (denominator == 0)
            {
                // To avoid division by zero, we will return null.
                return null;
            }

            decimal returnForPeriod = (navEnd - denominator) / denominator;
            cumulativeReturnFactor *= (1 + returnForPeriod);
        }

        decimal totalReturn = cumulativeReturnFactor - 1;

        // 5. Annualize the return if requested
        if (annualizeReturn)
        {
            double daysInPeriod = (evaluationEnd - evaluationStart).TotalDays;
            if (daysInPeriod > 0)
            {
                double yearsInPeriod = daysInPeriod / 365.25; // Accounting for leap years
                totalReturn = (decimal)(Math.Pow((double)(1 + totalReturn), 1 / yearsInPeriod) - 1);
            }
        }

        return totalReturn;
    }
}
