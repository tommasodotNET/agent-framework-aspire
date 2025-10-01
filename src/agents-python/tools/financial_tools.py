"""
Financial analysis tools - AI agent tools for financial analysis, reporting, and business metrics.
"""
import asyncio
import json
from typing import List, Optional, Dict, Any
from typing_extensions import Annotated
from pydantic import Field
from decimal import Decimal

from services.financial_service import FinancialService


class FinancialTools:
    """Financial analysis tools that integrate with AI agents for business metrics and reporting."""
    
    def __init__(self, financial_service: FinancialService):
        """Initialize with financial service dependency."""
        self.financial_service = financial_service
    
    async def search_sales_data(
        self,
        query: Annotated[str, Field(description="Search query for sales data (product name, category, region, etc.).")],
        date_range: Annotated[Optional[str], Field(description="Date range filter (e.g., 'last_quarter', 'ytd', '2024-Q4').")] = None,
        category: Annotated[Optional[str], Field(description="Product category filter.")] = None,
    ) -> str:
        """Search and analyze sales data by various criteria."""
        await asyncio.sleep(0.1)  # Minimal delay for async consistency
        
        # Use the service to get sales reports
        sales_reports = await self.financial_service.get_sales_reports(date_range, category)
        
        # Filter based on query
        filtered_reports = []
        for report in sales_reports:
            if (query.lower() in report.product_name.lower() or 
                query.lower() in report.category.lower() or 
                query.lower() in report.region.lower() or
                query.lower() in report.sales_rep.lower()):
                filtered_reports.append(report)
        
        # Convert to serializable format
        results = []
        for report in filtered_reports:
            results.append({
                "product_id": report.product_id,
                "product_name": report.product_name,
                "category": report.category,
                "sales_amount": float(report.sales_amount),
                "units_sold": report.units_sold,
                "revenue": float(report.revenue),
                "profit_margin": report.profit_margin,
                "sales_date": report.sales_date.isoformat(),
                "region": report.region,
                "sales_rep": report.sales_rep
            })
        
        return json.dumps({
            "query": query,
            "date_range": date_range,
            "category": category,
            "total_results": len(results),
            "total_revenue": sum(float(r["revenue"]) for r in results),
            "avg_profit_margin": sum(r["profit_margin"] for r in results) / len(results) if results else 0,
            "results": results
        })
    
    async def analyze_revenue_trends(
        self,
        period: Annotated[str, Field(description="Analysis period: 'monthly', 'quarterly', 'yearly'.")],
        metrics: Annotated[List[str], Field(description="Metrics to analyze: 'growth_rate', 'recurring_revenue', 'forecasting'.")] = None,
    ) -> str:
        """Analyze revenue trends and patterns over specified periods."""
        if metrics is None:
            metrics = ["growth_rate", "recurring_revenue"]
        
        # Get revenue data from service
        revenue_data = await self.financial_service.get_revenue_data(period)
        
        # Analyze trends
        trend_analysis = await self.financial_service.analyze_trends("sales", "6months")
        
        # Convert to serializable format
        revenue_results = []
        for data in revenue_data:
            revenue_results.append({
                "period": data.period,
                "revenue": float(data.revenue),
                "recurring_revenue": float(data.recurring_revenue),
                "one_time_revenue": float(data.one_time_revenue),
                "growth_rate": data.growth_rate,
                "revenue_source": data.revenue_source,
                "department": data.department
            })
        
        return json.dumps({
            "analysis_period": period,
            "metrics_analyzed": metrics,
            "total_periods": len(revenue_results),
            "revenue_data": revenue_results,
            "trend_analysis": trend_analysis,
            "summary": {
                "total_revenue": sum(float(r["revenue"]) for r in revenue_results),
                "avg_growth_rate": sum(r["growth_rate"] for r in revenue_results) / len(revenue_results) if revenue_results else 0,
                "recurring_percentage": sum(float(r["recurring_revenue"]) for r in revenue_results) / sum(float(r["revenue"]) for r in revenue_results) * 100 if revenue_results else 0
            }
        })
    
    async def calculate_business_metrics(
        self,
        metric_names: Annotated[List[str], Field(description="Metrics to calculate: 'customer_acquisition_cost', 'revenue_growth_rate', 'profit_margin', 'customer_lifetime_value'.")],
        period: Annotated[str, Field(description="Calculation period (e.g., 'Q4 2024', '2024', 'last_quarter').")] = "current",
    ) -> str:
        """Calculate key business metrics and KPIs."""
        # Get calculated metrics from service
        financial_metrics = await self.financial_service.calculate_financial_metrics(metric_names, period)
        
        # Convert to serializable format
        metrics_results = []
        for metric in financial_metrics:
            metrics_results.append({
                "metric_name": metric.metric_name,
                "value": float(metric.value),
                "period": metric.period,
                "calculation_method": metric.calculation_method,
                "benchmark": float(metric.benchmark) if metric.benchmark else None,
                "trend": metric.trend.value,
                "metadata": metric.metadata
            })
        
        return json.dumps({
            "requested_metrics": metric_names,
            "calculation_period": period,
            "total_metrics": len(metrics_results),
            "metrics": metrics_results,
            "performance_summary": {
                "metrics_above_benchmark": sum(1 for m in metrics_results if m["benchmark"] and m["value"] > m["benchmark"]),
                "positive_trends": sum(1 for m in metrics_results if m["trend"] in ["up", "stable"]),
                "areas_for_improvement": [m["metric_name"] for m in metrics_results if m["trend"] == "down"]
            }
        })
    
    async def get_top_performing_products(
        self,
        time_period: Annotated[str, Field(description="Time period for analysis: 'last_quarter', 'ytd', 'last_month', 'custom_range'.")],
        metric: Annotated[str, Field(description="Performance metric: 'revenue', 'profit_margin', 'units_sold', 'growth_rate'.")] = "revenue",
        limit: Annotated[int, Field(description="Number of top products to return.")] = 10,
    ) -> str:
        """Get top-performing products based on specified metrics and time periods."""
        # Get sales data from service
        sales_reports = await self.financial_service.get_sales_reports(time_period)
        
        # Sort by the specified metric
        if metric == "revenue":
            sorted_products = sorted(sales_reports, key=lambda x: x.revenue, reverse=True)
        elif metric == "profit_margin":
            sorted_products = sorted(sales_reports, key=lambda x: x.profit_margin, reverse=True)
        elif metric == "units_sold":
            sorted_products = sorted(sales_reports, key=lambda x: x.units_sold, reverse=True)
        else:  # Default to revenue
            sorted_products = sorted(sales_reports, key=lambda x: x.revenue, reverse=True)
        
        # Take top N products
        top_products = sorted_products[:limit]
        
        # Convert to serializable format
        results = []
        for i, product in enumerate(top_products, 1):
            results.append({
                "rank": i,
                "product_id": product.product_id,
                "product_name": product.product_name,
                "category": product.category,
                "revenue": float(product.revenue),
                "profit_margin": product.profit_margin,
                "units_sold": product.units_sold,
                "region": product.region,
                "sales_rep": product.sales_rep,
                "performance_score": float(product.revenue) * product.profit_margin  # Custom performance score
            })
        
        return json.dumps({
            "time_period": time_period,
            "performance_metric": metric,
            "limit": limit,
            "total_products_analyzed": len(sales_reports),
            "top_products": results,
            "summary": {
                "top_revenue": float(results[0]["revenue"]) if results else 0,
                "avg_profit_margin": sum(r["profit_margin"] for r in results) / len(results) if results else 0,
                "total_units": sum(r["units_sold"] for r in results),
                "dominant_category": max(set(r["category"] for r in results), key=lambda x: sum(1 for r in results if r["category"] == x)) if results else None
            }
        })
    
    async def analyze_customer_metrics(
        self,
        analysis_type: Annotated[str, Field(description="Type of customer analysis: 'acquisition_cost', 'lifetime_value', 'churn_analysis', 'segmentation'.")],
        segment: Annotated[Optional[str], Field(description="Customer segment filter: 'enterprise', 'smb', 'individual'.")] = None,
        include_predictions: Annotated[bool, Field(description="Whether to include predictive analytics.")] = False,
    ) -> str:
        """Analyze customer metrics including acquisition cost, lifetime value, and churn analysis."""
        # Get customer data from service
        customer_data = await self.financial_service.get_customer_data(segment)
        
        # Perform analysis based on type
        if analysis_type == "acquisition_cost":
            # Calculate CAC metrics
            metrics = await self.financial_service.calculate_financial_metrics(["customer_acquisition_cost"])
            avg_cac = float(metrics[0].value) if metrics else 0
            
            analysis_result = {
                "metric": "Customer Acquisition Cost",
                "average_cac": avg_cac,
                "total_customers": len(customer_data),
                "by_segment": {}
            }
            
            # CAC by segment
            segments = set(c.segment for c in customer_data)
            for seg in segments:
                segment_customers = [c for c in customer_data if c.segment == seg]
                avg_cost = sum(float(c.acquisition_cost) for c in segment_customers) / len(segment_customers)
                analysis_result["by_segment"][seg] = {
                    "customer_count": len(segment_customers),
                    "average_cac": avg_cost
                }
        
        elif analysis_type == "lifetime_value":
            analysis_result = {
                "metric": "Customer Lifetime Value",
                "total_customers": len(customer_data),
                "average_ltv": sum(float(c.lifetime_value) for c in customer_data) / len(customer_data),
                "ltv_distribution": {
                    "high_value": len([c for c in customer_data if c.lifetime_value > 50000]),
                    "medium_value": len([c for c in customer_data if 10000 <= c.lifetime_value <= 50000]),
                    "low_value": len([c for c in customer_data if c.lifetime_value < 10000])
                }
            }
        
        elif analysis_type == "churn_analysis":
            churn_distribution = {}
            for risk_level in ["low", "medium", "high"]:
                churn_distribution[risk_level] = len([c for c in customer_data if c.churn_risk == risk_level])
            
            analysis_result = {
                "metric": "Churn Risk Analysis",
                "total_customers": len(customer_data),
                "churn_distribution": churn_distribution,
                "at_risk_customers": churn_distribution.get("high", 0) + churn_distribution.get("medium", 0)
            }
        
        else:  # segmentation
            segments = {}
            for customer in customer_data:
                if customer.segment not in segments:
                    segments[customer.segment] = {
                        "count": 0,
                        "total_ltv": 0,
                        "avg_order_value": 0,
                        "total_orders": 0
                    }
                segments[customer.segment]["count"] += 1
                segments[customer.segment]["total_ltv"] += float(customer.lifetime_value)
                segments[customer.segment]["avg_order_value"] += float(customer.avg_order_value)
                segments[customer.segment]["total_orders"] += customer.total_orders
            
            # Calculate averages
            for segment_data in segments.values():
                if segment_data["count"] > 0:
                    segment_data["avg_ltv"] = segment_data["total_ltv"] / segment_data["count"]
                    segment_data["avg_order_value"] = segment_data["avg_order_value"] / segment_data["count"]
                    segment_data["avg_orders_per_customer"] = segment_data["total_orders"] / segment_data["count"]
            
            analysis_result = {
                "metric": "Customer Segmentation Analysis",
                "total_customers": len(customer_data),
                "segments": segments
            }
        
        return json.dumps({
            "analysis_type": analysis_type,
            "segment_filter": segment,
            "include_predictions": include_predictions,
            "analysis_result": analysis_result,
            "recommendations": [
                "Focus retention efforts on high-risk customers",
                "Optimize acquisition channels for best ROI",
                "Develop targeted campaigns for each segment"
            ]
        })
    
    async def get_financial_summary(
        self,
        report_type: Annotated[str, Field(description="Summary type: 'executive', 'operational', 'investor', 'department'.")],
        period: Annotated[str, Field(description="Reporting period: 'current_quarter', 'ytd', 'last_year'.")] = "current_quarter",
        include_forecasts: Annotated[bool, Field(description="Whether to include forecasting data.")] = True,
    ) -> str:
        """Generate comprehensive financial summaries and dashboards."""
        # Get data from multiple service methods
        sales_reports = await self.financial_service.get_sales_reports()
        revenue_data = await self.financial_service.get_revenue_data()
        performance_data = await self.financial_service.get_employee_performance()
        customer_data = await self.financial_service.get_customer_data()
        
        # Calculate key metrics
        key_metrics = await self.financial_service.calculate_financial_metrics([
            "customer_acquisition_cost", "revenue_growth_rate", "profit_margin"
        ])
        
        # Create summary based on report type
        if report_type == "executive":
            summary = {
                "report_title": f"Executive Summary - {period}",
                "key_highlights": {
                    "total_revenue": sum(float(r.revenue) for r in revenue_data),
                    "revenue_growth": next((float(m.value) for m in key_metrics if "growth" in m.metric_name.lower()), 0),
                    "profit_margin": next((float(m.value) for m in key_metrics if "margin" in m.metric_name.lower()), 0),
                    "customer_acquisition_cost": next((float(m.value) for m in key_metrics if "acquisition" in m.metric_name.lower()), 0),
                    "total_customers": len(customer_data),
                    "top_performing_product": max(sales_reports, key=lambda x: x.revenue).product_name if sales_reports else "N/A"
                },
                "performance_indicators": [
                    {"name": "Revenue Growth", "value": "12.5%", "status": "positive", "trend": "up"},
                    {"name": "Customer Satisfaction", "value": "4.7/5", "status": "excellent", "trend": "stable"},
                    {"name": "Market Share", "value": "18%", "status": "good", "trend": "up"},
                    {"name": "Employee Retention", "value": "94%", "status": "excellent", "trend": "stable"}
                ]
            }
        
        elif report_type == "operational":
            summary = {
                "report_title": f"Operational Summary - {period}",
                "operational_metrics": {
                    "total_sales_transactions": len(sales_reports),
                    "avg_deal_size": sum(float(r.revenue) for r in sales_reports) / len(sales_reports) if sales_reports else 0,
                    "sales_team_performance": sum(p.performance_score for p in performance_data if p.sales_target) / len([p for p in performance_data if p.sales_target]) if performance_data else 0,
                    "pipeline_health": "Strong",
                    "conversion_rates": {
                        "lead_to_opportunity": "24%",
                        "opportunity_to_close": "67%"
                    }
                }
            }
        
        else:  # Default comprehensive summary
            summary = {
                "report_title": f"Financial Summary - {period}",
                "overview": {
                    "total_revenue": sum(float(r.revenue) for r in revenue_data),
                    "total_sales": len(sales_reports),
                    "active_customers": len(customer_data),
                    "team_size": len(performance_data)
                }
            }
        
        # Add forecasts if requested
        if include_forecasts:
            trend_data = await self.financial_service.analyze_trends("sales")
            summary["forecasts"] = {
                "next_quarter_revenue": trend_data.get("forecast", "Data not available"),
                "growth_projection": "15% expected growth",
                "market_conditions": "Favorable"
            }
        
        return json.dumps({
            "report_type": report_type,
            "period": period,
            "generated_at": "2024-09-22T14:30:00Z",
            "include_forecasts": include_forecasts,
            "summary": summary,
            "data_sources": ["sales_reports", "revenue_data", "customer_data", "performance_metrics"],
            "recommendations": [
                "Continue focus on high-margin products",
                "Invest in customer retention programs",
                "Expand successful regional strategies",
                "Monitor market trends for opportunities"
            ]
        })