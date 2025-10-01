using System;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace Agents.Dotnet.Services;

public class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonObjectSerializer systemTextJsonSerializer;

    public CosmosSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        this.systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
    }

    public CosmosSystemTextJsonSerializer()
    {
        JsonSerializerOptions serializationOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Preserve metadata for polymorphic types
            WriteIndented = false,
            // Ensure proper handling of $type metadata
            PropertyNameCaseInsensitive = false
        };

        this.systemTextJsonSerializer = new JsonObjectSerializer(serializationOptions);
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
#pragma warning disable CS8603 // Possible null reference return.
                return default;
#pragma warning restore CS8603 // Possible null reference return.
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
            return (T)this.systemTextJsonSerializer.Deserialize(stream, typeof(T), default);
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new();
        this.systemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
        streamPayload.Position = 0;
        return streamPayload;
    }
}