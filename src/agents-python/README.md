# Python Agent

Python-based agent for financial analysis, reporting, and business metrics.

## Role

This agent specializes in:
- Financial analysis, reporting, and business metrics
- CSV/Excel file parsing and analysis  
- Database queries (SQLite, PostgreSQL local connections)
- Financial calculations and trend analysis
- Report generation (PDF, charts)
- Data validation and cleaning
- Statistical analysis libraries (pandas, numpy)

## Data Sources (Mocked)

- Sales reports, revenue data, expense tracking
- Employee performance metrics, payroll data  
- Inventory levels, supply chain data
- Customer demographics and behavior analytics

## Sample Questions

- "What were our top-performing products last quarter?"
- "Show me the sales trend for the past 6 months"  
- "Calculate our customer acquisition cost"

## Development

This is a `uv` application. To run:

```bash
uv sync --prerelease=allow
uv run agents_python.main:main
```