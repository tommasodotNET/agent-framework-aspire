// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.Logging;

namespace SharedServices;

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
public sealed class CosmosAgentSessionStore : AgentSessionStore
{
    private readonly ICosmosThreadRepository _repository;
    private readonly ILogger<CosmosAgentSessionStore> _logger;

    public CosmosAgentSessionStore(
        ICosmosThreadRepository repository,
        ILogger<CosmosAgentSessionStore> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async ValueTask SaveSessionAsync(
        AIAgent agent,
        string conversationId,
        AgentSession session,
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

        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var key = GetKey(conversationId, agent.Id);
        var serializedSession = session.Serialize();

        _logger.LogDebug("Saving agent session. AgentId: {AgentId}, ConversationId: {ConversationId}, Key: {Key}",
            agent.Id, conversationId, key);

        await _repository.SaveThreadAsync(key, serializedSession, cancellationToken);
    }

    /// <inheritdoc/>
    public override async ValueTask<AgentSession> GetSessionAsync(
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

        var serializedSession = await _repository.GetThreadAsync(key, cancellationToken);

        if (serializedSession == null)
        {
            _logger.LogInformation("No existing session found, creating new session. AgentId: {AgentId}, ConversationId: {ConversationId}",
                agent.Id, conversationId);
            return await agent.GetNewSessionAsync();
        }

        _logger.LogInformation("Existing session found, deserializing. AgentId: {AgentId}, ConversationId: {ConversationId}",
            agent.Id, conversationId);
        return await agent.DeserializeSessionAsync(serializedSession.Value);
    }

    /// <summary>
    /// Generates a unique key for storing the thread based on agent ID and conversation ID.
    /// </summary>
    /// <param name="conversationId">The conversation identifier</param>
    /// <param name="agentId">The agent identifier</param>
    /// <returns>A composite key in the format "agentId:conversationId"</returns>
    private static string GetKey(string conversationId, string agentId) => $"{agentId}:{conversationId}";
}
