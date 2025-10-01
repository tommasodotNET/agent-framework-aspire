using System;
using Microsoft.Extensions.AI;

namespace Agents.Dotnet.Models.Conversation;

public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<ChatMessage> Messages { get; set; } = [];
}