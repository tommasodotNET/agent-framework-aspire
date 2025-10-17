using A2A;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient(connectionName: "foundry",
    configureSettings: settings =>
        {
            settings.TokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = builder.Configuration.GetValue<string>("TenantId") });
            settings.EnableSensitiveTelemetryData = true;
        })
    .AddChatClient("gpt-4.1");

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

app.MapGet("/agent/chat", async (
    [FromKeyedServices("document-management-agent")] AIAgent documentAgent,
    [FromKeyedServices("financial-analysis-agent")] AIAgent financialAgent) =>
{
    Workflow workflow =
        AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents =>
                new AgentWorkflowBuilder.RoundRobinGroupChatManager(agents)
                {
                    MaximumIterationCount = 2
                })
            .AddParticipants(documentAgent, financialAgent)
            .Build();

    AIAgent workflowAgent = await workflow.AsAgentAsync();

    var prompt = "According to our procurement policy, what vendors are we required to use for office supplies, and what has been our spending pattern with those vendors over the past 6 months?";
    AgentRunResponse response = await workflowAgent.RunAsync(prompt);
    return Results.Ok(response);
});

app.MapDefaultEndpoints();

app.Run();
