"""
Financial analysis and business metrics models.
"""
from typing import List, Optional, Dict, Any
from datetime import datetime
from pydantic import BaseModel, Field
from enum import Enum
from decimal import Decimal


class ReportType(str, Enum):
    """Types of financial reports."""
    SALES = "sales"
    REVENUE = "revenue"
    EXPENSE = "expense"
    PERFORMANCE = "performance"
    INVENTORY = "inventory"
    CUSTOMER = "customer"
    PAYROLL = "payroll"


class DataSourceType(str, Enum):
    """Types of data sources."""
    CSV = "csv"
    EXCEL = "excel"
    DATABASE = "database"
    API = "api"


class TrendDirection(str, Enum):
    """Trend analysis directions."""
    UP = "up"
    DOWN = "down"
    STABLE = "stable"
    VOLATILE = "volatile"


class SalesReport(BaseModel):
    """Sales report data model."""
    product_id: str
    product_name: str
    category: str
    sales_amount: Decimal
    units_sold: int
    revenue: Decimal
    profit_margin: float
    sales_date: datetime
    region: str
    sales_rep: str


class RevenueData(BaseModel):
    """Revenue tracking data model."""
    period: str  # "Q1 2024", "2024-09", etc.
    revenue: Decimal
    recurring_revenue: Decimal
    one_time_revenue: Decimal
    growth_rate: float
    revenue_source: str
    department: Optional[str] = None


class ExpenseRecord(BaseModel):
    """Expense tracking data model."""
    expense_id: str
    category: str
    subcategory: str
    amount: Decimal
    expense_date: datetime
    department: str
    vendor: Optional[str] = None
    description: str
    approval_status: str


class EmployeePerformance(BaseModel):
    """Employee performance metrics."""
    employee_id: str
    employee_name: str
    department: str
    role: str
    performance_score: float
    sales_target: Optional[Decimal] = None
    sales_achieved: Optional[Decimal] = None
    commission: Optional[Decimal] = None
    review_period: str


class InventoryData(BaseModel):
    """Inventory tracking data."""
    item_id: str
    item_name: str
    category: str
    current_stock: int
    reorder_level: int
    unit_cost: Decimal
    retail_price: Decimal
    supplier: str
    last_updated: datetime
    location: str


class CustomerData(BaseModel):
    """Customer demographics and behavior data."""
    customer_id: str
    acquisition_date: datetime
    acquisition_cost: Decimal
    lifetime_value: Decimal
    segment: str  # "enterprise", "smb", "individual"
    region: str
    total_orders: int
    avg_order_value: Decimal
    last_order_date: Optional[datetime] = None
    churn_risk: str  # "low", "medium", "high"


class FinancialMetrics(BaseModel):
    """Key financial metrics and calculations."""
    metric_name: str
    value: Decimal
    period: str
    calculation_method: str
    benchmark: Optional[Decimal] = None
    trend: TrendDirection
    metadata: Dict[str, Any] = Field(default_factory=dict)


class AnalysisRequest(BaseModel):
    """Request model for financial analysis."""
    analysis_type: str
    data_source: Optional[DataSourceType] = None
    date_range: Optional[str] = None
    filters: Dict[str, Any] = Field(default_factory=dict)
    metrics: List[str] = Field(default_factory=list)


class ChartConfig(BaseModel):
    """Chart generation configuration."""
    chart_type: str  # "line", "bar", "pie", "scatter"
    title: str
    x_axis: str
    y_axis: str
    data_series: List[str]
    colors: Optional[List[str]] = None
    export_format: str = "png"  # "png", "pdf", "svg"