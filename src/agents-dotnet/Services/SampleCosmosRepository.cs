using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agents.Dotnet.Services;

public class SampleCosmosRepository: ICosmosRepository
{
    protected readonly Container _cosmosContainer;
    protected readonly ILogger<SampleCosmosRepository> _logger;

    private class CosmosConversationStoreItem
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        /// <summary>
        /// The conversation identifier to which this message belongs.
        /// </summary>
        [JsonPropertyName("conversationId")]
        public required string ConversationId { get; set; }

        [JsonPropertyName("chatMessageContent")]
        public string ChatMessageContent { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the message was created/saved
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        /// <summary>
        /// Indicates if this message is a summarization of the conversation
        /// </summary>
        [JsonPropertyName("isSummary")]
        public bool IsSummary { get; set; } = false;

        [JsonPropertyName("ttl")]
        public int? Ttl { get; set; }
    }

    public SampleCosmosRepository(
        [FromKeyedServices("conversations")] Container cosmosContainer,
        //ConversationHistoryConfig conversationHistoryConfig,
        ILogger<SampleCosmosRepository> logger
    )
    {
        _cosmosContainer = cosmosContainer ?? throw new ArgumentNullException(nameof(cosmosContainer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ChatMessage>> GetMessageAsync(CustomConversationState conversationState, CancellationToken token = default)
    {
        if (conversationState == null || string.IsNullOrEmpty(conversationState.Id))
        {
            throw new ArgumentNullException(nameof(conversationState), "ConversationState or ConversationId is null or empty.");
        }

        var output = new List<ChatMessage>();
        int nextMessageId = 0;

        try
        {

            var query = new QueryDefinition("SELECT * FROM c ORDER BY c.id ASC");
            await foreach (var message in StreamMessagesAsync(query, conversationState.Id, token).ConfigureAwait(false))
            {
                var content = JsonSerializer.Deserialize<ChatMessage>(message.ChatMessageContent);
                if (content != null)
                {
                    output.Add(content);
                    var messageIndexIsNumber = int.TryParse(message.Id, out var messageIndex);
                    nextMessageId = messageIndexIsNumber ? messageIndex + 1 : 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for conversation {conversationId}", conversationState.Id);
            throw;
        }

        _logger.LogInformation("Loaded conversation {conversationId} with {messageCount} messages", conversationState.Id, output.Count);

        conversationState.NextSequence = nextMessageId;
        return output;
    }

    public async Task AddMessageAsync(CustomConversationState conversationState, IEnumerable<ChatMessage> messages, CancellationToken token = default)
    {
        if (conversationState == null || string.IsNullOrEmpty(conversationState.Id))
        {
            throw new ArgumentNullException(nameof(conversationState), "ConversationState or ConversationId is null or empty.");
        }

        var requestOptions = new ItemRequestOptions
        {
            EnableContentResponseOnWrite = false, // We don't need the response body on write
        };

        var partitionKey = new PartitionKey(conversationState.Id);

        //we should use the TransactionalBatch since we are writing multiple items on the same partition
        //in this way we can have atomicity and better performance
        //In this sample we will do single upsert for each message
        //since cosmos emulator vnext does not support batch yet
        try
        {
            foreach (var message in messages)
            {
                var conversationMessage = new CosmosConversationStoreItem
                {
                    Id = $"{conversationState.NextSequence++:D6}",
                    ConversationId = conversationState.Id,
                    ChatMessageContent = JsonSerializer.Serialize(message),
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    Ttl = -1 // Never expire, get this value from a configuration if needed
                };


                await _cosmosContainer.UpsertItemAsync(
                    conversationMessage,
                    partitionKey,
                    requestOptions: requestOptions,
                    cancellationToken: token);
            }
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Failed to add message to Cosmos DB. ConversationId: {ConversationId}", conversationState.Id);
            throw;
        }
    }


    private async IAsyncEnumerable<CosmosConversationStoreItem> StreamMessagesAsync(QueryDefinition query, string conversationId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var results = _cosmosContainer.GetItemQueryIterator<CosmosConversationStoreItem>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new(conversationId) }
        );

        while (results.HasMoreResults)
        {
            var response = await results.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }
}
