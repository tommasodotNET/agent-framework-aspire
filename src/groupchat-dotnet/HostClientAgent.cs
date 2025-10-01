using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace GroupChat.Dotnet;

public class HostClientAgent
{
    public async Task InitializeAgentAsync(IChatClient chatClient, string[] agentUrls)
    {
        try
        {
            // Connect to the remote agents via A2A
            var createAgentTasks = agentUrls.Select(CreateAgentAsync);
            var agents = await Task.WhenAll(createAgentTasks);
            var tools = agents.Select(agent => (AITool)agent.AsAIFunction()).ToList();

            this.Agent = new ChatClientAgent(chatClient,
                name: "HostClient",
                instructions: "You specialize in handling queries for users and using your tools to provide answers.",
                tools: tools);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing HostClientAgent: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// The associated <see cref="Agent"/>
    /// </summary>
    public AIAgent? Agent { get; private set; }

    #region private
    private static async Task<AIAgent> CreateAgentAsync(string agentUri)
    {
        var url = new Uri(agentUri);
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        var agentCardResolver = new A2ACardResolver(url, httpClient);

        return await agentCardResolver.GetAIAgentAsync();
    }
    #endregion
}