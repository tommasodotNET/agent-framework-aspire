using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agents.Dotnet.Services;

public class CustomConversationState
{
    /// <summary>
    /// The conversation identifier
    /// </summary>
    /// 
    [JsonPropertyName("convid")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// next message sequence number
    /// </summary>
    [JsonPropertyName("nextSequence")]
    public int NextSequence { get; set; } = 0;

    //serialize our conversation state
    public virtual JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null) =>
        JsonSerializer.SerializeToElement(new { StoreState = this }, jsonSerializerOptions);
}
