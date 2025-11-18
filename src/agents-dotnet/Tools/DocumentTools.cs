using System;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Agents.Dotnet.Services;
using Agents.Dotnet.Models.Tools;
using Agents.Dotnet.Models.UI;
using SharedModels;

namespace Agents.Dotnet.Tools;

/// <summary>
/// Document management tools that integrate with AI agents for document search, policy lookup, and compliance checking
/// </summary>
public class DocumentTools
{
    private readonly DocumentService _documentService;

    public DocumentTools(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [Description("Search for documents by content, title, category, or department")]
    public string SearchDocuments([Description("Search query or keywords")] string query, 
                                 [Description("Document type filter (optional)")] string? documentType = null,
                                 [Description("Category filter (optional)")] string? category = null)
    {
        var results = _documentService.SearchDocuments(query, documentType, category).ToList();

        return JsonSerializer.Serialize(new { 
            query = query, 
            found = results.Count, 
            documents = results.Select(d => new {
                id = d.Id,
                title = d.Title,
                type = d.Type.ToString(),
                category = d.Category,
                department = d.Department,
                version = d.Version,
                lastModified = d.LastModified,
                relevantContent = _documentService.TruncateContentAroundQuery(d.Content, query),
                tags = d.Tags
            })
        });
    }

    [Description("Check compliance rules for specific operations, amounts, or departments")]
    public string CheckCompliance([Description("Operation or activity to check")] string operation,
                                 [Description("Amount involved (if applicable)")] decimal? amount = null,
                                 [Description("Department performing the operation")] string? department = null)
    {
        var (applicableRules, requiredApprovals, violations) = _documentService.CheckCompliance(operation, amount, department);

        return JsonSerializer.Serialize(new {
            operation = operation,
            amount = amount,
            department = department,
            rulesChecked = applicableRules.Count(),
            complianceStatus = !violations.Any() ? "Compliant" : "Violations Found",
            violations = violations,
            requiredApprovals = requiredApprovals,
            applicableRules = applicableRules.Select(r => new {
                id = r.Id,
                name = r.Name,
                type = r.RuleType.ToString(),
                severity = r.Severity.ToString(),
                criteria = r.CheckCriteria,
                remediation = r.Remediation
            })
        });
    }

    [Description("Get document version history and check for latest versions")]
    public string CheckDocumentVersion([Description("Document ID or title")] string documentId)
    {
        var document = _documentService.FindDocument(documentId);

        if (document == null)
        {
            return JsonSerializer.Serialize(new { 
                error = "Document not found",
                searchedFor = documentId
            });
        }

        var versionHistory = _documentService.GetDocumentVersionHistory(document);

        return JsonSerializer.Serialize(new {
            document = new {
                id = document.Id,
                title = document.Title,
                currentVersion = document.Version,
                lastModified = document.LastModified,
                author = document.Author,
                department = document.Department
            },
            versionHistory = versionHistory,
            isLatest = true,
            nextReviewDate = document.LastModified.AddDays(365) // Annual review
        });
    }

    public IEnumerable<AIFunction> GetFunctions()
    {
        yield return AIFunctionFactory.Create(SearchDocuments);
        yield return AIFunctionFactory.Create(CheckCompliance);
        yield return AIFunctionFactory.Create(CheckDocumentVersion);
    }
}
