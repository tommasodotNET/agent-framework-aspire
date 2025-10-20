using Microsoft.Extensions.AI;

namespace GroupChat.Dotnet.Services;

public interface ICosmosRepository
{
    Task AddMessageAsync(CustomConversationState conversationState, IEnumerable<ChatMessage> messages, CancellationToken token = default);
    Task<IEnumerable<ChatMessage>> GetMessageAsync(CustomConversationState conversationState, CancellationToken token = default);
}