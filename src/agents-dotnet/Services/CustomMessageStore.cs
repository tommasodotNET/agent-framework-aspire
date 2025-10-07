using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Agents.Dotnet.Services;

public class CustomMessageStore: ChatMessageStore
{
    private readonly ICosmosRepository _repository;
    private readonly CustomConversationState _conversationState;

    public CustomMessageStore(
            ICosmosRepository repository,
            JsonElement serializedStoreState,
            JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        if (serializedStoreState.ValueKind is JsonValueKind.Object)
        {
            try
            {
                _conversationState = serializedStoreState.Deserialize<CustomConversationState>() ?? new();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse serialized state.", ex);
            }
        }
        else
        {
            _conversationState = new();
        }
    }

    //this method retrieves the messages from the repository
    //it is called automatically at the start of the agent run
    public override Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (_conversationState == null)
        {
            throw new NullReferenceException("ConversationId is null or empty.");
        }

        // Retrieve messages from the repository
        return _repository.GetMessageAsync(_conversationState, cancellationToken);
    }

    //this method adds the messages exchanged in the last conversation turn to the repository
    //it is called automatically at the end of the agent run
    public override Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        if (_conversationState == null)
        {
            throw new NullReferenceException("ConversationId is null or empty.");
        }

        // Add messages to the repository
        return _repository.AddMessageAsync(_conversationState, messages, cancellationToken);
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        //serialize our conversation state
        return _conversationState.Serialize(jsonSerializerOptions);
    }

}
