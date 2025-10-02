using System.ComponentModel;

namespace GroupChat.Dotnet.Models.Financial;

/// <summary>
/// Types of financial reports
/// </summary>
public enum ReportType
{
    [Description("Sales reports and analysis")]
    Sales,
    
    [Description("Revenue tracking and forecasting")]
    Revenue,
    
    [Description("Expense analysis and categorization")]
    Expense,
    
    [Description("Performance metrics and KPIs")]
    Performance,
    
    [Description("Inventory tracking and management")]
    Inventory,
    
    [Description("Customer analytics and behavior")]
    Customer,
    
    [Description("Payroll and compensation analysis")]
    Payroll
}

/// <summary>
/// Types of data sources for financial data
/// </summary>
public enum DataSourceType
{
    [Description("CSV file source")]
    Csv,
    
    [Description("Excel spreadsheet")]
    Excel,
    
    [Description("Database connection")]
    Database,
    
    [Description("API integration")]
    Api
}

/// <summary>
/// Trend analysis directions
/// </summary>
public enum TrendDirection
{
    [Description("Upward trend")]
    Up,
    
    [Description("Downward trend")]
    Down,
    
    [Description("Stable trend")]
    Stable,
    
    [Description("Volatile pattern")]
    Volatile
}