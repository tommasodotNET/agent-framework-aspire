using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Agents.Dotnet.Models.UI;
using Agents.Dotnet.Services;
using Agents.Dotnet.Tools;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .AddAzureOpenAIClient("azureOpenAI", configureSettings: settings =>
    {
        settings.Credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = builder.Configuration.GetValue<string>("TenantId") });
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

app.Run();