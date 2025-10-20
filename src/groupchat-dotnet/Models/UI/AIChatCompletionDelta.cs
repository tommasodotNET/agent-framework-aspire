// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GroupChat.Dotnet.Models.UI;

public record AIChatCompletionDelta([property: JsonPropertyName("delta")] AIChatMessageDelta Delta)
{
    [JsonInclude, JsonPropertyName("sessionState"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SessionState;

    [JsonInclude, JsonPropertyName("context"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BinaryData? Context;
}