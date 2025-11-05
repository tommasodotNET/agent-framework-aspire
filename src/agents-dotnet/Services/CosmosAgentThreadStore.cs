// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.Logging;

namespace Agents.Dotnet.Services;

/// <summary>
/// Provides a Cosmos DB implementation of <see cref="AgentThreadStore"/> for production scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This implementation stores serialized agent threads in Azure Cosmos DB and is suitable for:
/// <list type="bullet">
/// <item><description>Production multi-instance deployments</description></item>
/// <item><description>Scenarios requiring thread persistence across application restarts</description></item>
/// <item><description>Distributed systems with shared conversation state</description></item>
/// </list>
/// </para>
/// <para>
/// The store uses a composite key consisting of the agent ID and conversation ID to uniquely identify each thread.
/// This allows multiple agents to maintain separate threads for the same conversation.
/// </para>
/// </remarks>
public sealed class CosmosAgentThreadStore : AgentThreadStore
{
    private readonly ICosmosThreadRepository _repository;
    private readonly ILogger<CosmosAgentThreadStore> _logger;

    public CosmosAgentThreadStore(
        ICosmosThreadRepository repository,
        ILogger<CosmosAgentThreadStore> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async ValueTask SaveThreadAsync(
        AIAgent agent,
        string conversationId,
        AgentThread thread,
        CancellationToken cancellationToken = default)
    {
        if (agent == null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
        }

        if (thread == null)
        {
            throw new ArgumentNullException(nameof(thread));
        }

        var key = GetKey(conversationId, agent.Id);
        var serializedThread = thread.Serialize();

        _logger.LogDebug("Saving agent thread. AgentId: {AgentId}, ConversationId: {ConversationId}, Key: {Key}",
            agent.Id, conversationId, key);

        await _repository.SaveThreadAsync(key, serializedThread, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<AgentThread> GetThreadAsync(
        AIAgent agent,
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        if (agent == null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
        }

        var key = GetKey(conversationId, agent.Id);

        _logger.LogDebug("Retrieving agent thread. AgentId: {AgentId}, ConversationId: {ConversationId}, Key: {Key}",
            agent.Id, conversationId, key);

        var serializedThread = await _repository.GetThreadAsync(key, cancellationToken);

        if (serializedThread == null)
        {
            _logger.LogInformation("No existing thread found, creating new thread. AgentId: {AgentId}, ConversationId: {ConversationId}",
                agent.Id, conversationId);
            return agent.GetNewThread();
        }

        _logger.LogInformation("Existing thread found, deserializing. AgentId: {AgentId}, ConversationId: {ConversationId}",
            agent.Id, conversationId);
        return agent.DeserializeThread(serializedThread.Value);
    }

    /// <summary>
    /// Generates a unique key for storing the thread based on agent ID and conversation ID.
    /// </summary>
    /// <param name="conversationId">The conversation identifier</param>
    /// <param name="agentId">The agent identifier</param>
    /// <returns>A composite key in the format "agentId:conversationId"</returns>
    private static string GetKey(string conversationId, string agentId) => $"{agentId}:{conversationId}";
}
