using System.Text.Json.Serialization;

namespace McpServer.Dotnet.Models;

public class ComplianceRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("ruleType")]
    public ComplianceRuleType RuleType { get; set; }
    
    [JsonPropertyName("severity")]
    public ComplianceSeverity Severity { get; set; }
    
    [JsonPropertyName("applicableTo")]
    public List<string> ApplicableTo { get; set; } = new();
    
    [JsonPropertyName("checkCriteria")]
    public string CheckCriteria { get; set; } = string.Empty;
    
    [JsonPropertyName("remediation")]
    public string Remediation { get; set; } = string.Empty;
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
    
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public enum ComplianceRuleType
{
    Financial,
    Safety,
    Security,
    Environmental,
    Quality,
    Legal,
    DataPrivacy,
    Procurement
}

public enum ComplianceSeverity
{
    Low,
    Medium,
    High,
    Critical
}
