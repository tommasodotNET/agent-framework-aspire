using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using GroupChat.Dotnet.Services;
using GroupChat.Dotnet.Models.Financial;

namespace GroupChat.Dotnet.Tools;

/// <summary>
/// Financial analysis tools that integrate with AI agents for business metrics and reporting
/// </summary>
public class FinancialTools
{
    private readonly FinancialService _financialService;

    public FinancialTools(FinancialService financialService)
    {
        _financialService = financialService;
    }

    [Description("Search and analyze sales data by various criteria")]
    public async Task<string> SearchSalesDataAsync(
        [Description("Search query for sales data (product name, category, region, etc.)")] string query,
        [Description("Date range filter (e.g., 'last_quarter', 'last_month', 'last_week')")] string? dateRange = null,
        [Description("Product category filter")] string? category = null)
    {
        var salesReports = await _financialService.GetSalesReportsAsync(dateRange, category);
        
        // Filter based on query
        var filteredReports = salesReports.Where(report =>
            report.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            report.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            report.Region.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            report.SalesRep.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        var results = filteredReports.Select(report => new
        {
            product_id = report.ProductId,
            product_name = report.ProductName,
            category = report.Category,
            sales_amount = report.SalesAmount,
            units_sold = report.UnitsSold,
            revenue = report.Revenue,
            profit_margin = report.ProfitMargin,
            sales_date = report.SalesDate.ToString("yyyy-MM-dd"),
            region = report.Region,
            sales_rep = report.SalesRep
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            query = query,
            date_range = dateRange,
            category = category,
            total_results = results.Count,
            total_revenue = results.Sum(r => r.revenue),
            avg_profit_margin = results.Count > 0 ? results.Average(r => r.profit_margin) : 0,
            results = results
        });
    }

    [Description("Analyze revenue trends and patterns over specified periods")]
    public async Task<string> AnalyzeRevenueTrendsAsync(
        [Description("Analysis period: 'monthly', 'quarterly', 'yearly'")] string period,
        [Description("Metrics to analyze: 'growth_rate', 'recurring_revenue', 'forecasting'")] List<string>? metrics = null)
    {
        metrics ??= new List<string> { "growth_rate", "recurring_revenue" };
        
        var revenueData = await _financialService.GetRevenueDataAsync(period);
        
        var revenueResults = revenueData.Select(data => new
        {
            period = data.Period,
            revenue = data.Revenue,
            recurring_revenue = data.RecurringRevenue,
            one_time_revenue = data.OneTimeRevenue,
            growth_rate = data.GrowthRate,
            revenue_source = data.RevenueSource,
            department = data.Department
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            analysis_period = period,
            metrics_analyzed = metrics,
            total_periods = revenueResults.Count,
            revenue_data = revenueResults,
            summary = new
            {
                total_revenue = revenueResults.Sum(r => r.revenue),
                avg_growth_rate = revenueResults.Count > 0 ? revenueResults.Average(r => r.growth_rate) : 0,
                recurring_percentage = revenueResults.Sum(r => r.revenue) > 0 ? 
                    revenueResults.Sum(r => r.recurring_revenue) / revenueResults.Sum(r => r.revenue) * 100 : 0
            }
        });
    }

    [Description("Calculate key business metrics and KPIs")]
    public async Task<string> CalculateBusinessMetricsAsync(
        [Description("Metrics to calculate: 'customer_acquisition_cost', 'revenue_growth_rate', 'profit_margin', 'customer_lifetime_value'")] List<string> metricNames,
        [Description("Calculation period (e.g., 'Q4 2024', '2024', 'last_quarter')")] string period = "current")
    {
        var financialMetrics = await _financialService.CalculateFinancialMetricsAsync(metricNames, period);
        
        var metricsResults = financialMetrics.Select(metric => new
        {
            metric_name = metric.MetricName,
            value = metric.Value,
            period = metric.Period,
            calculation_method = metric.CalculationMethod,
            benchmark = metric.Benchmark,
            trend = metric.Trend.ToString().ToLower(),
            metadata = metric.Metadata
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            requested_metrics = metricNames,
            calculation_period = period,
            total_metrics = metricsResults.Count,
            metrics = metricsResults,
            performance_summary = new
            {
                metrics_above_benchmark = metricsResults.Count(m => m.benchmark.HasValue && m.value > m.benchmark.Value),
                positive_trends = metricsResults.Count(m => m.trend is "up" or "stable"),
                areas_for_improvement = metricsResults.Where(m => m.trend == "down").Select(m => m.metric_name).ToList()
            }
        });
    }

    [Description("Get top-performing products based on specified metrics and time periods")]
    public async Task<string> GetTopPerformingProductsAsync(
        [Description("Time period for analysis: 'last_quarter', 'last_month', 'last_week'")] string timePeriod,
        [Description("Performance metric: 'revenue', 'profit_margin', 'units_sold', 'growth_rate'")] string metric = "revenue",
        [Description("Number of top products to return")] int limit = 10)
    {
        var topProducts = await _financialService.GetTopPerformingProductsAsync(timePeriod, metric, limit);
        
        var results = topProducts.Select(product => new
        {
            product_id = product.ProductId,
            product_name = product.ProductName,
            category = product.Category,
            metric_value = metric.ToLower() switch
            {
                "revenue" => (object)product.Revenue,
                "profit_margin" => product.ProfitMargin,
                "units_sold" => product.UnitsSold,
                _ => product.Revenue
            },
            sales_amount = product.SalesAmount,
            region = product.Region,
            sales_rep = product.SalesRep
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            time_period = timePeriod,
            performance_metric = metric,
            limit = limit,
            total_products = results.Count,
            top_products = results
        });
    }

    [Description("Analyze budget performance and variances across departments")]
    public async Task<string> AnalyzeBudgetPerformanceAsync(
        [Description("Department filter (optional)")] string? department = null,
        [Description("Analysis period")] string period = "current")
    {
        var budgetAnalysis = await _financialService.GetBudgetAnalysisAsync(department);
        
        var results = budgetAnalysis.Select(budget => new
        {
            department = budget.Department,
            category = budget.Category,
            budget_allocated = budget.BudgetAllocated,
            actual_spending = budget.ActualSpending,
            variance = budget.Variance,
            variance_percentage = budget.VariancePercentage,
            period = budget.Period,
            status = budget.Status
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            department_filter = department,
            analysis_period = period,
            total_budget_items = results.Count,
            budget_analysis = results,
            summary = new
            {
                total_allocated = results.Sum(r => r.budget_allocated),
                total_spent = results.Sum(r => r.actual_spending),
                total_variance = results.Sum(r => r.variance),
                over_budget_count = results.Count(r => r.status == "over"),
                under_budget_count = results.Count(r => r.status == "under"),
                on_track_count = results.Count(r => r.status == "on-track")
            }
        });
    }

    /// <summary>
    /// Get the financial analysis functions for AI agent registration
    /// </summary>
    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(SearchSalesDataAsync);
        yield return AIFunctionFactory.Create(AnalyzeRevenueTrendsAsync);
        yield return AIFunctionFactory.Create(CalculateBusinessMetricsAsync);
        yield return AIFunctionFactory.Create(GetTopPerformingProductsAsync);
        yield return AIFunctionFactory.Create(AnalyzeBudgetPerformanceAsync);
    }
}