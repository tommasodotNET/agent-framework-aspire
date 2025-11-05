using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Mvc;
using GroupChat.Dotnet.Models.UI;
using GroupChat.Dotnet.Services;
using System.Text.Json;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Cosmos DB for conversation storage
builder.AddKeyedAzureCosmosContainer("conversations", configureClientOptions: (option) => { option.Serializer = new CosmosSystemTextJsonSerializer(); });
builder.Services.AddSingleton<ICosmosRepository, SampleCosmosRepository>();

// Register Cosmos Thread Store services
builder.Services.AddSingleton<ICosmosThreadRepository, CosmosThreadRepository>();
builder.Services.AddSingleton<CosmosAgentThreadStore>();

var dotnetHttpClient = new HttpClient()
{
    BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__dotnetagent__https__0") ?? Environment.GetEnvironmentVariable("services__dotnetagent__http__0")!),
    Timeout = TimeSpan.FromSeconds(60)
};
var dotnetAgentCardResolver = new A2ACardResolver(dotnetHttpClient.BaseAddress!, dotnetHttpClient, agentCardPath: "/agenta2a/v1/card");

var documentManagementAgent = dotnetAgentCardResolver.GetAIAgentAsync().Result;
builder.AddAIAgent("document-management-agent", (sp, key) => documentManagementAgent);

var pythonHttpClient = new HttpClient()
{
    BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__pythonagent__https__0") ?? Environment.GetEnvironmentVariable("services__pythonagent__http__0")!),
    Timeout = TimeSpan.FromSeconds(60)
};
var pythonAgentCardResolver = new A2ACardResolver(pythonHttpClient.BaseAddress!, pythonHttpClient, agentCardPath: "/agenta2a/v1/card");

var financialAnalysisAgent = pythonAgentCardResolver.GetAIAgentAsync().Result;

builder.AddAIAgent("financial-analysis-agent", (sp, key) => financialAnalysisAgent);

builder.AddAIAgent("group-chat", (sp, key) =>
{
    var documentAgent = sp.GetRequiredKeyedService<AIAgent>("document-management-agent");
    var financialAgent = sp.GetRequiredKeyedService<AIAgent>("financial-analysis-agent");

    Workflow workflow =
        AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 2
                })
            .AddParticipants(documentAgent, financialAgent)
            .Build();

    return workflow.AsAgent(name: key);
}).WithThreadStore((sp, key) => sp.GetRequiredService<CosmosAgentThreadStore>());

var app = builder.Build();

app.MapGet("/test-dotnet-a2a-agent", async ([FromKeyedServices("document-management-agent")] AIAgent documentAgent) =>
{
    var documentResponse = await documentAgent.RunAsync("What is our remote work policy?");
    Console.WriteLine($"Document Agent: {documentResponse.Text}");

    return Results.Ok(new { DocumentAgent = documentResponse.Text });
});

app.MapGet("/test-python-a2a-agent", async ([FromKeyedServices("financial-analysis-agent")] AIAgent documentAgent) =>
{
    var documentResponse = await documentAgent.RunAsync("What were our top-performing products last quarter?");
    Console.WriteLine($"Financial Agent: {documentResponse.Text}");

    return Results.Ok(new { FinancialAgent = documentResponse.Text });
});

app.MapGet("/agent/chat", async ([FromKeyedServices("group-chat")] AIAgent groupChatAgent) =>
{
    var prompt = "According to our procurement policy, what vendors are we required to use for office supplies, and what has been our spending pattern with those vendors over the past 6 months?";
    var groupChatResponse = await groupChatAgent.RunAsync(prompt);
    return Results.Ok(groupChatResponse);
});

app.MapPost("/agent/chat/stream", async ([FromKeyedServices("group-chat")] AIAgent agent,
    [FromKeyedServices("group-chat")] AgentThreadStore threadStore,
    [FromBody] AIChatRequest request,
    [FromServices] ILogger<Program> logger,
    HttpResponse response) =>
{  
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

        var thread = await threadStore.GetThreadAsync(agent, conversationId);

        var chatMessage = new ChatMessage(ChatRole.User, message.Content);

        await foreach (var update in agent.RunStreamingAsync(chatMessage, thread))
        {
            await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = update.Text }))}\r\n");
            await response.Body.FlushAsync();
        }

        await threadStore.SaveThreadAsync(agent, conversationId, thread);
    }

    return;
});

app.MapDefaultEndpoints();

app.Run();
