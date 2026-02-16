// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedServices;

/// <summary>
/// Provides a Cosmos DB implementation of the <see cref="ChatHistoryProvider"/> abstract class
/// using a keyed Azure Cosmos container.
/// </summary>
[RequiresUnreferencedCode("The CosmosChatHistoryProvider uses JSON serialization which is incompatible with trimming.")]
[RequiresDynamicCode("The CosmosChatHistoryProvider uses JSON serialization which is incompatible with NativeAOT.")]
public sealed class CosmosChatHistoryProvider : ChatHistoryProvider, IDisposable
{
    private readonly Container _container;
    private bool _disposed;

    private readonly string? _tenantId;
    private readonly string? _userId;
    private readonly PartitionKey _partitionKey;
    private readonly bool _useHierarchicalPartitioning;

    /// <summary>
    /// Cached JSON serializer options for compatibility.
    /// </summary>
    private static readonly JsonSerializerOptions s_defaultJsonOptions = CreateDefaultJsonOptions();

    private static JsonSerializerOptions CreateDefaultJsonOptions()
    {
        var options = new JsonSerializerOptions();
#if NET9_0_OR_GREATER
        options.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
#endif
        return options;
    }

    /// <summary>
    /// Gets or sets the maximum number of messages to return in a single query batch.
    /// Default is 100 for optimal performance.
    /// </summary>
    public int MaxItemCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of messages to retrieve from the provider.
    /// This helps prevent exceeding LLM context windows in long conversations.
    /// Default is null (no limit). When set, only the most recent messages are returned.
    /// </summary>
    public int? MaxMessagesToRetrieve { get; set; }

    /// <summary>
    /// Gets or sets the Time-To-Live (TTL) in seconds for messages.
    /// Default is 86400 seconds (24 hours). Set to null to disable TTL.
    /// </summary>
    public int? MessageTtlSeconds { get; set; } = 86400;

    /// <summary>
    /// Gets the conversation ID associated with this provider.
    /// </summary>
    public string ConversationId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosChatHistoryProvider"/> class
    /// using a keyed Cosmos container.
    /// </summary>
    /// <param name="container">The Cosmos DB container to use for storage.</param>
    /// <param name="conversationId">The unique identifier for this conversation thread.</param>
    /// <param name="tenantId">Optional tenant identifier for hierarchical partitioning.</param>
    /// <param name="userId">Optional user identifier for hierarchical partitioning.</param>
    public CosmosChatHistoryProvider(
        Container container,
        string conversationId,
        string? tenantId = null,
        string? userId = null)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        ConversationId = string.IsNullOrWhiteSpace(conversationId)
            ? throw new ArgumentException("Conversation ID cannot be null or whitespace", nameof(conversationId))
            : conversationId;

        _tenantId = tenantId;
        _userId = userId;
        _useHierarchicalPartitioning = tenantId != null && userId != null;

        _partitionKey = _useHierarchicalPartitioning
            ? new PartitionKeyBuilder()
                .Add(tenantId!)
                .Add(userId!)
                .Add(conversationId)
                .Build()
            : new PartitionKey(conversationId);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CosmosChatHistoryProvider"/> class from previously
    /// serialized state.
    /// </summary>
    /// <param name="container">The Cosmos DB container to use for storage.</param>
    /// <param name="serializedState">A <see cref="JsonElement"/> representing the serialized state of the provider.</param>
    /// <param name="jsonSerializerOptions">Optional settings for customizing the JSON deserialization process.</param>
    /// <returns>A new instance of <see cref="CosmosChatHistoryProvider"/> initialized from the serialized state.</returns>
    public static CosmosChatHistoryProvider CreateFromSerializedState(
        Container container,
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        ArgumentNullException.ThrowIfNull(container);

        if (serializedState.ValueKind is not JsonValueKind.Object)
        {
            throw new ArgumentException("Invalid serialized state", nameof(serializedState));
        }

        var state = serializedState.Deserialize<State>(jsonSerializerOptions ?? s_defaultJsonOptions);
        if (state?.ConversationIdentifier is not { } conversationId)
        {
            throw new ArgumentException("Invalid serialized state", nameof(serializedState));
        }

        return state.UseHierarchicalPartitioning && state.TenantId != null && state.UserId != null
            ? new CosmosChatHistoryProvider(container, conversationId, state.TenantId, state.UserId)
            : new CosmosChatHistoryProvider(container, conversationId);
    }

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<ChatMessage>> InvokingCoreAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Fetch most recent messages in descending order when limit is set, then reverse to ascending
        var orderDirection = MaxMessagesToRetrieve.HasValue ? "DESC" : "ASC";
        var query = new QueryDefinition($"SELECT * FROM c WHERE c.conversationId = @conversationId AND c.type = @type ORDER BY c.timestamp {orderDirection}")
            .WithParameter("@conversationId", ConversationId)
            .WithParameter("@type", "ChatMessage");

        var iterator = _container.GetItemQueryIterator<CosmosMessageDocument>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = _partitionKey,
            MaxItemCount = MaxItemCount
        });

        var messages = new List<ChatMessage>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var document in response)
            {
                if (MaxMessagesToRetrieve.HasValue && messages.Count >= MaxMessagesToRetrieve.Value)
                {
                    break;
                }

                if (!string.IsNullOrEmpty(document.Message))
                {
                    var message = JsonSerializer.Deserialize<ChatMessage>(document.Message, s_defaultJsonOptions);
                    if (message != null)
                    {
                        messages.Add(message);
                    }
                }
            }

            if (MaxMessagesToRetrieve.HasValue && messages.Count >= MaxMessagesToRetrieve.Value)
            {
                break;
            }
        }

        // If we fetched in descending order (most recent first), reverse to ascending order
        if (MaxMessagesToRetrieve.HasValue)
        {
            messages.Reverse();
        }

        return messages;
    }

    /// <inheritdoc />
    protected override async ValueTask InvokedCoreAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.InvokeException is not null)
        {
            // Do not store messages if there was an exception during invocation
            return;
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
        var messageList = context.RequestMessages
            .Concat(context.RequestMessages ?? [])
            .Concat(context.ResponseMessages ?? [])
            .ToList();

        if (messageList.Count == 0)
        {
            return;
        }

        // Add all messages using simple upsert operations
        await AddMessagesAsync(messageList, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds all messages to the store using upsert operations.
    /// </summary>
    private async Task AddMessagesAsync(List<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var message in messages)
        {
            var document = CreateMessageDocument(message, currentTimestamp);

            try
            {
                await _container.UpsertItemAsync(document, _partitionKey, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                throw new InvalidOperationException(
                    $"Message exceeds Cosmos DB's maximum item size limit of 2MB. " +
                    $"Message ID: {message.MessageId}, Serialized size is too large. " +
                    "Consider reducing message content or splitting into smaller messages.",
                    ex);
            }
        }
    }

    /// <summary>
    /// Creates a message document with enhanced metadata.
    /// </summary>
    private CosmosMessageDocument CreateMessageDocument(ChatMessage message, long timestamp)
    {
        return new CosmosMessageDocument
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = ConversationId,
            Timestamp = timestamp,
            MessageId = message.MessageId,
            Role = message.Role.Value,
            Message = JsonSerializer.Serialize(message, s_defaultJsonOptions),
            Type = "ChatMessage",
            Ttl = MessageTtlSeconds,
            TenantId = _useHierarchicalPartitioning ? _tenantId : null,
            UserId = _useHierarchicalPartitioning ? _userId : null,
            SessionId = _useHierarchicalPartitioning ? ConversationId : null
        };
    }

    /// <inheritdoc />
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var state = new State
        {
            ConversationIdentifier = ConversationId,
            TenantId = _tenantId,
            UserId = _userId,
            UseHierarchicalPartitioning = _useHierarchicalPartitioning
        };

        var options = jsonSerializerOptions ?? s_defaultJsonOptions;
        return JsonSerializer.SerializeToElement(state, options);
    }

    /// <summary>
    /// Gets the count of messages in this conversation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of messages in the conversation.</returns>
    public async Task<int> GetMessageCountAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.conversationId = @conversationId AND c.type = @type")
            .WithParameter("@conversationId", ConversationId)
            .WithParameter("@type", "ChatMessage");

        var iterator = _container.GetItemQueryIterator<int>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = _partitionKey
        });

        var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
        return response.FirstOrDefault();
    }

    /// <summary>
    /// Deletes all messages in this conversation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of messages deleted.</returns>
    public async Task<int> ClearMessagesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.conversationId = @conversationId AND c.type = @type")
            .WithParameter("@conversationId", ConversationId)
            .WithParameter("@type", "ChatMessage");

        var iterator = _container.GetItemQueryIterator<string>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = _partitionKey,
            MaxItemCount = MaxItemCount
        });

        var deletedCount = 0;

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var itemId in response)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    await _container.DeleteItemAsync<CosmosMessageDocument>(itemId, _partitionKey, cancellationToken: cancellationToken).ConfigureAwait(false);
                    deletedCount++;
                }
            }
        }

        return deletedCount;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            // Container is managed by DI, so we don't dispose it
            _disposed = true;
        }
    }

    private sealed class State
    {
        [JsonPropertyName("conversationIdentifier")]
        public string ConversationIdentifier { get; set; } = string.Empty;

        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("useHierarchicalPartitioning")]
        public bool UseHierarchicalPartitioning { get; set; }
    }

    /// <summary>
    /// Represents a document stored in Cosmos DB for chat messages.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Cosmos DB operations")]
    private sealed class CosmosMessageDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("conversationId")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("ttl")]
        public int? Ttl { get; set; }

        /// <summary>
        /// Tenant ID for hierarchical partitioning scenarios (optional).
        /// </summary>
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        /// <summary>
        /// User ID for hierarchical partitioning scenarios (optional).
        /// </summary>
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        /// <summary>
        /// Session ID for hierarchical partitioning scenarios (same as ConversationId for compatibility).
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }
    }
}
