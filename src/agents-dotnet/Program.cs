using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.A2A;
using Microsoft.Agents.AI.Hosting;
using Agents.Dotnet.Services;
using Agents.Dotnet.Tools;
using A2A;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using SharedServices;
using System.Text.Json;
using Azure.AI.Inference;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient(connectionName: "foundry",
    configureSettings: settings =>
        {
            settings.TokenCredential = new DefaultAzureCredential();
            settings.EnableSensitiveTelemetryData = true;
        })
    .AddChatClient("gpt-4.1");

builder.Services.AddSingleton<DocumentService>();
builder.Services.AddSingleton<DocumentTools>();
builder.AddKeyedAzureCosmosContainer("conversations", configureClientOptions: (option) => option.Serializer = new CosmosSystemTextJsonSerializer());

// Configure CORS for A2A frontend access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Cosmos Thread Store services
builder.Services.AddSingleton<ICosmosThreadRepository, CosmosThreadRepository>();
builder.Services.AddSingleton<CosmosAgentSessionStore>();

var mcpServerUrl = Environment.GetEnvironmentVariable("services__mcpserver__https__0") 
       ?? Environment.GetEnvironmentVariable("services__mcpserver__http__0")!;

// Append the MCP endpoint path
var mcpEndpoint = new Uri(new Uri(mcpServerUrl), "/mcp");

var transport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = mcpEndpoint
});

var mcpClient = await McpClient.CreateAsync(transport);

// Retrieve the list of tools available on the MCP server
var mcpTools = await mcpClient.ListToolsAsync();

// Register MCP client as a singleton
builder.Services.AddSingleton(mcpClient);

builder.AddAIAgent("document-management-agent", (sp, key) =>
{
    var instrumentedChatClient = sp.GetRequiredService<IChatClient>();
    var documentTools = sp.GetRequiredService<DocumentTools>().GetFunctions();
    var chatHistoryContainer = sp.GetRequiredKeyedService<Container>("conversations");

    var agent = instrumentedChatClient.AsAIAgent(new ChatClientAgentOptions
    {
        Name = key,
        ChatOptions = new() {
            Instructions = @"You are a specialized Document Management and Policy Compliance Assistant. Your role is to help users find company policies, procedures, compliance requirements, and manage document-related tasks.

            Your capabilities include:
            - Searching and retrieving company documents, policies, and procedures
            - Extracting and analyzing content from PDF, Word, and PowerPoint documents
            - Looking up specific policies by category (HR, Safety, Finance, IT, etc.)
            - Checking compliance requirements for various operations and spending levels
            - Providing document version information and management
            - Indexing and organizing documents from various sources

            When users ask about policies, always provide specific requirements, procedures, and any exceptions that apply. For compliance questions, clearly explain what approvals are needed and any additional requirements. Be helpful and thorough in your responses while maintaining accuracy based on the available document data.

            Sample areas you can help with:
            - Remote work policies and procedures
            - Safety requirements and procedures
            - Purchase authorization and approval processes
            - HR policies and employee handbook information
            - Compliance rules and requirements
            - Document version management
            - Contract and legal document information",
            Tools = [.. documentTools, ..mcpTools.Cast<AITool>()]
        } ,
        ChatHistoryProviderFactory = (ctx, ct) => new ValueTask<ChatHistoryProvider>(
            // Create a new chat history provider for this agent that stores the messages in Cosmos DB.
            // Each session must get its own copy of the CosmosChatHistoryProvider, since the provider
            // also contains the conversation ID that the session is stored under.
            ctx.SerializedState.ValueKind is JsonValueKind.Object
                ? CosmosChatHistoryProvider.CreateFromSerializedState(
                    chatHistoryContainer,
                    ctx.SerializedState,
                    ctx.JsonSerializerOptions)
                : new CosmosChatHistoryProvider(
                    chatHistoryContainer,
                    Guid.NewGuid().ToString("N")))
    });

    // var agent = instrumentedChatClient.AsAIAgent(
    //     instructions: @"You are a specialized Document Management and Policy Compliance Assistant. Your role is to help users find company policies, procedures, compliance requirements, and manage document-related tasks.

    //         Your capabilities include:
    //         - Searching and retrieving company documents, policies, and procedures
    //         - Extracting and analyzing content from PDF, Word, and PowerPoint documents
    //         - Looking up specific policies by category (HR, Safety, Finance, IT, etc.)
    //         - Checking compliance requirements for various operations and spending levels
    //         - Providing document version information and management
    //         - Indexing and organizing documents from various sources

    //         When users ask about policies, always provide specific requirements, procedures, and any exceptions that apply. For compliance questions, clearly explain what approvals are needed and any additional requirements. Be helpful and thorough in your responses while maintaining accuracy based on the available document data.

    //         Sample areas you can help with:
    //         - Remote work policies and procedures
    //         - Safety requirements and procedures
    //         - Purchase authorization and approval processes
    //         - HR policies and employee handbook information
    //         - Compliance rules and requirements
    //         - Document version management
    //         - Contract and legal document information",
    //     description: "A friendly AI assistant",
    //     name: key,
    //     tools: [.. documentTools,
    //             ..mcpTools.Cast<AITool>()]
    // );

    return agent;
}).WithSessionStore((sp, key) => sp.GetRequiredService<CosmosAgentSessionStore>());

var app = builder.Build();

// Enable CORS
app.UseCors();

app.MapDefaultEndpoints();

// var agent = app.Services.GetRequiredKeyedService<AIAgent>("document-management-agent");

// app.MapAGUI("/agent/chat/stream", agent);

app.MapA2A("document-management-agent", "/agenta2a", new AgentCard
{
    Name = "document-management-agent",
    Url = app.Configuration["ASPNETCORE_URLS"]?.Split(';')[0] + "/agenta2a" ?? "http://localhost:5196/agenta2a",
    Description = "Document Management and Policy Compliance Assistant",
    Version = "1.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Capabilities = new AgentCapabilities
    {
        Streaming = true,
        PushNotifications = false
    },
    Skills = [
        new AgentSkill
        {
            Name = "Document Search",
            Description = "Search and retrieve company documents, policies, and procedures",
            Examples = ["Find the remote work policy", "What is the purchase approval process?", "Show me the latest HR policies"]
        },
        new AgentSkill
        {
            Name = "Content Extraction",
            Description = "Extract and analyze content from PDF, Word, and PowerPoint documents",
            Examples = ["Extract text from this PDF", "Analyze the content of this Word document"]
        },
        new AgentSkill
        {
            Name = "Compliance Lookup",
            Description = "Check compliance requirements for various operations and spending levels",
            Examples = ["What are the compliance requirements for a $10,000 purchase?", "List the safety compliance rules"]
        },
        new AgentSkill
        {
            Name = "Document Management",
            Description = "Provide document version information and management",
            Examples = ["What is the latest version of the employee handbook?", "Manage document versions"]
        }
    ]

});

app.Run();