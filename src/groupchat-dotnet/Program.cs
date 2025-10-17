using A2A;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
    var agentCardResolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient, agentCardPath: "/agenta2a/v1/card");

    return agentCardResolver.GetAIAgentAsync().GetAwaiter().GetResult();
});

builder.AddAIAgent("group-chat", (sp, key) =>
{
    var documentAgent = sp.GetRequiredKeyedService<AIAgent>("document-management-agent");
    var financialAgent = sp.GetRequiredKeyedService<AIAgent>("financial-analysis-agent");

    Workflow workflow =
        AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new AgentWorkflowBuilder.RoundRobinGroupChatManager(agents)
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
    AgentRunResponse response = await groupChatAgent.RunAsync(prompt);
    return Results.Ok(response);
});

app.MapDefaultEndpoints();

app.Run();
