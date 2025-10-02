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
    var agentUrls = builder.Configuration.GetValue<string>("AgentUrls")!;
    var dotnetAgent = new HostClientAgent();
    var dotnetAgentUrl = agentUrls.Split(';').FirstOrDefault()!;
    await dotnetAgent.InitializeAgentAsync(chatClient, dotnetAgentUrl);
    AgentThread thread = dotnetAgent.Agent!.GetNewThread();
    var response = await dotnetAgent.Agent!.RunAsync("What is the remote work policy?", thread);
    Console.WriteLine(response);
    var pythonAgentUrl = agentUrls.Split(';').LastOrDefault()!;
    var pythonAgent = new HostClientAgent();
    await pythonAgent.InitializeAgentAsync(chatClient, pythonAgentUrl);
    thread = pythonAgent.Agent!.GetNewThread();
    response = await pythonAgent.Agent!.RunAsync("What were our top-performing products last quarter?", thread);
    Console.WriteLine(response);
    return Results.Ok();
});

app.MapDefaultEndpoints();

app.Run();
