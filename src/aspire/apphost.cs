#:sdk Aspire.AppHost.Sdk@13.0.0-preview.1.25529.4
#:package Aspire.Hosting.AppHost@13.0.0-preview.1.25529.4
#:package Aspire.Hosting.Azure.AIFoundry@13.0.0-preview.1.25529.4
#:package Aspire.Hosting.Azure.CosmosDB@13.0.0-preview.1.25529.4
#:package Aspire.Hosting.Azure.Search@13.0.0-preview.1.25529.4
#:package Aspire.Hosting.NodeJs@13.0.0-preview.1.25529.4
#:package Aspire.Hosting.Python@13.0.0-preview.1.25529.4
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.8.0-beta.376

var builder = DistributedApplication.CreateBuilder(args);

var tenantId = builder.AddParameter("TenantId")
    .WithDescription("The Azure tenant ID for authentication.");
var existingFoundryName = builder.AddParameter("existingFoundryName")
    .WithDescription("The name of the existing Azure Foundry resource.");
var existingFoundryResourceGroup = builder.AddParameter("existingFoundryResourceGroup")
    .WithDescription("The resource group of the existing Azure Foundry resource.");

var foundry = builder.AddAzureAIFoundry("foundry")
    .AsExisting(existingFoundryName, existingFoundryResourceGroup);

tenantId.WithParentRelationship(foundry);
existingFoundryName.WithParentRelationship(foundry);
existingFoundryResourceGroup.WithParentRelationship(foundry);

#pragma warning disable ASPIRECOSMOSDB001
var cosmos = builder.AddAzureCosmosDB("cosmos-db")
    .RunAsPreviewEmulator(
        emulator =>
        {
            emulator.WithDataExplorer();
            emulator.WithLifetime(ContainerLifetime.Persistent);
        });
var db = cosmos.AddCosmosDatabase("db");
var conversations = db.AddContainer("conversations", "/conversationId");

var mcpServer = builder.AddProject("mcpserver", "../mcp-server-dotnet/McpServer.Dotnet.csproj")
    .WithHttpHealthCheck("/health");

var dotnetAgent = builder.AddProject("dotnetagent", "../agents-dotnet/Agents.Dotnet.csproj")
    .WithHttpHealthCheck("/health")
    .WithReference(foundry).WaitFor(foundry)
    .WithReference(conversations).WaitFor(conversations)
    .WithReference(mcpServer).WaitFor(mcpServer)
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .WithUrls((e) =>
    {
        e.Urls.Add(new() { Url = "/agenta2a/v1/card", DisplayText = "🤖A2A Card", Endpoint = e.GetEndpoint("https") });
    });

#pragma warning disable ASPIREHOSTINGPYTHON001
var pythonAgent = builder.AddPythonModule("pythonagent", "../agents-python", "agents_python.main")
    .WithUvEnvironment()
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", $"https://{existingFoundryName}.openai.azure.com/")
    .WithEnvironment("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME", "gpt-4.1")
    .WithEnvironment("OTEL_PYTHON_CONFIGURATOR", "configurator")
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .WithUrls((e) =>
    {
        e.Urls.Add(new() { Url = "/agenta2a/v1/card", DisplayText = "🤖A2A Card", Endpoint = e.GetEndpoint("http") });
    });

var pythonCustomWorkflow = builder.AddPythonModule("pythonCustomWorkflow", "../custom-workflow-python", "custom_workflow_python.main")
    .WithUvEnvironment()
    .WithHttpEndpoint(env: "PORT")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", $"https://{existingFoundryName}.openai.azure.com/")
    .WithEnvironment("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME", "gpt-4.1")
    .WithEnvironment("OTEL_PYTHON_CONFIGURATOR", "configurator")
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE", "true")
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .WithReference(dotnetAgent).WaitFor(dotnetAgent)
    .WithReference(pythonAgent).WaitFor(pythonAgent)
    .WithUrls((e) =>
    {
        e.Urls.Add(new() { Url = "/analyze", DisplayText = "🤖Custom Workflow", Endpoint = e.GetEndpoint("http") });
    });

var dotnetGroupChat = builder.AddProject("dotnetgroupchat", "../groupchat-dotnet/GroupChat.Dotnet.csproj")
    .WithHttpHealthCheck("/health")
    .WithReference(foundry).WaitFor(foundry)
    .WithReference(conversations).WaitFor(conversations)
    .WithReference(dotnetAgent).WaitFor(dotnetAgent)
    .WithReference(pythonAgent).WaitFor(pythonAgent)
    .WithEnvironment("AZURE_TENANT_ID", tenantId)
    .WithUrls((e) =>
    {
        e.Urls.Add(new() { Url = "/agent/chat", DisplayText = "🤖Group Chat", Endpoint = e.GetEndpoint("https") });
    });

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(dotnetAgent).WaitFor(dotnetAgent)
    .WithReference(pythonAgent).WaitFor(pythonAgent)
    .WithReference(dotnetGroupChat).WaitFor(dotnetGroupChat)
    .WithHttpEndpoint(env: "PORT")
    .WithUrls((e) =>
    {
        e.Urls.Clear();
        e.Urls.Add(new() { Url = "/", DisplayText = "💬Chat", Endpoint = e.GetEndpoint("http") });
    });

builder.Build().Run();
