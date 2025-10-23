# Agent Framework Aspire

This repository contains a sample implementation of the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework/) using [Aspire](https://github.com/dotnet/aspire/), demonstrating how to build Retrieval-Augmented Generation (RAG) applications with both .NET and Python agents.

## Features

- Building agent with .NET and Agent Framework
- Building agent with Python and Agent Framework
- Agent orchestration with .NET and Agent Framework
- Inter-agent communication (A2A) with .NET and Agent Framework
- Using MCP with .NET and Agent Framework
- Using function filtering with Agent Framework
- Test agents behaviour without using evaluation frameworks
- Using Aspire to host multi-agent applications


## Run the sample

> This sample requires .Net 10 Preview SDK and Python 3.11+ installed on your machine.

To allow Aspire to create or reference existing resources on Azure (e.g. Foundry), you need to configure Azure settings in the [appsettings.json](./src/apphost/appsettings.json) file:

```json
"Azure": {
  "SubscriptionId": "<YOUR-SUBSCRIPTION-ID>",
  "AllowResourceGroupCreation": false,
  "Location": "<YOUR-LOCATION>",
  "CredentialSource": "AzureCli"
}
```

Use [aspire cli](https://learn.microsoft.com/en-us/dotnet/aspire/cli/install) to run the sample.

Powershell:
```bash
Invoke-RestMethod https://aspire.dev/install.ps1 -OutFile aspire-install.ps1
./aspire-install.ps1 -q dev

aspire run
```

Bash:
```bash
curl -sSL https://aspire.dev/install.sh -o aspire-install.sh
./aspire-install.sh -q dev

aspire run
```

To ease the debug experience, you can use the [Aspire extension for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=microsoft-aspire.aspire-vscode#:~:text=The%20Aspire%20VS%20Code%20extension,directly%20from%20Visual%20Studio%20Code.). Otherwise, you can use the [C# Dev Kit for Visual Studio Code](https://learn.microsoft.com/it-it/visualstudio/subscriptions/vs-c-sharp-dev-kit).

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
├── agents-dotnet/              # dotnet Agent API
│   ├── services/               # Services for dotnet Agent
│   └── tools/                  # Tools for dotnet Agent
├── groupchat-dotnet/           # dotnet Group Chat API
├── mcp-server-dotnet/          # dotnet MCP Server
├── custom-workflow-python/     # Custom Python Workflow
├── apphost/                    # Aspire App Host
├── apphost-singlefile/         # Aspire App Host (Single File)
└── service-defaults/           # Default configurations for services
test/
└── agents-dotnet-tests/        # Test project for dotnet agent
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

Is a Python-based agent hosted in a uv-based uvicorn application.

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

## Dotnet Group Chat

Is a .NET-based group chat that creates a local agent with the same capabilities of the Python Agent and reference the .NET agent via A2A. It can be easily invoked with a hardcoded prompt for convenience from the Aspire dashboard. It can also be invoked via the frontend.

Sample Questions:
- According to our procurement policy, what vendors are we required to use for office supplies, and what has been our spending pattern with those vendors over the past 6 months?

## Python Custom Workflow 

This is a custom workflow written in python that implements a custom flow using both dotnet and python agent via A2A. It can be easily invoked with a hardcoded prompt for convenience from the Aspire dashboard. The workflow is described [here](./src/custom-workflow-python/custom-workflow-description.md)

## Test project

The assumption in the test project is that the agents will respond correctly if they invoke the correct tools with the correct parameters. I use function filters to gather the tools invocations and parameters and validate them against the expected ones. This way I can test the agents behaviour without using evaluation frameworks.