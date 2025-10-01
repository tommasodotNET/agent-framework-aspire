var builder = DistributedApplication.CreateBuilder(args);

var tenantId = builder.AddParameter("TenantId")
    .WithDescription("The Azure tenant ID for authentication.");
var existingOpenAIName = builder.AddParameter("existingOpenAIName")
    .WithDescription("The name of the existing Azure OpenAI resource.");
var existingOpenAIResourceGroup = builder.AddParameter("existingOpenAIResourceGroup")
    .WithDescription("The resource group of the existing Azure OpenAI resource.");

var azureOpenAI = builder.AddAzureOpenAI("azureOpenAI");

// If you want to use an existing Azure OpenAI resource, uncomment the following line
azureOpenAI.AsExisting(existingOpenAIName, existingOpenAIResourceGroup);

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

// var dotnetAgent = builder.AddProject("dotnetagent", "../Agents.Dotnet/Agents.Dotnet.csproj")

var dotnetAgent = builder.AddProject<Projects.Agents_Dotnet>("dotnetagent")
    .WithHttpHealthCheck("/health")
    .WithReference(azureOpenAI)
    .WithReference(conversations).WaitFor(conversations)
    .WithEnvironment("TenantId", tenantId)
    .WaitFor(azureOpenAI);

#pragma warning disable ASPIREHOSTINGPYTHON001
var pythonAgent = builder.AddUvApp("pythonagent", "../agents-python", "start")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("AZURE_OPENAI_ENDPOINT", $"https://{existingOpenAIName}.openai.azure.com/")
    .WithEnvironment("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME", "gpt-4.1")
    .WithOtlpExporter()
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE", "true");

var dotnetGroupChat = builder.AddProject<Projects.GroupChat_Dotnet>("dotnetgroupchat")
    .WithHttpHealthCheck("/health")
    .WithReference(azureOpenAI)
    .WithEnvironment("TenantId", tenantId)
    .WithEnvironment("AgentUrls", $"{dotnetAgent.GetEndpoint("https")}")
    .WaitFor(azureOpenAI);

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithNpmPackageInstallation()
    .WithReference(dotnetAgent)
    .WithReference(pythonAgent)
    .WaitFor(dotnetAgent)
    .WaitFor(pythonAgent)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
