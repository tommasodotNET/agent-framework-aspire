using System;
using System.Collections.Generic;

namespace GroupChat.Dotnet.Models.Financial;

/// <summary>
/// Sales report data model
/// </summary>
public class SalesReport
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
    public double ProfitMargin { get; set; }
    public DateTime SalesDate { get; set; }
    public string Region { get; set; } = string.Empty;
    public string SalesRep { get; set; } = string.Empty;
}

/// <summary>
/// Revenue tracking data model
/// </summary>
public class RevenueData
{
    public string Period { get; set; } = string.Empty; // "Q1 2024", "2024-09", etc.
    public decimal Revenue { get; set; }
    public decimal RecurringRevenue { get; set; }
    public decimal OneTimeRevenue { get; set; }
    public double GrowthRate { get; set; }
    public string RevenueSource { get; set; } = string.Empty;
    public string? Department { get; set; }
}

/// <summary>
/// Expense tracking data model
/// </summary>
public class ExpenseRecord
{
    public string ExpenseId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
}

/// <summary>
/// Employee performance metrics
/// </summary>
public class EmployeePerformance
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public double PerformanceScore { get; set; }
    public decimal? SalesTarget { get; set; }
    public decimal? SalesAchieved { get; set; }
    public decimal? Commission { get; set; }
    public string ReviewPeriod { get; set; } = string.Empty;
}

/// <summary>
/// Key financial metrics and calculations
/// </summary>
public class FinancialMetrics
{
    public string MetricName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Period { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public decimal? Benchmark { get; set; }
    public TrendDirection Trend { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Budget analysis data model
/// </summary>
public class BudgetAnalysis
{
    public string Department { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BudgetAllocated { get; set; }
    public decimal ActualSpending { get; set; }
    public decimal Variance { get; set; }
    public double VariancePercentage { get; set; }
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "under", "over", "on-track"
}