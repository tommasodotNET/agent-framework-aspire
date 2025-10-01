using Azure.Identity;
using GroupChat.Dotnet;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .AddAzureOpenAIClient("azureOpenAI", configureSettings: settings =>
    {
        settings.Credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = builder.Configuration.GetValue<string>("TenantId") });
        settings.EnableSensitiveTelemetryData = true;
    })
    .AddChatClient("gpt-4.1");

var app = builder.Build();

app.MapGet("/", async (IChatClient chatClient) =>
{
    var agentUrls = builder.Configuration.GetValue<string>("AgentUrls");
    var hostAgent = new HostClientAgent();
    await hostAgent.InitializeAgentAsync(chatClient, agentUrls!.Split(";"));
    AgentThread thread = hostAgent.Agent!.GetNewThread();
    var response = await hostAgent.Agent!.RunAsync("What is the remote work policy?", thread);
    Console.WriteLine(response);
    return Results.Ok();
});

app.MapDefaultEndpoints();

app.Run();
