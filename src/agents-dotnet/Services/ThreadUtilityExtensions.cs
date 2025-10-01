using System;
using Agents.Dotnet.Models;
using Microsoft.Agents.AI;

namespace Agents.Dotnet.Services;

public static class ThreadUtilityExtensions
{
    public static async Task<AgentThread> GetThreadAsync(
        this AIAgent agent,
        string? conversationId,
        CosmosConversationRepository? conversationRepository,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(agent);

        logger.LogDebug("Retrieving thread with conversationId: {ConversationId}", conversationId);

        if(conversationId is null)
        {
            return agent.GetNewThread();
        }

        var thread = await conversationRepository!.LoadAsync(agent, conversationId);

        return (ChatClientAgentThread)thread!;
    }

    public static async Task SaveThreadAsync(
        this ChatClientAgent agent,
        AgentThread thread,
        string conversationId,
        CosmosConversationRepository? conversationRepository,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        logger.LogInformation("Saving thread with ID: {ThreadId}", conversationId);

        await conversationRepository!.SaveAsync(thread, conversationId, cancellationToken);

        logger.LogInformation("Conversation for thread ID: {ThreadId} saved.", conversationId);
    }
}

