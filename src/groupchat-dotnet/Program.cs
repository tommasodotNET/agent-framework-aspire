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

builder.AddAIAgent("document-management-agent", (sp, key) =>
{
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__dotnetagent__https__0") ?? Environment.GetEnvironmentVariable("services__dotnetagent__http__0")!),
        Timeout = TimeSpan.FromSeconds(60)
    };
    var agentCardResolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient, agentCardPath: "/agenta2a/v1/card");

    return agentCardResolver.GetAIAgentAsync().GetAwaiter().GetResult();
});

builder.AddAIAgent("financial-analysis-agent", (sp, key) =>
{
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__pythonagent__https__0") ?? Environment.GetEnvironmentVariable("services__pythonagent__http__0")!),
        Timeout = TimeSpan.FromSeconds(60)
    };
    var agentCardResolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient);

    return agentCardResolver.GetAIAgentAsync().GetAwaiter().GetResult();
});

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

    return workflow.AsAgentAsync(name: key).GetAwaiter().GetResult();
});

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

        // we can't yet assign a custom message store to a workflow-as-agent: https://github.com/microsoft/agent-framework/issues/1573
        // //let's create a CustomConversationState
        // CustomConversationState conversationState = new() { Id = conversationId };
        // //let's serialize the conversationstate to pass it to our CustomMessageStore
        // var serializedState = conversationState.Serialize();
        // //resume the thread with our CustomMessageStore
        // AgentThread resumedThread = agent.DeserializeThread(serializedState);

        var chatMessage = new ChatMessage(ChatRole.User, message.Content);
        
        try
        {
            var result = await agent.RunAsync(chatMessage);
            
            // Stream the response back token by token
            var responseText = result.Text ?? "";
            var words = responseText.Split(' ');
            
            foreach (var word in words)
            {
                await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = word + " " }))}\r\n");
                await response.Body.FlushAsync();
                await Task.Delay(50); // Small delay for streaming effect
            }
            
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running group chat agent");
            await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = "I encountered an error processing your request." }))}\r\n");
            await response.Body.FlushAsync();
        }

        //when the agent has finished MAF automatically calls the AddMessagesAsync of our CustomMessageStore

    }

    return;
});

app.MapDefaultEndpoints();

app.Run();
