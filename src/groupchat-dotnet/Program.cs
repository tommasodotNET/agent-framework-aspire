using A2A;
using Azure.Identity;
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

var app = builder.Build();

app.MapGet("/", async () =>
{
    var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__dotnetagent__https__0")!),
        Timeout = TimeSpan.FromSeconds(60)
    };
    var agentCardResolver = new A2ACardResolver(httpClient.BaseAddress!, httpClient);

    var agent = await agentCardResolver.GetAIAgentAsync();

    var response = await agent.RunAsync("What is our remote work policy?");

    Console.WriteLine(response.Text);
    return Results.Ok();
});

app.MapDefaultEndpoints();

app.Run();
