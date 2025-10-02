#:sdk Aspire.AppHost.Sdk@9.6.0-preview.1.25476.6
#:package Aspire.Hosting.AppHost@9.6.0-preview.1.25471.2
#:package Aspire.Hosting.Azure.AIFoundry@13.0.0-preview.1.25502.4
#:package Aspire.Hosting.Azure.CosmosDB@9.6.0-preview.1.25471.2
#:package Aspire.Hosting.Azure.Search@9.6.0-preview.1.25471.2
#:package Aspire.Hosting.NodeJs@9.6.0-preview.1.25471.2
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.8.0-beta.376
#:package CommunityToolkit.Aspire.Hosting.Python.Extensions@9.8.0-beta.376

var builder = DistributedApplication.CreateBuilder(args);

var tenantId = builder.AddParameter("TenantId")
    .WithDescription("The Azure tenant ID for authentication.");
var existingFoundryName = builder.AddParameter("existingFoundryName")
    .WithDescription("The name of the existing Azure Foundry resource.");
var existingFoundryResourceGroup = builder.AddParameter("existingFoundryResourceGroup")
    .WithDescription("The resource group of the existing Azure Foundry resource.");

var foundry = builder.AddAzureAIFoundry("foundry")
    .AsExisting(existingFoundryName, existingFoundryResourceGroup);

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

var dotnetAgent = builder.AddProject("dotnetagent", "../agents-dotnet/Agents.Dotnet.csproj")
    .WithHttpHealthCheck("/health")
    .WithReference(foundry)
    .WithReference(conversations).WaitFor(conversations)
    .WithEnvironment("TenantId", tenantId)
    .WaitFor(foundry);

#pragma warning disable ASPIREHOSTINGPYTHON001
var pythonAgent = builder.AddUvApp("pythonagent", "../agents-python", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", $"https://{existingFoundryName}.openai.azure.com/")
    .WithEnvironment("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME", "gpt-4.1")
    .WithOtlpExporter()
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE", "true");

var dotnetGroupChat = builder.AddProject("dotnetgroupchat", "../groupchat-dotnet/GroupChat.Dotnet.csproj")
    .WithHttpHealthCheck("/health")
    .WithReference(foundry)
    .WithEnvironment("TenantId", tenantId)
    .WithReference(dotnetAgent)
    .WithEnvironment("dotnetagenturl", $"{dotnetAgent.GetEndpoint("https")}")
    .WaitFor(foundry)
    .WithUrls((e) =>
    {
        e.Urls.Clear();
        e.Urls.Add(new() { Url = "/test-a2a-agent", DisplayText = "💬A2A Agent", Endpoint = e.GetEndpoint("https") });
        e.Urls.Add(new() { Url = "/agent/chat", DisplayText = "💬Group Chat", Endpoint = e.GetEndpoint("https") });
    });

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(dotnetAgent)
    .WithReference(pythonAgent)
    .WaitFor(dotnetAgent)
    .WaitFor(pythonAgent)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();