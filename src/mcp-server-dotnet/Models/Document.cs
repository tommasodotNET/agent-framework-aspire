using System.Text.Json.Serialization;

namespace McpServer.Dotnet.Models;

public class Document
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public DocumentType Type { get; set; }
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; }
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;
    
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public enum DocumentType
{
    Policy,
    Procedure,
    Handbook,
    ComplianceRule,
    TechnicalSpec,
    Contract,
    TrainingMaterial,
    SafetyProcedure
}
