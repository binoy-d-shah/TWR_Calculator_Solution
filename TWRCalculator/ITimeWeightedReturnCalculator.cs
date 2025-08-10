using System;
using System.Collections.Generic;

public interface ITimeWeightedReturnCalculator
{
    decimal? CalculateTimeWeightedReturn(
        SortedDictionary<DateTime, decimal> externalFlowTimeSeries,
        SortedDictionary<DateTime, decimal> navTimeSeries,
        DateTime evaluationStart,
        DateTime evaluationEnd,
        bool annualizeReturn);
}
