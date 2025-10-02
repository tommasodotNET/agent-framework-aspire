using GroupChat.Dotnet.Models.Financial;

namespace GroupChat.Dotnet.Services;

/// <summary>
/// Core business logic for financial analysis, reporting, and business metrics
/// </summary>
public class FinancialService
{
    /// <summary>
    /// Get sales reports with optional filtering by date range and category
    /// </summary>
    public async Task<List<SalesReport>> GetSalesReportsAsync(string? dateRange = null, string? category = null)
    {
        await Task.Delay(50); // Simulate async operation
        
        var salesData = new List<SalesReport>
        {
            new SalesReport
            {
                ProductId = "PROD001",
                ProductName = "Enterprise Software License",
                Category = "Software",
                SalesAmount = 125000.00m,
                UnitsSold = 50,
                Revenue = 125000.00m,
                ProfitMargin = 0.75,
                SalesDate = DateTime.Now.AddDays(-15),
                Region = "North America",
                SalesRep = "Alice Johnson"
            },
            new SalesReport
            {
                ProductId = "PROD002",
                ProductName = "Consulting Services",
                Category = "Services",
                SalesAmount = 89500.00m,
                UnitsSold = 179,
                Revenue = 89500.00m,
                ProfitMargin = 0.65,
                SalesDate = DateTime.Now.AddDays(-8),
                Region = "Europe",
                SalesRep = "Bob Smith"
            },
            new SalesReport
            {
                ProductId = "PROD003",
                ProductName = "Hardware Components",
                Category = "Hardware",
                SalesAmount = 67800.00m,
                UnitsSold = 226,
                Revenue = 67800.00m,
                ProfitMargin = 0.35,
                SalesDate = DateTime.Now.AddDays(-22),
                Region = "Asia Pacific",
                SalesRep = "Carol Chen"
            },
            new SalesReport
            {
                ProductId = "PROD004",
                ProductName = "Training Package",
                Category = "Services",
                SalesAmount = 34500.00m,
                UnitsSold = 115,
                Revenue = 34500.00m,
                ProfitMargin = 0.80,
                SalesDate = DateTime.Now.AddDays(-5),
                Region = "North America",
                SalesRep = "David Wilson"
            }
        };

        // Apply filters
        if (!string.IsNullOrEmpty(category))
        {
            salesData = salesData.Where(s => s.Category.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrEmpty(dateRange))
        {
            // Simple date range filtering - in real implementation this would be more sophisticated
            var cutoffDate = dateRange.ToLower() switch
            {
                "last_week" => DateTime.Now.AddDays(-7),
                "last_month" => DateTime.Now.AddDays(-30),
                "last_quarter" => DateTime.Now.AddDays(-90),
                _ => DateTime.MinValue
            };
            
            if (cutoffDate != DateTime.MinValue)
            {
                salesData = salesData.Where(s => s.SalesDate >= cutoffDate).ToList();
            }
        }

        return salesData;
    }

    /// <summary>
    /// Get revenue data for trend analysis
    /// </summary>
    public async Task<List<RevenueData>> GetRevenueDataAsync(string period)
    {
        await Task.Delay(50);
        
        return new List<RevenueData>
        {
            new RevenueData
            {
                Period = "Q3 2024",
                Revenue = 450000m,
                RecurringRevenue = 320000m,
                OneTimeRevenue = 130000m,
                GrowthRate = 0.15,
                RevenueSource = "Software Sales",
                Department = "Sales"
            },
            new RevenueData
            {
                Period = "Q2 2024",
                Revenue = 390000m,
                RecurringRevenue = 290000m,
                OneTimeRevenue = 100000m,
                GrowthRate = 0.08,
                RevenueSource = "Software Sales",
                Department = "Sales"
            },
            new RevenueData
            {
                Period = "Q1 2024",
                Revenue = 360000m,
                RecurringRevenue = 270000m,
                OneTimeRevenue = 90000m,
                GrowthRate = 0.12,
                RevenueSource = "Software Sales",
                Department = "Sales"
            }
        };
    }

    /// <summary>
    /// Calculate key financial metrics
    /// </summary>
    public async Task<List<FinancialMetrics>> CalculateFinancialMetricsAsync(List<string> metricNames, string period)
    {
        await Task.Delay(50);
        
        var metrics = new List<FinancialMetrics>();
        
        foreach (var metricName in metricNames)
        {
            var metric = metricName.ToLower() switch
            {
                "customer_acquisition_cost" => new FinancialMetrics
                {
                    MetricName = "Customer Acquisition Cost",
                    Value = 85.50m,
                    Period = period,
                    CalculationMethod = "Total Marketing Spend / New Customers Acquired",
                    Benchmark = 100.00m,
                    Trend = TrendDirection.Down,
                    Metadata = new Dictionary<string, object> { ["currency"] = "USD", ["target"] = 75.00 }
                },
                "revenue_growth_rate" => new FinancialMetrics
                {
                    MetricName = "Revenue Growth Rate",
                    Value = 15.3m,
                    Period = period,
                    CalculationMethod = "((Current Period Revenue - Previous Period Revenue) / Previous Period Revenue) * 100",
                    Benchmark = 12.0m,
                    Trend = TrendDirection.Up,
                    Metadata = new Dictionary<string, object> { ["unit"] = "percentage", ["target"] = 18.0 }
                },
                "profit_margin" => new FinancialMetrics
                {
                    MetricName = "Profit Margin",
                    Value = 22.8m,
                    Period = period,
                    CalculationMethod = "(Net Income / Revenue) * 100",
                    Benchmark = 20.0m,
                    Trend = TrendDirection.Up,
                    Metadata = new Dictionary<string, object> { ["unit"] = "percentage", ["industry_avg"] = 18.5 }
                },
                "customer_lifetime_value" => new FinancialMetrics
                {
                    MetricName = "Customer Lifetime Value",
                    Value = 2850.00m,
                    Period = period,
                    CalculationMethod = "Average Revenue Per Customer * Average Customer Lifespan",
                    Benchmark = 2500.00m,
                    Trend = TrendDirection.Up,
                    Metadata = new Dictionary<string, object> { ["currency"] = "USD", ["cohort"] = "enterprise" }
                },
                _ => new FinancialMetrics
                {
                    MetricName = metricName,
                    Value = 0m,
                    Period = period,
                    CalculationMethod = "Unknown metric",
                    Trend = TrendDirection.Stable
                }
            };
            
            metrics.Add(metric);
        }
        
        return metrics;
    }

    /// <summary>
    /// Get budget analysis data
    /// </summary>
    public async Task<List<BudgetAnalysis>> GetBudgetAnalysisAsync(string? department = null)
    {
        await Task.Delay(50);
        
        var budgetData = new List<BudgetAnalysis>
        {
            new BudgetAnalysis
            {
                Department = "Marketing",
                Category = "Digital Advertising",
                BudgetAllocated = 50000m,
                ActualSpending = 47500m,
                Variance = -2500m,
                VariancePercentage = -5.0,
                Period = "Q3 2024",
                Status = "under"
            },
            new BudgetAnalysis
            {
                Department = "Engineering",
                Category = "Software Tools",
                BudgetAllocated = 25000m,
                ActualSpending = 28000m,
                Variance = 3000m,
                VariancePercentage = 12.0,
                Period = "Q3 2024",
                Status = "over"
            },
            new BudgetAnalysis
            {
                Department = "Sales",
                Category = "Travel & Entertainment",
                BudgetAllocated = 15000m,
                ActualSpending = 14800m,
                Variance = -200m,
                VariancePercentage = -1.3,
                Period = "Q3 2024",
                Status = "on-track"
            }
        };

        if (!string.IsNullOrEmpty(department))
        {
            budgetData = budgetData.Where(b => b.Department.Contains(department, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return budgetData;
    }

    /// <summary>
    /// Get top performing products
    /// </summary>
    public async Task<List<SalesReport>> GetTopPerformingProductsAsync(string timePeriod, string metric = "revenue", int limit = 10)
    {
        var salesData = await GetSalesReportsAsync(timePeriod);
        
        return metric.ToLower() switch
        {
            "revenue" => salesData.OrderByDescending(s => s.Revenue).Take(limit).ToList(),
            "profit_margin" => salesData.OrderByDescending(s => s.ProfitMargin).Take(limit).ToList(),
            "units_sold" => salesData.OrderByDescending(s => s.UnitsSold).Take(limit).ToList(),
            _ => salesData.OrderByDescending(s => s.Revenue).Take(limit).ToList()
        };
    }
}