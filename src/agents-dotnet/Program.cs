using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.A2A.AspNetCore;
using Microsoft.Agents.AI.Hosting;
using Agents.Dotnet.Models.UI;
using Agents.Dotnet.Services;
using Agents.Dotnet.Tools;
using System.Text.Json;
using A2A.AspNetCore;
using A2A;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient(connectionName: "foundry",
    configureSettings: settings =>
        {
            settings.TokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = builder.Configuration.GetValue<string>("TenantId") });
            settings.EnableSensitiveTelemetryData = true;
        })
    .AddChatClient("gpt-4.1");

builder.Services.AddSingleton<DocumentService>();
builder.Services.AddSingleton<DocumentTools>();
builder.AddKeyedAzureCosmosContainer("conversations", configureClientOptions: (option) => { option.Serializer = new CosmosSystemTextJsonSerializer(); });
builder.Services.AddSingleton<CosmosConversationRepository>();

builder
    .AddAIAgent("document-management-agent", (sp, key) =>
    {
        var instrumentedChatClient = sp.GetRequiredService<IChatClient>();

        var documentTools = sp.GetRequiredService<DocumentTools>().GetFunctions();

        var agent = new ChatClientAgent(instrumentedChatClient,
                name: key,
                instructions: @"You are a specialized Document Management and Policy Compliance Assistant. Your role is to help users find company policies, procedures, compliance requirements, and manage document-related tasks.

Your capabilities include:
- Searching and retrieving company documents, policies, and procedures
- Extracting and analyzing content from PDF, Word, and PowerPoint documents
- Looking up specific policies by category (HR, Safety, Finance, IT, etc.)
- Checking compliance requirements for various operations and spending levels
- Providing document version information and management
- Indexing and organizing documents from various sources

When users ask about policies, always provide specific requirements, proce  dures, and any exceptions that apply. For compliance questions, clearly explain what approvals are needed and any additional requirements. Be helpful and thorough in your responses while maintaining accuracy based on the available document data.

Sample areas you can help with:
- Remote work policies and procedures
- Safety requirements and procedures
- Purchase authorization and approval processes
- HR policies and employee handbook information
- Compliance rules and requirements
- Document version management
- Contract and legal document information",
                tools: [.. documentTools,
                AIFunctionFactory.Create(DocumentProcessingTools.ExtractPdfText),
                AIFunctionFactory.Create(DocumentProcessingTools.ParseOfficeDocument),
                AIFunctionFactory.Create(DocumentProcessingTools.IndexDocuments)]);

        return agent;
    });

var app = builder.Build();

app.MapPost("/agent/chat/stream", async ([FromKeyedServices("document-management-agent")] AIAgent agent,
    [FromBody] AIChatRequest request,
    [FromServices] CosmosConversationRepository? conversationRepository,
    [FromServices] ILogger<Program> logger,
    HttpResponse response) =>
{
    var thread = await (agent as ChatClientAgent)!.GetThreadAsync(request.SessionState, conversationRepository, logger);
    var conversationId = request.SessionState ?? Guid.NewGuid().ToString();

    if (request.Messages.Count == 0)
    {
        AIChatCompletionDelta delta = new(new AIChatMessageDelta() { Content = $"Hi, I'm {agent.Name}" })
        {
            SessionState = conversationId
        };

        await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");
        await response.Body.FlushAsync();
    }
    else
    {
        var message = request.Messages.LastOrDefault();
        var chatMessage = new ChatMessage(ChatRole.User, message.Content);

        await foreach (var update in agent.RunStreamingAsync(chatMessage, thread))
        {
            await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = update.Text }))}\r\n");
            await response.Body.FlushAsync();
        }
    }

    await (agent as ChatClientAgent)!.SaveThreadAsync(thread, conversationId, conversationRepository, logger);
    return;
});

app.MapDefaultEndpoints();

var agent = app.Services.GetKeyedService<AIAgent>("document-management-agent");

app.MapA2A("document-management-agent", "/agenta2a", new AgentCard
{
    Name = agent!.Name!,
    Url = "http://localhost:5196/agenta2a",
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