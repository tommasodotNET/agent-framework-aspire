# Custom Workflow Python

A custom workflow implementation for financial analysis with policy compliance checking using Microsoft Agent Framework and A2A (Agent-to-Agent) protocol compliant agents.

## Features

- **A2A Agent Integration**: Uses Agent-to-Agent protocol for seamless agent communication
- **Financial Analysis**: A2A agent analyzes sales performance, revenue trends, and business metrics
- **Policy Compliance**: A2A agent searches and retrieves relevant company policies
- **Conditional Workflow**: Intelligent routing based on financial analysis risk assessment
- **Compliance Reporting**: Comprehensive reports combining financial and policy insights
- **REST API**: FastAPI-based service with structured request/response models

## Architecture

The workflow demonstrates a sophisticated A2A agent orchestration:

1. **Financial Agent** (Python A2A) → Analyzes financial data and identifies risk areas
2. **Conditional Router** → Routes based on `needs_policy_review` flag
3. **Document Agent** (.NET A2A) → Searches compliance policies if needed
4. **Compliance Agent** (Mock) → Always returns positive compliance status
5. **Direct Output** → Simple financial summary if no policy review needed

## A2A Configuration

Set environment variables for A2A agent endpoints:

```bash
FINANCIAL_AGENT_HOST=http://services__pythonagent__http__0
POLICY_AGENT_HOST=http://services__dotnetagent__http__0
# Note: Compliance agent is mocked and always returns "compliant" status
```

Each A2A agent must expose an Agent Card at `/.well-known/agent.json`.

## API Endpoints

- `POST /analyze` - Run financial analysis with policy compliance checking
- `GET /health` - Health check endpoint

## Usage

```bash
# Install dependencies
uv sync

# Run tests (no external dependencies)
uv run python test_workflow.py

# Run the application
uv run start
```

The API will be available at `http://localhost:8000`.

## Example Request

```bash
curl -X POST http://localhost:8000/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Analyze Q4 sales performance for enterprise software products with policy compliance check",
    "period": "Q4 2024"
  }'
```

## Documentation

- See [A2A_INTEGRATION.md](A2A_INTEGRATION.md) for detailed A2A integration documentation
- Run `uv run python test_workflow.py` to see workflow structure demonstration