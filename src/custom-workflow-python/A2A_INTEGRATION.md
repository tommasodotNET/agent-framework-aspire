# A2A Agent Integration Documentation

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

## A2A Agent Configuration

### Environment Variables

Set the following environment variables to configure A2A agent endpoints:

```bash
# Financial analysis agent endpoint (Python agent)
FINANCIAL_AGENT_HOST=http://services__pythonagent__http__0

# Policy/Document lookup agent endpoint (.NET agent)  
POLICY_AGENT_HOST=http://services__dotnetagent__http__0

# Note: Compliance agent is mocked - always returns "compliant" status
# COMPLIANCE_AGENT_HOST not needed
```

### Agent Card Requirements

Each A2A agent must expose an Agent Card at `/.well-known/agent.json` with the following structure:

```json
{
  "name": "Financial Analysis Agent",
  "description": "Analyzes financial data and business metrics",
  "version": "1.0.0",
  "capabilities": [
    "financial-analysis",
    "revenue-analysis", 
    "business-metrics"
  ],
  "endpoints": {
    "chat": "/v1/chat"
  }
}
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

## API Usage

### Start the Service
```bash
uv run start
```

### Analyze Financial Compliance
```bash
curl -X POST http://localhost:8000/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Analyze Q4 sales performance for enterprise products with policy compliance",
    "period": "Q4 2024"
  }'
```

### Health Check
```bash
curl http://localhost:8000/health
```

## Development Notes

### Testing
- Use `uv run python test_workflow.py` to test data models and routing logic
- Mock A2A endpoints for local development
- Integration tests require actual A2A-compliant agents

### Extending the Workflow
1. Add new data models for additional analysis types
2. Create new executor functions for transformation logic
3. Add edges to the workflow graph with appropriate conditions
4. Configure additional A2A agent endpoints

### Error Handling
- HTTP client timeouts are set to 60 seconds
- Fallback responses for A2A agent failures
- Structured error logging for debugging

## Production Considerations

1. **Security**: Implement proper authentication for A2A endpoints
2. **Monitoring**: Add telemetry and health checks for A2A agents
3. **Resilience**: Implement retry logic and circuit breakers
4. **Caching**: Cache agent cards and frequently accessed policies
5. **Load Balancing**: Use multiple instances of A2A agents for scalability