using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.A2A;
using Microsoft.Agents.AI.Hosting;
using Agents.Dotnet.Models.UI;
using Agents.Dotnet.Services;
using Agents.Dotnet.Tools;
using System.Text.Json;
using A2A.AspNetCore;
using A2A;
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient(connectionName: "foundry",
    configureSettings: settings =>
        {
            settings.TokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = builder.Configuration.GetValue<string>("TenantId") });
            settings.EnableSensitiveTelemetryData = true;
        })
    .AddChatClient("gpt-4.1");

builder.Services.AddSingleton<DocumentService>();
builder.Services.AddSingleton<DocumentTools>();
builder.AddKeyedAzureCosmosContainer("conversations", configureClientOptions: (option) => { option.Serializer = new CosmosSystemTextJsonSerializer(); });
builder.Services.AddSingleton<ICosmosRepository, SampleCosmosRepository>();

// Register MCP client as a singleton
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    var mcpServerUrl = Environment.GetEnvironmentVariable("services__mcpserver__https__0") 
           ?? Environment.GetEnvironmentVariable("services__mcpserver__http__0")!;
    
    // Append the MCP endpoint path
    var mcpEndpoint = new Uri(new Uri(mcpServerUrl), "/mcp");
    
    logger.LogInformation("Connecting to MCP server at {McpEndpoint}", mcpEndpoint);
    
    var transport = new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = mcpEndpoint
    });
    
    return McpClient.CreateAsync(transport).GetAwaiter().GetResult();
});

builder
    .AddAIAgent("document-management-agent", (sp, key) =>
    {
        var instrumentedChatClient = sp.GetRequiredService<IChatClient>();
        var documentTools = sp.GetRequiredService<DocumentTools>().GetFunctions();
        var mcpClient = sp.GetRequiredService<McpClient>();
        
        // Retrieve the list of tools available on the MCP server
        var mcpTools = mcpClient.ListToolsAsync().GetAwaiter().GetResult();
        
        ChatClientAgentOptions options = new ()
        {
            Id = Guid.NewGuid().ToString(), //the agent identifier
            Name = key, //the agent name
            Instructions = @"You are a specialized Document Management and Policy Compliance Assistant. Your role is to help users find company policies, procedures, compliance requirements, and manage document-related tasks.

Your capabilities include:
- Searching and retrieving company documents, policies, and procedures
- Extracting and analyzing content from PDF, Word, and PowerPoint documents
- Looking up specific policies by category (HR, Safety, Finance, IT, etc.)
- Checking compliance requirements for various operations and spending levels
- Providing document version information and management
- Indexing and organizing documents from various sources

When users ask about policies, always provide specific requirements, proce  dures, and any exceptions that apply. For compliance questions, clearly explain what approvals are needed and any additional requirements. Be helpful and thorough in your responses while maintaining accuracy based on the available document data.

Sample areas you can help with:
- Remote work policies and procedures
- Safety requirements and procedures
- Purchase authorization and approval processes
- HR policies and employee handbook information
- Compliance rules and requirements
- Document version management
- Contract and legal document information",
            Description = "A friendly AI assistant", //the agent description
            ChatOptions = new ChatOptions
            {
                Tools = [.. documentTools,
                    ..mcpTools.Cast<AITool>()],
            },
            
            ChatMessageStoreFactory = ctx =>
            {
                // Create a new chat message store for this agent that stores the messages in a cosmos store.
                return new CustomMessageStore(
                sp.GetRequiredService<ICosmosRepository>(),
                ctx.SerializedState, //<-lo stato della conversazione serializzato qui ci devo fare arrivare l'id
                ctx.JsonSerializerOptions);
            }
        };

        var agent = new ChatClientAgent(instrumentedChatClient, options);

        return agent;
    });

var app = builder.Build();

app.MapPost("/agent/chat/stream", async ([FromKeyedServices("document-management-agent")] AIAgent agent,
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

        //let's create a CustomConversationState
        CustomConversationState conversationState = new() { Id = conversationId };
        //let's serialize the conversationstate to pass it to our CustomMessageStore
        var serializedState = conversationState.Serialize();
        //resume the thread with our CustomMessageStore
        AgentThread resumedThread = agent.DeserializeThread(serializedState);

        var chatMessage = new ChatMessage(ChatRole.User, message.Content);

        //before invoking the agent MAF automatically calls the GetMessagesAsync of our CustomMessageStore
        await foreach (var update in agent.RunStreamingAsync(chatMessage, resumedThread))
        {
            await response.WriteAsync($"{JsonSerializer.Serialize(new AIChatCompletionDelta(new AIChatMessageDelta() { Content = update.Text }))}\r\n");
            await response.Body.FlushAsync();
        }

        //when the agent has finished MAF automatically calls the AddMessagesAsync of our CustomMessageStore

    }

    return;
});

app.MapDefaultEndpoints();

var agent = app.Services.GetKeyedService<AIAgent>("document-management-agent");

var a2aAgent = new A2AHostAgent(agent!, new AgentCard
{
    Name = agent!.Name!,
    Description = "Document Management and Policy Compliance Assistant",
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
            Name = "Document Search",
            Description = "Search and retrieve company documents, policies, and procedures",
            Examples = ["Find the remote work policy", "What is the purchase approval process?", "Show me the latest HR policies"]
        },
        new AgentSkill
        {
            Name = "Content Extraction",
            Description = "Extract and analyze content from PDF, Word, and PowerPoint documents",
            Examples = ["Extract text from this PDF", "Analyze the content of this Word document"]
        },
        new AgentSkill
        {
            Name = "Compliance Lookup",
            Description = "Check compliance requirements for various operations and spending levels",
            Examples = ["What are the compliance requirements for a $10,000 purchase?", "List the safety compliance rules"]
        },
        new AgentSkill
        {
            Name = "Document Management",
            Description = "Provide document version information and management",
            Examples = ["What is the latest version of the employee handbook?", "Manage document versions"]
        }
    ]

});

app.MapA2A(a2aAgent.TaskManager!, "/");

app.Run();