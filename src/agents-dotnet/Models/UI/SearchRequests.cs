using System.Text.Json.Serialization;
using Agents.Dotnet.Models.Tools;

namespace Agents.Dotnet.Models.UI;

public class DocumentSearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("documentType")]
    public DocumentType? DocumentType { get; set; }
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("department")]
    public string? Department { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class PolicySearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public PolicyCategory? Category { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; } = true;
}

public class ComplianceCheckRequest
{
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
    
    [JsonPropertyName("department")]
    public string? Department { get; set; }
    
    [JsonPropertyName("ruleType")]
    public ComplianceRuleType? RuleType { get; set; }
}