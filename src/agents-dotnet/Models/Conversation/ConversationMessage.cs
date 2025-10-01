using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Agents.Dotnet.Models.Conversation;

public class ConversationThread
{
    /// <summary>
    /// the message identifier, which is a unique string inside the conversation.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The conversation identifier to which this message belongs.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public required string ConversationId { get; set; }

    [JsonPropertyName("thread")]
    public required string Thread { get; set; }
}