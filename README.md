# Agent Framework Aspire

This repository contains a sample implementation of an agent framework using Aspire, demonstrating how to build Retrieval-Augmented Generation (RAG) applications with both .NET and Python agents.

## Run the sample

Use [aspire cli](https://learn.microsoft.com/en-us/dotnet/aspire/cli/install) to run the sample:

```bash
cd src/agents-python
uv sync --prerelease=allow
uv run agents_python.main:main
cd ..
aspire run
```

## Folder Structure

```
src/
├── frontend/                   # React Frontend application
├── agents-python/              # Python API for RAG
│   ├── services/               # Services for Python Agent
│   └── tools/                  # Tools for Python Agent
├── Agents.Dotnet/              # .NET Agent API
│   ├── Services/               # Services for .NET Agent
│   └── Tools/                  # Tools for .NET Agent
├── AppHost/                    # Aspire App Host
└── ServiceDefaults/            # Default configurations for services
```

## .NET Agent

Is a .NET-based agent hosted in a minimal API.

Role: Document management, policy lookup, and compliance Local Tools (methods returning mocked data):

- PDF text extraction and search
- Word/PowerPoint document parsing
- SharePoint/file system document indexing
- Policy and procedure lookups
- Compliance rule checking
- Document version management

Data Sources:

- Company policies, HR handbook, compliance documents
- Project documentation, technical specifications
- Legal contracts, vendor agreements
- Training materials, standard operating procedures

Sample Questions:

"What's our remote work policy?"
"Find the latest safety procedures for warehouse operations"
"What are the approval requirements for purchases over $5000?"

## Python Agent

Is a Python-based agent hosted in a uv application. Please note uv is supported by Aspire in the [Community Toolkit](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/hosting-python-extensions?tabs=dotnet-cli%2Cuv).

Role: Financial analysis, reporting, and business metrics Local Tools (methods returning mocked data):

- CSV/Excel file parsing and analysis
- Database queries (SQLite, PostgreSQL local connections)
- Financial calculations and trend analysis
- Report generation (PDF, charts)
- Data validation and cleaning
- Statistical analysis libraries (pandas, numpy)

Data Sources:

- Sales reports, revenue data, expense tracking
- Employee performance metrics, payroll data
- Inventory levels, supply chain data
- Customer demographics and behavior analytics

Sample Questions:

"What were our top-performing products last quarter?"
"Show me the sales trend for the past 6 months"
"Calculate our customer acquisition cost"