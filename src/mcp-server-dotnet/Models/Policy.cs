using System.Text.Json.Serialization;

namespace McpServer.Dotnet.Models;

public class Policy
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public PolicyCategory Category { get; set; }
    
    [JsonPropertyName("effectiveDate")]
    public DateTime EffectiveDate { get; set; }
    
    [JsonPropertyName("reviewDate")]
    public DateTime ReviewDate { get; set; }
    
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;
    
    [JsonPropertyName("approver")]
    public string Approver { get; set; } = string.Empty;
    
    [JsonPropertyName("requirements")]
    public List<string> Requirements { get; set; } = new();
    
    [JsonPropertyName("procedures")]
    public List<string> Procedures { get; set; } = new();
    
    [JsonPropertyName("exceptions")]
    public List<string> Exceptions { get; set; } = new();
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public enum PolicyCategory
{
    HR,
    Safety,
    Security,
    Financial,
    IT,
    Operations,
    Legal,
    RemoteWork,
    Procurement
}
