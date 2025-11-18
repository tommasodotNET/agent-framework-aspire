# Microsoft Agent Framework - .NET Agent Development Guide

This guide provides comprehensive instructions for writing code for hosted agents using Microsoft Agent Framework (MAF) with .NET in this repository.

## Table of Contents

1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Dependencies](#dependencies)
4. [Agent Patterns](#agent-patterns)
5. [Tools and Functions](#tools-and-functions)
6. [Thread Store and Conversation Management](#thread-store-and-conversation-management)
7. [Complete Examples](#complete-examples)

## Overview

In this repository, we implement agents using Microsoft Agent Framework with .NET 10. Each agent can be exposed in multiple ways:
- **A2A (Agent-to-Agent)** communication for inter-agent scenarios
- **Custom API endpoints** for direct frontend integration
- **OpenAI Responses and Conversations** (OpenAI-compatible endpoints)

## Project Structure

A typical .NET agent project follows this structure:

```
src/your-agent-dotnet/
├── Program.cs                      # Main entry point
├── YourAgent.Dotnet.csproj        # Project file with dependencies
├── appsettings.json               # Configuration
├── Properties/
├── Models/                        # Data models
│   ├── Tools/                    # Tool-specific models
│   └── UI/                       # UI/API models
├── Services/                      # Business logic services
│   └── CosmosAgentThreadStore.cs # Thread persistence
├── Tools/                         # Agent tools/functions
│   └── YourTools.cs
└── Converters/                    # JSON converters if needed
```

## Dependencies

### Required NuGet Packages

Add these packages to your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Agent Framework packages -->
    <PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251113.1" />
    <PackageReference Include="Microsoft.Agents.AI.Abstractions" Version="1.0.0-preview.251113.1" />
    <PackageReference Include="Microsoft.Agents.AI.Hosting" Version="1.0.0-preview.251113.1" />
    <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-preview.251113.1" />
    
    <!-- A2A Support -->
    <PackageReference Include="Microsoft.Agents.AI.Hosting.A2A.AspNetCore" Version="1.0.0-preview.251113.1" />
    
    <!-- Optional: OpenAI-compatible endpoints -->
    <PackageReference Include="Microsoft.Agents.AI.Hosting.OpenAI" Version="1.0.0-preview.*" />
    
    <!-- Optional: DevUI for development -->
    <PackageReference Include="Microsoft.Agents.AI.DevUI" Version="1.0.0-preview.*" />
    
    <!-- Optional: Workflows for multi-agent scenarios -->
    <PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.0.0-preview.251113.1" />
    
    <!-- Azure and Aspire integrations -->
    <PackageReference Include="Aspire.Azure.AI.Inference" Version="13.0.0-preview.1.25560.3" />
    <PackageReference Include="Aspire.Microsoft.Azure.Cosmos" Version="13.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\service-defaults\ServiceDefaults.csproj" />
  </ItemGroup>
</Project>
```

### Key Namespace Imports

```csharp
using Microsoft.Agents.AI;                           // Core agent types
using Microsoft.Agents.AI.Hosting;                   // Hosting extensions
using Microsoft.Agents.AI.Hosting.A2A;               // A2A support
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;   // AGUI support (optional)
using Microsoft.Extensions.AI;                       // AI abstractions
using Azure.Identity;                                // Azure authentication
using A2A;                                          // A2A types
```

## Agent Patterns

### Pattern 1: A2A Agent with Custom API

This is the primary pattern used in the repository (see `src/agents-dotnet/Program.cs`).

#### Step 1: Configure Services and Chat Client

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure Azure chat client
builder.AddAzureChatCompletionsClient(connectionName: "foundry",
    configureSettings: settings =>
    {
        settings.TokenCredential = new DefaultAzureCredential();
        settings.EnableSensitiveTelemetryData = true;
    })
    .AddChatClient("gpt-4.1");

// Register your services
builder.Services.AddSingleton<YourService>();
builder.Services.AddSingleton<YourTools>();

// Register Cosmos for conversation storage
builder.AddKeyedAzureCosmosContainer("conversations", 
    configureClientOptions: (option) => option.Serializer = new CosmosSystemTextJsonSerializer());
builder.Services.AddSingleton<ICosmosThreadRepository, CosmosThreadRepository>();
builder.Services.AddSingleton<CosmosAgentThreadStore>();
```

#### Step 2: Register the Agent

```csharp
builder.AddAIAgent("your-agent-name", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var yourTools = sp.GetRequiredService<YourTools>().GetFunctions();

    var agent = chatClient.CreateAIAgent(
        instructions: @"You are a helpful assistant that...",
        description: "A friendly AI assistant",
        name: key,
        tools: yourTools
    );

    return agent;
}).WithThreadStore((sp, key) => sp.GetRequiredService<CosmosAgentThreadStore>());
```

#### Step 3: Add Custom API Endpoint

```csharp
var app = builder.Build();

app.MapPost("/agent/chat/stream", async (
    [FromKeyedServices("your-agent-name")] AIAgent agent,
    [FromKeyedServices("your-agent-name")] AgentThreadStore threadStore,
    [FromBody] AIChatRequest request,
    [FromServices] ILogger<Program> logger,
    HttpResponse response) =>
{
    var conversationId = request.SessionState ?? Guid.NewGuid().ToString();

    if (request.Messages.Count == 0)
    {
        // Initial greeting
        AIChatCompletionDelta delta = new(new AIChatMessageDelta() 
            { Content = $"Hi, I'm {agent.Name}" })
        {
            SessionState = conversationId
        };

        await response.WriteAsync($"{JsonSerializer.Serialize(delta)}\r\n");
        await response.Body.FlushAsync();
    }
    else
    {
        var message = request.Messages.LastOrDefault();
        var thread = await threadStore.GetThreadAsync(agent, conversationId);
        var chatMessage = new ChatMessage(ChatRole.User, message.Content);

        // Stream responses
        await foreach (var update in agent.RunStreamingAsync(chatMessage, thread))
        {
            await response.WriteAsync($"{JsonSerializer.Serialize(
                new AIChatCompletionDelta(new AIChatMessageDelta() 
                    { Content = update.Text }))}\r\n");
            await response.Body.FlushAsync();
        }

        await threadStore.SaveThreadAsync(agent, conversationId, thread);
    }

    return;
});
```

#### Step 4: Add A2A Endpoint

```csharp
app.MapA2A("your-agent-name", "/agenta2a", new AgentCard
{
    Name = "your-agent-name",
    Url = "http://localhost:5196/agenta2a",
    Description = "Your agent description",
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
            Name = "Skill Name",
            Description = "Skill description",
            Examples = ["Example 1", "Example 2"]
        }
    ]
});

app.MapDefaultEndpoints();
app.Run();
```

### Pattern 2: OpenAI-Compatible Endpoints

For OpenAI-compatible API endpoints (based on the reference template), add these endpoints:

#### Step 1: Register OpenAI Services

```csharp
// Register services for OpenAI responses and conversations
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();
```

#### Step 2: Map OpenAI Endpoints

```csharp
var app = builder.Build();

// Map endpoints for OpenAI responses and conversations
app.MapOpenAIResponses();
app.MapOpenAIConversations();
```

These endpoints provide OpenAI-compatible APIs:
- `/v1/chat/completions` - Chat completions endpoint
- `/v1/completions` - Text completions endpoint
- Streaming support via SSE (Server-Sent Events)

#### Step 3: (Optional) Add DevUI in Development

```csharp
if (builder.Environment.IsDevelopment())
{
    // Map DevUI endpoint for testing
    app.MapDevUI();
}
```

The DevUI will be available at `/devui` and provides a web interface for testing your agent.

### Pattern 3: Multi-Agent with A2A Communication

For orchestrating multiple agents (see `src/groupchat-dotnet/Program.cs`):

```csharp
// Connect to remote agents via A2A
var httpClient = new HttpClient()
{
    BaseAddress = new Uri(Environment.GetEnvironmentVariable("services__agent1__https__0")!),
    Timeout = TimeSpan.FromSeconds(60)
};
var cardResolver = new A2ACardResolver(
    httpClient.BaseAddress!, 
    httpClient, 
    agentCardPath: "/agenta2a/v1/card"
);

var remoteAgent = cardResolver.GetAIAgentAsync().Result;
builder.AddAIAgent("remote-agent", (sp, key) => remoteAgent);

// Create a workflow with multiple agents
builder.AddAIAgent("group-chat", (sp, key) =>
{
    var agent1 = sp.GetRequiredKeyedService<AIAgent>("agent1");
    var agent2 = sp.GetRequiredKeyedService<AIAgent>("agent2");

    Workflow workflow = AgentWorkflowBuilder
        .CreateGroupChatBuilderWith(agents => 
            new RoundRobinGroupChatManager(agents)
            {
                MaximumIterationCount = 2
            })
        .AddParticipants(agent1, agent2)
        .Build();

    return workflow.AsAgent(name: key);
}).WithThreadStore((sp, key) => sp.GetRequiredService<CosmosAgentThreadStore>());
```

### Pattern 4: Sequential Workflow

For sequential agent workflows (from the reference template):

```csharp
builder.AddAIAgent("writer", "You write short stories about the specified topic.");

builder.AddAIAgent("editor", (sp, key) => new ChatClientAgent(
    sp.GetRequiredService<IChatClient>(),
    name: key,
    instructions: "You edit short stories to improve grammar and style.",
    tools: [AIFunctionFactory.Create(FormatStory)]
));

builder.AddWorkflow("publisher", (sp, key) => AgentWorkflowBuilder.BuildSequential(
    workflowName: key,
    sp.GetRequiredKeyedService<AIAgent>("writer"),
    sp.GetRequiredKeyedService<AIAgent>("editor")
)).AddAsAIAgent();
```

## Tools and Functions

### Creating Tool Classes

Tools are implemented as classes with methods decorated with `[Description]` attributes:

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace YourNamespace.Tools;

public class YourTools
{
    private readonly YourService _service;

    public YourTools(YourService service)
    {
        _service = service;
    }

    [Description("Search for documents by content or title")]
    public string SearchDocuments(
        [Description("Search query or keywords")] string query,
        [Description("Document type filter (optional)")] string? documentType = null)
    {
        var results = _service.SearchDocuments(query, documentType);
        return JsonSerializer.Serialize(results);
    }

    [Description("Calculate business metrics")]
    public string CalculateMetrics(
        [Description("Metric type to calculate")] string metricType,
        [Description("Date range for calculation")] DateRange dateRange)
    {
        var result = _service.CalculateMetrics(metricType, dateRange);
        return JsonSerializer.Serialize(result);
    }

    // Helper method to get AIFunction collection
    public IEnumerable<AIFunction> GetFunctions()
    {
        return AIFunctionFactory.Create(this);
    }
}
```

### Inline Function Tools

For simple tools, you can define them inline:

```csharp
builder.AddAIAgent("agent", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    
    var agent = chatClient.CreateAIAgent(
        name: key,
        instructions: "Your instructions",
        tools: [
            AIFunctionFactory.Create(
                (string input) => $"Processed: {input}",
                name: "ProcessInput",
                description: "Processes the input string"
            )
        ]
    );
    
    return agent;
});
```

### Tool Parameter Types

Tools support various parameter types:
- Primitives: `string`, `int`, `decimal`, `bool`, `DateTime`
- Nullable types: `string?`, `int?`, etc.
- Complex types: Custom classes/records (will be serialized as JSON)
- Collections: `List<T>`, `IEnumerable<T>`, arrays

## Thread Store and Conversation Management

### Implementing Cosmos Thread Store

The thread store manages conversation history and state:

```csharp
public class CosmosAgentThreadStore : AgentThreadStore
{
    private readonly ICosmosThreadRepository _repository;

    public CosmosAgentThreadStore(ICosmosThreadRepository repository)
    {
        _repository = repository;
    }

    public override async Task<AgentThread> GetThreadAsync(
        AIAgent agent, 
        string threadId, 
        CancellationToken cancellationToken = default)
    {
        var conversation = await _repository.GetConversationAsync(threadId);
        
        if (conversation == null)
        {
            return new AgentThread(threadId);
        }

        var thread = new AgentThread(threadId);
        
        foreach (var msg in conversation.Messages)
        {
            thread.AppendMessage(new ChatMessage(
                msg.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                msg.Content
            ));
        }

        return thread;
    }

    public override async Task SaveThreadAsync(
        AIAgent agent, 
        string threadId, 
        AgentThread thread, 
        CancellationToken cancellationToken = default)
    {
        var messages = thread.GetMessages()
            .Select(m => new ConversationMessage
            {
                Role = m.Role.ToString().ToLower(),
                Content = m.Text,
                Timestamp = DateTime.UtcNow
            })
            .ToList();

        await _repository.SaveConversationAsync(threadId, messages);
    }
}
```

### Using Thread Store

Register and use the thread store:

```csharp
// Registration
builder.Services.AddSingleton<CosmosAgentThreadStore>();

builder.AddAIAgent("agent", (sp, key) => { /* ... */ })
    .WithThreadStore((sp, key) => sp.GetRequiredService<CosmosAgentThreadStore>());

// Usage in endpoint
var thread = await threadStore.GetThreadAsync(agent, conversationId);
await foreach (var update in agent.RunStreamingAsync(chatMessage, thread))
{
    // Process updates
}
await threadStore.SaveThreadAsync(agent, conversationId, thread);
```

## Complete Examples

### Example 1: Basic Agent with Tools

```csharp
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient(connectionName: "foundry")
    .AddChatClient("gpt-4.1");

builder.Services.AddSingleton<DocumentService>();
builder.Services.AddSingleton<DocumentTools>();

builder.AddAIAgent("doc-agent", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var tools = sp.GetRequiredService<DocumentTools>().GetFunctions();

    return chatClient.CreateAIAgent(
        name: key,
        instructions: "You help users find and manage documents.",
        tools: tools
    );
});

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
```

### Example 2: Agent with All Patterns

```csharp
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.A2A;
using Microsoft.Extensions.AI;
using A2A;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Chat client
builder.AddAzureChatCompletionsClient(connectionName: "foundry")
    .AddChatClient("gpt-4.1");

// Services
builder.Services.AddSingleton<YourService>();
builder.Services.AddSingleton<YourTools>();

// OpenAI endpoints support
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Thread store
builder.AddKeyedAzureCosmosContainer("conversations");
builder.Services.AddSingleton<CosmosAgentThreadStore>();

// Agent
builder.AddAIAgent("multi-pattern-agent", (sp, key) =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    var tools = sp.GetRequiredService<YourTools>().GetFunctions();

    return chatClient.CreateAIAgent(
        name: key,
        instructions: "You are a helpful assistant.",
        tools: tools
    );
}).WithThreadStore((sp, key) => sp.GetRequiredService<CosmosAgentThreadStore>());

var app = builder.Build();

// A2A endpoint
app.MapA2A("multi-pattern-agent", "/agenta2a", new AgentCard
{
    Name = "multi-pattern-agent",
    Url = "http://localhost:5196/agenta2a",
    Description = "Multi-pattern agent example",
    Version = "1.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    Capabilities = new AgentCapabilities
    {
        Streaming = true,
        PushNotifications = false
    },
    Skills = []
});

// OpenAI-compatible endpoints
app.MapOpenAIResponses();
app.MapOpenAIConversations();

// DevUI for development
if (builder.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.MapDefaultEndpoints();
app.Run();
```

## Best Practices

1. **Tool Design**
   - Keep tools focused and single-purpose
   - Use clear descriptions for the agent to understand when to use each tool
   - Return JSON for complex data structures
   - Handle errors gracefully and return meaningful error messages

2. **Agent Instructions**
   - Be specific about the agent's capabilities and limitations
   - Include examples of what the agent can help with
   - Specify the tone and style of responses
   - Define how the agent should handle edge cases

3. **Thread Management**
   - Always use a thread store for conversation persistence
   - Generate or use consistent conversation IDs
   - Clean up old conversations periodically
   - Consider token limits when storing conversation history

4. **Performance**
   - Use async/await consistently
   - Stream responses for better UX
   - Cache expensive operations
   - Use appropriate timeouts for remote agent calls

5. **Security**
   - Use Azure Managed Identity when possible
   - Never expose API keys in code
   - Validate and sanitize user inputs
   - Implement proper authorization for agent endpoints

6. **Testing**
   - Test tool invocations with various inputs
   - Verify streaming behavior
   - Test conversation persistence
   - Use function filters to test tool selection without LLM costs

## Additional Resources

- [Microsoft Agent Framework GitHub](https://github.com/microsoft/agent-framework/)
- [.NET Extensions Templates](https://github.com/dotnet/extensions/tree/main/src/ProjectTemplates/Microsoft.Agents.AI.ProjectTemplates)
- [Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- Repository examples: `src/agents-dotnet`, `src/groupchat-dotnet`
