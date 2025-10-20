# Custom Workflow Description

## Overview

This custom workflow implementation demonstrates how to integrate A2A (Agent-to-Agent) protocol compliant agents in a financial compliance workflow using the Microsoft Agent Framework.

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│                 │    │                  │    │                 │
│ Financial Agent │───▶│ Workflow Engine  │───▶│ Document Agent  │
│ (Python A2A)    │    │  (Conditional    │    │ (.NET A2A)      │
│                 │    │   Routing)       │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │                 │
                       │ Compliance      │
                       │ Agent           │
                       │ (Mock - Always  │
                       │ Returns "OK")   │
                       └─────────────────┘
```

## Workflow Flow

### 1. Financial Analysis Phase
- **Input**: User query about financial performance
- **Processing**: Python A2A Agent analyzes sales data, revenue trends
- **Output**: `FinancialAnalysisResult` with structured financial metrics

### 2. Conditional Routing
- **Logic**: If `needs_policy_review == true`, route to document agent
- **Decision**: Based on risk areas identified in financial analysis

### 3a. Document/Policy Review Path
- **Input**: Financial analysis results transformed to policy query
- **Processing**: .NET A2A Agent searches for relevant compliance policies
- **Output**: `PolicyLookupResult` with policy requirements

### 3b. Compliance Report Generation
- **Input**: Combined financial + policy results
- **Processing**: A2A Compliance Agent generates comprehensive report
- **Output**: `ComplianceReport` with recommendations

### 3c. Alternative Direct Path
- **Condition**: If no policy review needed
- **Output**: Direct financial analysis summary

## Data Models

### FinancialAnalysisResult
```python
{
    "total_revenue": 2450000.00,
    "top_products": ["Enterprise Suite Pro", "Analytics Dashboard"],
    "revenue_growth_rate": 15.3,
    "profit_margin": 28.7,
    "needs_policy_review": true,
    "risk_areas": ["international sales", "revenue recognition"],
    "query_context": "Original user query",
    "analysis_period": "Q4 2024"
}
```

### PolicyLookupResult
```python
{
    "relevant_policies": ["Revenue Recognition Policy", "Sales Policy"],
    "compliance_requirements": ["SOX compliance", "GDPR"],
    "risk_mitigation_steps": ["Quarterly audits", "Documentation"],
    "policy_categories": ["Financial", "Legal"],
    "last_updated": "2024-Q3",
    "financial_context": "Context from financial analysis"
}
```

### ComplianceReport
```python
{
    "executive_summary": "High-level findings summary",
    "financial_highlights": {"Revenue": "$2.45M", "Growth": "15.3%"},
    "compliance_status": "needs-attention",
    "policy_alignment": ["How results align with policies"],
    "action_items": ["Recommended actions"],
    "priority_areas": ["High-priority areas"]
}
```