using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agents.Dotnet.Services;

/// <summary>
/// Cosmos DB implementation for storing and retrieving serialized agent threads.
/// </summary>
public class CosmosThreadRepository : ICosmosThreadRepository
{
    private readonly Container _cosmosContainer;
    private readonly ILogger<CosmosThreadRepository> _logger;

    /// <summary>
    /// Cosmos DB item model for storing agent threads.
    /// </summary>
    private class CosmosThreadItem
    {
        /// <summary>
        /// Unique identifier (combination of agentId and conversationId)
        /// </summary>
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// Partition key - using the same id for simplicity
        /// </summary>
        [JsonPropertyName("conversationId")]
        public required string ConversationId { get; set; }

        /// <summary>
        /// The serialized thread content as a JSON string
        /// </summary>
        [JsonPropertyName("serializedThread")]
        public required string SerializedThread { get; set; }

        /// <summary>
        /// Timestamp when the thread was last updated
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public string LastUpdated { get; set; } = DateTime.UtcNow.ToString("o");

        /// <summary>
        /// Time to live in seconds (-1 for never expire)
        /// </summary>
        [JsonPropertyName("ttl")]
        public int? Ttl { get; set; }
    }

    public CosmosThreadRepository(
        [FromKeyedServices("conversations")] Container cosmosContainer,
        ILogger<CosmosThreadRepository> logger)
    {
        _cosmosContainer = cosmosContainer ?? throw new ArgumentNullException(nameof(cosmosContainer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task SaveThreadAsync(string key, JsonElement serializedThread, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        try
        {
            // Convert JsonElement to JSON string
            var serializedThreadString = JsonSerializer.Serialize(serializedThread);

            var threadItem = new CosmosThreadItem
            {
                Id = key,
                ConversationId = key,
                SerializedThread = serializedThreadString,
                LastUpdated = DateTime.UtcNow.ToString("o"),
                Ttl = -1 // Never expire, adjust as needed
            };

            var requestOptions = new ItemRequestOptions
            {
                EnableContentResponseOnWrite = false
            };

            await _cosmosContainer.UpsertItemAsync(
                threadItem,
                new PartitionKey(key),
                requestOptions: requestOptions,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully saved agent thread with key: {Key}", key);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to save agent thread to Cosmos DB. Key: {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<JsonElement?> GetThreadAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        try
        {
            var response = await _cosmosContainer.ReadItemAsync<CosmosThreadItem>(
                key,
                new PartitionKey(key),
                cancellationToken: cancellationToken);

            // Deserialize the JSON string back to JsonElement
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(response.Resource.SerializedThread);

            _logger.LogInformation("Successfully retrieved agent thread with key: {Key}", key);
            return jsonElement;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Agent thread not found for key: {Key}", key);
            return null;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to retrieve agent thread from Cosmos DB. Key: {Key}", key);
            throw;
        }
    }
}
