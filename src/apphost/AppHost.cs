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
    .WithEnvironment("TenantId", tenantId);

#pragma warning disable ASPIREHOSTINGPYTHON001
var pythonAgent = builder.AddUvApp("pythonagent", "../agents-python", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", $"https://{existingFoundryName}.openai.azure.com/")
    .WithEnvironment("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME", "gpt-4.1")
    .WithEnvironment("OTEL_PYTHON_CONFIGURATOR", "configurator")
    .WithOtlpExporter()
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE", "true");

var dotnetGroupChat = builder.AddProject("dotnetgroupchat", "../groupchat-dotnet/GroupChat.Dotnet.csproj")
    .WithHttpHealthCheck("/health")
    .WithReference(foundry).WaitFor(foundry)
    .WithReference(dotnetAgent).WaitFor(dotnetAgent)
    .WithReference(pythonAgent).WaitFor(pythonAgent)
    .WithEnvironment("TenantId", tenantId)
    .WithUrls((e) =>
    {
        e.Urls.Clear();
        e.Urls.Add(new() { Url = "/agent/chat", DisplayText = "💬Group Chat", Endpoint = e.GetEndpoint("http") });
        e.Urls.Add(new() { Url = "/test-dotnet-a2a-agent", DisplayText = "💬.NET A2A Agent", Endpoint = e.GetEndpoint("http") });
        e.Urls.Add(new() { Url = "/test-python-a2a-agent", DisplayText = "💬Python A2A Agent", Endpoint = e.GetEndpoint("http") });
    });

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(dotnetAgent).WaitFor(dotnetAgent)
    .WithReference(pythonAgent).WaitFor(pythonAgent)
    .WithHttpEndpoint(env: "PORT")
    .WithUrls((e) =>
    {
        e.Urls.Clear();
        e.Urls.Add(new() { Url = "/", DisplayText = "💬Chat", Endpoint = e.GetEndpoint("http") });
    });

builder.Build().Run();
