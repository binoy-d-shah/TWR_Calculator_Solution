using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

[TestFixture]
public class TimeWeightedReturnCalculatorTests
{
    private ITimeWeightedReturnCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator = new TimeWeightedReturnCalculator();
    }

    /// <summary>
    /// A helper method to parse data from a CSV string.
    /// </summary>
    /// <param name="csvData">A string containing CSV data.</param>
    /// <returns>A SortedDictionary of DateTime to decimal values.</returns>
    private SortedDictionary<DateTime, decimal> ParseCsvData(string csvData)
    {
        var data = new SortedDictionary<DateTime, decimal>();
        var lines = csvData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out var date) && decimal.TryParse(parts[1], out var value))
            {
                data[date] = value;
            }
        }
        return data;
    }

    /// <summary>
    /// A helper method to parse data from a CSV string.
    /// </summary>
    /// <param name="csvData">A string containing CSV data.</param>
    /// <returns>A SortedDictionary of DateTime to decimal values.</returns>
    private SortedDictionary<DateTime, decimal> ReadCsvData(string filePath)
    {
        var data = new SortedDictionary<DateTime, decimal>();
        var lines = File.ReadAllLines(filePath);
        // Skip the header row
        foreach (var line in lines.Skip(1))
        {
            // Use semicolon as a delimiter
            var parts = line.Split(';');
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out var date) && decimal.TryParse(parts[1], out var value))
            {
                data[date] = value;
            }
        }
        return data;
    }

    [Test]
    public void TWR_SimplePositiveReturn_ReturnsCorrectValue()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 1, 31), 110m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0.10m).Within(0.0001m)); // 10% return
    }

    [Test]
    public void TWR_SimpleNegativeReturn_ReturnsCorrectValue()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 1, 31), 90m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(-0.10m).Within(0.0001m)); // -10% return
    }

    [Test]
    public void TWR_WithCashFlow_ReturnsCorrectValue()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 1, 15), 105m },
            { new DateTime(2023, 1, 31), 120m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 15), 10m } // A contribution of 10
        };
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        decimal expected = (105m / (100m + 10m)) * (120m / 105m) - 1;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expected).Within(0.0001m));
    }

    [Test]
    public void TWR_AnnualizedReturnTrue_ReturnsAnnualizedValue()
    {
        // Arrange: period of 6 months
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 7, 1), 110m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 7, 1);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, true);

        // Assert
        decimal nonAnnualizedReturn = 0.10m; // 10%
        double years = (endDate - startDate).TotalDays / 365.25;
        decimal expectedAnnualized = (decimal)(Math.Pow((double)(1 + nonAnnualizedReturn), 1 / years) - 1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedAnnualized).Within(0.0001m));
    }

    [Test]
    public void TWR_AnnualizedReturnFalse_ReturnsNonAnnualizedValue()
    {
        // Arrange: period of 6 months
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 7, 1), 110m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 7, 1);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0.10m).Within(0.0001m));
    }

    [Test]
    public void TWR_ZeroReturn_ReturnsZero()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 1, 31), 100m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0.0m).Within(0.0001m));
    }

    [Test]
    public void TWR_EmptyNAVSeries_ReturnsNull()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>();
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TWR_EvaluationStartAfterEnd_ReturnsNull()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 1, 31), 110m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 31);
        var endDate = new DateTime(2023, 1, 1);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TWR_NAVSeriesWithOnlyOnePoint_ReturnsNull()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TWR_WithMultipleSubPeriods_ReturnsCorrectValue()
    {
        // Arrange
        var navTimeSeries = new SortedDictionary<DateTime, decimal>
        {
            { new DateTime(2023, 1, 1), 100m },
            { new DateTime(2023, 1, 15), 105m },
            { new DateTime(2023, 1, 20), 100m },
            { new DateTime(2023, 1, 31), 110m }
        };
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 1, 31);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Expected: (105/100) * (100/105) * (110/100) - 1 = 1.10 - 1 = 0.10
        decimal expected = (105m / 100m) * (100m / 105m) * (110m / 100m) - 1;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expected).Within(0.0001m));
    }

    [Test]
    public void TWR_ComplexDataFromCsv_ReturnsCorrectValue()
    {
        // Arrange: A more complex scenario using data from the provided CSV files.
        // We will now embed the data directly in the test to ensure consistency.
        string navCsv = @"2018-01-01,1000
2018-01-05,1020
2018-01-10,1015
2018-01-15,1050";

        string cashFlowCsv = @"2018-01-05,50";

        var navTimeSeries = ParseCsvData(navCsv);
        var externalFlows = ParseCsvData(cashFlowCsv);

        var startDate = new DateTime(2018, 1, 1);
        var endDate = new DateTime(2018, 1, 15);

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Expected calculation:
        // Period 1 (Jan 1 to Jan 5): (1020 / (1000 + 50)) - 1
        // Period 2 (Jan 5 to Jan 10): (1015 / 1020) - 1
        // Period 3 (Jan 10 to Jan 15): (1050 / 1015) - 1
        // Total TWR = (1 + Period1_Return) * (1 + Period2_Return) * (1 + Period3_Return) - 1
        decimal expected = (1020m / (1000m + 50m)) * (1015m / 1020m) * (1050m / 1015m) - 1;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expected).Within(0.0001m));
    }

    [Test]
    public void TWR_ComplexDataFromCsvFile_ReturnsCorrectValue()
    {
        // Arrange: Dynamically determine the start and end dates from the CSV data.
        var navTimeSeries = ReadCsvData("TestData/NAVTimeSeries.csv");
        var externalFlows = ReadCsvData("TestData/CashflowTimeSeries.csv");

        var minDate = new[] { navTimeSeries.Keys.Min(), externalFlows.Keys.Min() }.Min();
        var maxDate = new[] { navTimeSeries.Keys.Max(), externalFlows.Keys.Max() }.Max();

        var startDate = minDate;
        var endDate = maxDate;

        // Act
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);

        // Assert
        Assert.That(Math.Round(result ?? 0m, 4), Is.EqualTo(-35.2173m).Within(0.0001m));
    }

    [Test]
    public void TWR_PerformanceTest_LargeSyntheticData_IsFast()
    {
        // Arrange
        // Create a large, synthetic data set to test performance.
        var navTimeSeries = new SortedDictionary<DateTime, decimal>();
        var externalFlows = new SortedDictionary<DateTime, decimal>();
        var startDate = new DateTime(2000, 1, 1);
        var endDate = new DateTime(2025, 1, 1);
        decimal currentNav = 100m;
        
        // Generate daily data for a 25-year period.
        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Add a small random fluctuation to the NAV.
            currentNav += (decimal)(new Random().NextDouble() * 2 - 1);
            navTimeSeries.Add(date, currentNav);
            
            // Add a cash flow on a specific interval (e.g., every 90 days).
            if (date.DayOfYear % 90 == 0)
            {
                externalFlows.Add(date, 5m);
            }
        }
        
        var stopwatch = new Stopwatch();
        
        // Act
        stopwatch.Start();
        var result = _calculator.CalculateTimeWeightedReturn(externalFlows, navTimeSeries, startDate, endDate, false);
        stopwatch.Stop();
        
        // Assert
        // Check that the result is valid and that the calculation took less than a specific time.
        // The time limit (e.g., 500ms) will depend on the expected performance of the system.
        Assert.That(result, Is.Not.Null);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500));
    }
}
