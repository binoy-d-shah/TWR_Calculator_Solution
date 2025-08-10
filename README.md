# Time-Weighted Return (TWR) Calculator

## Introduction
This project provides a robust **C#** implementation for calculating the **Time-Weighted Return (TWR)** of an investment portfolio. 

The TWR is a standard measure of investment performance that removes the effects of external cash flows (like contributions or withdrawals), making it ideal for evaluating the skill of an investment manager.

The calculator is designed to be resilient, handling scenarios where the start and end dates of the evaluation period do not perfectly align with the dates in the provided Net Asset Value (NAV) series.

## Features
- **Accurate Calculation:** Correctly calculates TWR by breaking the evaluation period into sub-periods based on cash flow dates and NAV dates.  
- **External Cash Flow Handling:** Properly accounts for cash contributions and withdrawals.  
- **Annualization:** Supports annualizing the final return to allow for comparison with other investments over different timeframes.  
- **Robustness:** Gracefully handles invalid inputs, such as empty data series or an evaluation start date that comes after the end date.  
- **NUnit Tests:** A comprehensive set of unit tests is included to verify the correctness of the implementation.  

## Installation and Setup
**Requirements:**
- .NET SDK (**version 8.0** or later)  
- A C# IDE like **Visual Studio** or **Visual Studio Code**  

**Steps:**
1. Clone the repository:
   ```bash
   git clone https://github.com/binoy-d-shah/TWR_Calculator_Solution.git
   cd TWR_Calculator_Solution
   ```

2. Open the project:  
   
   Open the `TWR_Calculator_Solution.sln` file in your preferred C# IDE.  
   The solution contains two projects:  
   - **TWRCalculator** (the main library)  
   - **TWRCalculator.Tests** (the unit tests)

3. Restore dependencies:  
   
   The project uses the NUnit test framework. Your IDE should automatically restore these dependencies when you open the solution.  
   If not, run:
   ```bash
   dotnet restore
   ```

## Usage
The core functionality is provided by the `TimeWeightedReturnCalculator` class, which implements the `ITimeWeightedReturnCalculator` interface.

To use the calculator, provide:
- A series of **cash flows**
- A series of **Net Asset Values (NAVs)**  
Both as `SortedDictionary<DateTime, decimal>` objects.

**Example:**
```csharp
var calculator = new TimeWeightedReturnCalculator();

var navTimeSeries = new SortedDictionary<DateTime, decimal>
{
    { new DateTime(2023, 1, 1), 1000m },
    { new DateTime(2023, 1, 15), 1050m },
    { new DateTime(2023, 1, 31), 1200m }
};

var externalFlows = new SortedDictionary<DateTime, decimal>
{
    { new DateTime(2023, 1, 15), 50m } // A contribution of $50
};

var startDate = new DateTime(2023, 1, 1);
var endDate = new DateTime(2023, 1, 31);

// Calculate the non-annualized TWR
decimal? twr = calculator.CalculateTimeWeightedReturn(
    externalFlows,
    navTimeSeries,
    startDate,
    endDate,
    false);

if (twr.HasValue)
{
    Console.WriteLine($"The Time-Weighted Return is: {twr.Value:P2}");
}
else
{
    Console.WriteLine("Could not calculate TWR due to invalid data.");
}
```

## Running the Tests
The `TWRCalculator.Tests` project contains a suite of unit tests to ensure the calculator's correctness.

**From the command line:**
```bash
dotnet test
```
This will build the test project and run all tests.

**From Visual Studio:**

Use the **Test Explorer** to discover and run tests.  
Right-click on the test project or individual test methods and select **Run Tests**.
