// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Agents.Dotnet.Converters;

namespace Agents.Dotnet.Models.UI;

[JsonConverter(typeof(JsonCamelCaseEnumConverter<AIChatRole>))]
public enum AIChatRole
{
    System,
    Assistant,
    User
}