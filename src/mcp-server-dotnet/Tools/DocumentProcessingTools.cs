using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using McpServer.Dotnet.Services;

namespace McpServer.Dotnet.Tools;

[McpServerToolType]
public class DocumentProcessingTools
{
    private readonly DocumentService _documentService;

    public DocumentProcessingTools(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [McpServerTool, Description("Look up company policies by name, category, or keywords")]
    public string LookupPolicy(
        [Description("Policy name or keywords to search")] string query,
        [Description("Policy category filter (optional)")] string? category = null)
    {
        try
        {
            var results = _documentService.SearchPolicies(query, category).ToList();

            var response = new
            {
                query = query,
                found = results.Count,
                policies = results.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    category = p.Category.ToString(),
                    effectiveDate = p.EffectiveDate,
                    reviewDate = p.ReviewDate,
                    owner = p.Owner,
                    approver = p.Approver,
                    requirements = p.Requirements,
                    procedures = p.Procedures,
                    exceptions = p.Exceptions,
                    isActive = p.IsActive
                })
            };

            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            var errorResult = new
            {
                query = query,
                category = category,
                status = "error",
                error = ex.Message
            };

            return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}