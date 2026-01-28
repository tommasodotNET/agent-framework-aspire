using A2A;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.A2A;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using SharedServices;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Cosmos DB for conversation storage
builder.AddKeyedAzureCosmosContainer("conversations", configureClientOptions: (option) => { option.Serializer = new CosmosSystemTextJsonSerializer(); });

// Register Cosmos Thread Store services
builder.Services.AddSingleton<ICosmosThreadRepository, CosmosThreadRepository>();
builder.Services.AddSingleton<CosmosAgentSessionStore>();

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
var pythonAgentCardResolver = new A2ACardResolver(pythonHttpClient.BaseAddress!, pythonHttpClient);

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
}).WithSessionStore((sp, key) => sp.GetRequiredService<CosmosAgentSessionStore>());

var app = builder.Build();

// Enable CORS
app.UseCors();

app.MapA2A("group-chat", "/agenta2a", new AgentCard
{
    Name = "group-chat",
    Url = app.Configuration["ASPNETCORE_URLS"]?.Split(';')[0] + "/agenta2a" ?? "http://localhost:5198/agenta2a",
    Description = "Multi-agent group chat orchestrating document and financial analysis agents",
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
            Name = "Multi-Agent Orchestration",
            Description = "Coordinate multiple specialized agents to answer complex queries",
            Examples = [
                "What vendors are we required to use for office supplies and what has been our spending pattern with those vendors?",
                "Analyze our procurement policy and recent financial data together"
            ]
        }
    ]
});

app.MapDefaultEndpoints();

app.Run();
