using System.Text.Json;

namespace SharedServices;

/// <summary>
/// Interface for storing and retrieving serialized agent threads in Cosmos DB.
/// </summary>
public interface ICosmosThreadRepository
{
    /// <summary>
    /// Saves a serialized agent thread to Cosmos DB.
    /// </summary>
    /// <param name="key">The unique key (combination of agentId and conversationId)</param>
    /// <param name="serializedThread">The serialized thread content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveThreadAsync(string key, JsonElement serializedThread, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a serialized agent thread from Cosmos DB.
    /// </summary>
    /// <param name="key">The unique key (combination of agentId and conversationId)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The serialized thread if found, null otherwise</returns>
    Task<JsonElement?> GetThreadAsync(string key, CancellationToken cancellationToken = default);
}
