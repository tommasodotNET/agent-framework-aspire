# Agent Framework Aspire

This repository contains a sample implementation of an agent framework using Aspire, demonstrating how to build Retrieval-Augmented Generation (RAG) applications with both .NET and Python agents.

## Run the sample

> This sample requires .Net 10 Preview SDK and Python 3.11+ installed on your machine.

To allow Aspire to create or reference existing resources on Azure (e.g. Foundry), you need to configure Azure settings in the [appsettings.json](./src/apphost/appsettings.json) file:

```json
"Azure": {
  "SubscriptionId": "<YOUR-SUBSCRIPTION-ID>",
  "AllowResourceGroupCreation": true,
  "ResourceGroup": "<YOUR-RESOURCE-GROUP>",
  "Location": "<YOUR-LOCATION>",
  "CredentialSource": "AzureCli"
}
```

Use [aspire cli](https://learn.microsoft.com/en-us/dotnet/aspire/cli/install) to run the sample:

```bash
cd src/agents-python
uv sync --prerelease=allow
uv run agents_python.main:main
cd ..
aspire run
```

To ease the debug experience, you can use the [Aspire extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=microsoft-aspire.aspire-vscode#:~:text=The%20Aspire%20VS%20Code%20extension,directly%20from%20Visual%20Studio%20Code.).

> Note: To support python telemetry, I'm setting Aspire to run with the http profile. Therefore, the endpoints for the services are http only. You can easily switch to https by changing profile order in the [aspire launchsettings.json](./src/apphost/Properties/launchSettings.json) file. That will require to update the [AppHost.cs](./src/apphost/AppHost.cs) to change the group chat endpoints to https.

### Aspire single-file AppHost

This sample can be use with single-file AppHost. Change the aspire configuration in the [.aspire/settings.json](./.aspire/settings.json) file to point to the [apphost.cs](./src/apphost-singlefile/apphost.cs) file:

```json
{
  "features": {
    "singlefileAppHostEnabled": "true",
    "minimumSdkCheckEnabled": "false"
  },
  "appHostPath": "../src/apphost-singlefile/apphost.cs"
}
```

Then run the sample as usual with `aspire run`.

## Folder Structure

```
src/
├── frontend/                   # React Frontend application
├── agents-python/              # Python API for RAG
│   ├── services/               # Services for Python Agent
│   └── tools/                  # Tools for Python Agent
├── agents-dotnet/              # .NET Agent API
│   ├── services/               # Services for .NET Agent
│   └── tools/                  # Tools for .NET Agent
├── groupchat-dotnet/           # .NET Group Chat API
├── apphost/                    # Aspire App Host
├── apphost-singlefile/         # Aspire App Host
└── servicedefaults/            # Default configurations for services
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

This question will invoke the remote mcp tool":
- "What's our remote work policy?"

While these questions will invoke local tools:
- "Find the latest safety procedures for warehouse operations"
- "What are the approval requirements for purchases over $5000?"

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

- "What were our top-performing products last quarter?"
- "Show me the sales trend for the past 6 months"
- "Calculate our customer acquisition cost"