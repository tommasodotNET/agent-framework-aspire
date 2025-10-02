using A2A;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using GroupChat.Dotnet.Services;
using GroupChat.Dotnet.Tools;
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

// Register financial services
builder.Services.AddSingleton<FinancialService>();
builder.Services.AddSingleton<FinancialTools>();

builder.AddAIAgent("document-management-agent", (sp, key) =>
{
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__dotnetagent__https__0")!),
        Timeout = TimeSpan.FromSeconds(60)
    };
    var agentCardResolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient);

    return agentCardResolver.GetAIAgentAsync().GetAwaiter().GetResult();
});

builder.AddAIAgent("financial-analysis-agent", (sp, key) =>
{
    var instrumentedChatClient = sp.GetRequiredService<IChatClient>();
    var financialTools = sp.GetRequiredService<FinancialTools>();

    var agent = new ChatClientAgent(instrumentedChatClient,
        name: key,
        instructions: @"You are a specialized Financial Analysis and Business Intelligence Assistant. Your role is to help users analyze financial data, track business metrics, and provide insights for strategic decision-making.

Your capabilities include:
- Analyzing sales performance and revenue trends
- Calculating key business metrics and KPIs (CAC, LTV, profit margins, growth rates)
- Tracking budget performance and identifying variances
- Identifying top-performing products and market segments
- Providing financial forecasting and trend analysis
- Generating business intelligence reports and insights

When users ask about financial performance, always provide specific metrics, trends, and actionable insights. For budget questions, clearly explain variances and their implications. Be thorough in your analysis while presenting information in a clear, business-friendly manner.

Sample areas you can help with:
- Revenue growth analysis and forecasting
- Sales performance by product, region, or time period
- Budget vs actual spending analysis
- Customer acquisition costs and lifetime value calculations
- Profit margin analysis and optimization opportunities
- Top performer identification and benchmarking
- Financial KPI tracking and reporting",
        tools: [.. financialTools.GetFunctions()]);

    return agent;
});

var app = builder.Build();

app.MapGet("/test-a2a-agent", async ([FromKeyedServices("document-management-agent")] AIAgent documentAgent) =>
{
    var documentResponse = await documentAgent.RunAsync("What is our remote work policy?");
    Console.WriteLine($"Document Agent: {documentResponse.Text}");


    return Results.Ok(new { DocumentAgent = documentResponse.Text });
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

    var prompt = "Gather all relevant documents and policies to help evaluate this new vendor partnership";
    AgentRunResponse response = await workflowAgent.RunAsync(prompt);
    return Results.Ok(response);
});

app.MapDefaultEndpoints();

app.Run();
