using A2A;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

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
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__dotnetagent__https__0")!),
        Timeout = TimeSpan.FromSeconds(60)
    };
    var agentCardResolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient);

    return agentCardResolver.GetAIAgentAsync().GetAwaiter().GetResult();
});

var app = builder.Build();

app.MapGet("/", async ([FromKeyedServices("document-management-agent")] AIAgent agent) =>
{
    var response = await agent.RunAsync("What is our remote work policy?");

    Console.WriteLine(response.Text);
    return Results.Ok();
});

app.MapDefaultEndpoints();

app.Run();
