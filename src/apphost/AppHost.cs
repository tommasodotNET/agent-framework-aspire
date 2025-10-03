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
    .WaitFor(foundry)
    .WithUrls((e) =>
    {
        e.Urls.Clear();
        e.Urls.Add(new() { Url = "/test-a2a-agent", DisplayText = "💬A2A Agent", Endpoint = e.GetEndpoint("http") });
        e.Urls.Add(new() { Url = "/agent/chat", DisplayText = "💬Group Chat", Endpoint = e.GetEndpoint("http") });
    });

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(dotnetAgent)
    .WithReference(pythonAgent)
    .WaitFor(dotnetAgent)
    .WaitFor(pythonAgent)
    .WithHttpEndpoint(env: "PORT")
    .WithUrls((e) =>
    {
        e.Urls.Clear();
        e.Urls.Add(new() { Url = "/", DisplayText = "💬Chat", Endpoint = e.GetEndpoint("http") });
    });
builder.Build().Run();
