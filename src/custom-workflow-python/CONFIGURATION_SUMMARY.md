# Configuration Updated

## ✅ A2A Agent Endpoints Configured

The workflow has been updated to use the correct service endpoints:

### Financial Agent (Python)
- **Endpoint**: `http://services__pythonagent__http__0`
- **Service**: Python-based financial analysis agent
- **Tools**: Handles financial analysis, sales data, revenue trends
- **A2A Path**: `/.well-known/agent.json`

### Document/Policy Agent (.NET)  
- **Endpoint**: `http://services__dotnetagent__http__0`
- **Service**: .NET-based document processing agent
- **Tools**: Handles policy lookup, document search, compliance requirements
- **A2A Path**: `/.well-known/agent.json`

### Compliance Agent (Mock)
- **Type**: Mock implementation
- **Behavior**: Always returns positive compliance status
- **Status**: "compliant" 
- **No external endpoint needed**

## Environment Variables

```bash
FINANCIAL_AGENT_HOST=http://services__pythonagent__http__0
POLICY_AGENT_HOST=http://services__dotnetagent__http__0
# COMPLIANCE_AGENT_HOST not needed (using mock)
```

## Sample Workflow Question

*"I need to analyze our Q4 sales performance for our enterprise software products, but I also need to ensure we're compliant with our sales commission and revenue recognition policies. Can you provide both the financial analysis and the relevant policy requirements?"*

### Expected Flow:
1. **Python Agent** → Analyzes Q4 financial performance
2. **Router** → Determines if policy review needed
3. **.NET Agent** → Searches relevant policies (if needed)
4. **Mock Compliance** → Always returns "compliant" status
5. **Output** → Combined financial analysis + policy compliance report

## Testing

Run the application:
```bash
cd custom-workflow-python
uv run start
```

Test the endpoint:
```bash
curl -X POST http://localhost:8000/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Analyze Q4 sales performance for enterprise products with policy compliance",
    "period": "Q4 2024"
  }'
```

The workflow is now configured to connect to the actual Python and .NET agent services!