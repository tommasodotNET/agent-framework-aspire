using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Agents.Dotnet.Services;
using Agents.Dotnet.Tools;
using Azure.AI.OpenAI;
using Azure.Identity;

namespace Agents.Dotnet.Tests;

[TestClass]
public class DocumentAgentBehaviorTests
{
    private AIAgent _agent = null!;
    private readonly List<FunctionInvocation> _functionCalls = new();

    [TestInitialize]
    public void Setup()
    {
        // Get Azure OpenAI configuration from environment variables
        var endpoint = "https://tstocchi-foundry.openai.azure.com/";
        var deploymentName = "gpt-4.1";

        // Create Azure OpenAI client
        var azureOpenAIClient = new AzureOpenAIClient(new Uri(endpoint),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions() { TenantId = "16b3c013-d300-468d-ac64-7eda0820b6d3" }))
            .GetChatClient(deploymentName);
        var chatClient = azureOpenAIClient.AsIChatClient();

        // Create document services and tools
        var documentService = new DocumentService();
        var documentTools = new DocumentTools(documentService);
        var tools = documentTools.GetFunctions();

        // Create base agent
        var baseAgent = new ChatClientAgent(
            chatClient,
            name: "test-document-agent",
            instructions: @"You are a specialized Document Management and Policy Compliance Assistant. 
                           When users ask about policies, use the LookupPolicy tool.
                           When users ask to find or search documents, use the SearchDocuments tool.
                           When users ask about compliance or approval requirements, use the CheckCompliance tool.",
            tools: [.. tools]);

        // Add middleware to track function calls
        _agent = baseAgent
            .AsBuilder()
            .Use(FunctionCallTrackingMiddleware)
            .Build();
    }

        // Function call tracking middleware - logs and tracks function invocations
    private async ValueTask<object?> FunctionCallTrackingMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
    {
        // Record the function call before execution
        var functionCall = new FunctionInvocation(context.Function.Name, context.Arguments);
        _functionCalls.Add(functionCall);
        
        Console.WriteLine($"Function tracked: {context.Function.Name} with parameters: {string.Join(", ", context.Arguments.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

        // Execute the function
        var result = await next(context, cancellationToken);
        
        Console.WriteLine($"Function {context.Function.Name} completed");
        
        return result;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _functionCalls.Clear();
    }

    [TestMethod]
    public async Task Agent_WhenAskedToSearchDocuments_ShouldCallSearchDocumentsTool()
    {
        // Arrange
        var userMessage = new ChatMessage(ChatRole.User, "Find documents about safety procedures");

        // Act
        var response = await _agent.RunAsync(userMessage, options: new ChatClientAgentRunOptions());

        // Assert
        Assert.IsTrue(WasFunctionCalled("SearchDocuments"), 
            "Agent should have called the SearchDocuments function when asked to find documents");
    }

    [TestMethod]
    public async Task Agent_WhenAskedAboutComplianceRequirements_ShouldCallCheckComplianceTool()
    {
        // Arrange
        var userMessage = new ChatMessage(ChatRole.User, "What are the approval requirements for purchases over $5000?");

        // Act
        var response = await _agent.RunAsync(userMessage, options: new ChatClientAgentRunOptions());

        // Assert
        Assert.IsTrue(WasFunctionCalled("CheckCompliance"), 
            "Agent should have called the CheckCompliance function when asked about approval requirements");
    }

    // Helper method to check if a function was called
    private bool WasFunctionCalled(string functionName) => 
        _functionCalls.Any(f => f.FunctionName.Equals(functionName, StringComparison.OrdinalIgnoreCase));

    // Record structure for function invocations
    private record FunctionInvocation(string FunctionName, IReadOnlyDictionary<string, object?> Arguments);
}