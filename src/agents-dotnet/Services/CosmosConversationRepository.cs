using System.Runtime.CompilerServices;
using System.Text.Json;
using Agents.Dotnet.Models.Conversation;
using Agents.Dotnet.Models.UI;
using Microsoft.Azure.Cosmos;
using Microsoft.Agents.AI;

namespace Agents.Dotnet.Services;

public class CosmosConversationRepository
{
    private readonly Container _cosmosContainer;
    private readonly ILogger<CosmosConversationRepository> _logger;

    private static JsonElement EnsureProperJsonStructure(JsonElement element, ILogger logger)
    {
        try
        {
            // If the element contains polymorphic type information, ensure $type is first
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("$type", out _))
            {
                // Re-serialize to ensure proper ordering
                var json = element.GetRawText();
                using var document = JsonDocument.Parse(json);
                
                // Create a new object with $type first
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                
                writer.WriteStartObject();
                
                // Write $type first if it exists
                if (document.RootElement.TryGetProperty("$type", out var typeProperty))
                {
                    writer.WritePropertyName("$type");
                    typeProperty.WriteTo(writer);
                }
                
                // Write all other properties
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Name != "$type")
                    {
                        property.WriteTo(writer);
                    }
                }
                
                writer.WriteEndObject();
                writer.Flush();
                
                stream.Position = 0;
                using var newDocument = JsonDocument.Parse(stream);
                return newDocument.RootElement.Clone();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to restructure JSON, using original element");
        }
        
        return element;
    }

    public CosmosConversationRepository([FromKeyedServices("conversations")] Container cosmosContainer,
        ILogger<CosmosConversationRepository> logger)
    {
        _cosmosContainer = cosmosContainer ?? throw new ArgumentNullException(nameof(cosmosContainer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentThread?> LoadAsync(AIAgent agent, string? conversationId = null, CancellationToken cancellationToken = default)
    {
        if (conversationId is null)
        {
            _logger.LogDebug("No conversation ID provided, starting a new conversation.");
            return null;
        }
        
        var query = new QueryDefinition("SELECT * FROM c");

        // Stream processing for better memory efficiency
        await foreach (var message in StreamMessagesAsync(query, conversationId, cancellationToken))
        {
            try
            {
                var jsonString = message.Thread;
                
                // Log the raw JSON for debugging
                _logger.LogDebug("Attempting to deserialize thread JSON: {JsonContent}", jsonString);
                
                // Parse the string back to JsonElement
                using var document = JsonDocument.Parse(jsonString);
                var jsonElement = document.RootElement;
                
                // Ensure proper JSON structure for polymorphic types
                var structuredElement = EnsureProperJsonStructure(jsonElement, _logger);
                
                return agent.DeserializeThread(structuredElement);
            }
            catch (JsonException ex)
            {
                // Enhanced logging to understand the issue better
                _logger.LogError(ex, "Failed to deserialize thread for conversationId {conversationId}. Raw JSON: {RawJson}", 
                    conversationId, message.Thread);
            }
        }

        return null;
    }

    //TODO: passare ad approccio con TransactionBatch per migliorare le performance appena disponibile nell'emulatore
    public async Task SaveAsync(AgentThread agentThread, string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agentThread);
        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty.", nameof(conversationId));
        }

        _logger.LogDebug("Saving conversation {conversationId}", conversationId);

        var partitionKey = new PartitionKey(conversationId);

        var serializedThread = agentThread.Serialize();
        var messageDocument = new ConversationThread()
        {
            Id = conversationId,
            ConversationId = conversationId,
            Thread = serializedThread.GetRawText()
        };

        await _cosmosContainer.UpsertItemAsync(
            messageDocument,
            partitionKey,
            cancellationToken: cancellationToken);
    }

    private async IAsyncEnumerable<ConversationThread> StreamMessagesAsync(QueryDefinition query, string conversationId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var results = _cosmosContainer.GetItemQueryIterator<ConversationThread>(
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
