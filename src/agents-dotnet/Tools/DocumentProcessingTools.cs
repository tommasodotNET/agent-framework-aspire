using System.ComponentModel;
using System.Text.Json;

namespace Agents.Dotnet.Tools;

/// <summary>
/// Tools for processing and extracting content from various document formats
/// </summary>
public static class DocumentProcessingTools
{
    [Description("Extract text content from PDF documents for analysis and search")]
    public static string ExtractPdfText([Description("PDF file path or document ID")] string pdfPath)
    {
        // Mock PDF text extraction
        var extractedTexts = new Dictionary<string, string>
        {
            ["remote-work-policy.pdf"] = "Remote Work Policy - Employees may work remotely up to 3 days per week with manager approval. Core hours: 9 AM - 3 PM local time. Equipment and security requirements apply.",
            ["safety-manual.pdf"] = "Safety Manual - Warehouse safety procedures require hard hats, steel-toe boots, high-vis vests. Forklift certification mandatory. Emergency procedures and incident reporting guidelines included.",
            ["purchase-procedures.pdf"] = "Purchase Authorization Procedures - Approval matrix: <$1K supervisor, $1K-$5K manager, $5K-$25K director, >$25K VP+CFO. Three quotes required for purchases over $10K."
        };

        object result;
        if (extractedTexts.ContainsKey(pdfPath))
        {
            result = new { success = true, text = extractedTexts[pdfPath], error = (string?)null, extractedAt = DateTime.Now };
        }
        else
        {
            result = new { success = false, text = (string?)null, error = "PDF not found or could not be processed", extractedAt = DateTime.Now };
        }

        return JsonSerializer.Serialize(result);
    }

    [Description("Parse Word or PowerPoint documents and extract structured content")]
    public static string ParseOfficeDocument([Description("Document path or ID (Word/PowerPoint)")] string docPath)
    {
        // Mock Office document parsing
        var parsedDocs = new Dictionary<string, object>
        {
            ["hr-handbook.docx"] = new 
            {
                title = "Employee Handbook",
                sections = new[] { "Code of Conduct", "Benefits", "Time Off", "Remote Work", "Performance Reviews" },
                wordCount = 15420,
                lastModified = "2024-08-15T10:30:00Z",
                content = "Comprehensive guide covering employee policies, benefits, and procedures."
            },
            ["training-presentation.pptx"] = new 
            {
                title = "Safety Training Module",
                slideCount = 45,
                sections = new[] { "Introduction", "Warehouse Safety", "Emergency Procedures", "Equipment Usage", "Quiz" },
                lastModified = "2024-09-01T14:20:00Z",
                content = "Interactive safety training covering all warehouse operations and emergency procedures."
            }
        };

        object result;
        if (parsedDocs.ContainsKey(docPath))
        {
            result = new { success = true, document = parsedDocs[docPath], error = (string?)null, parsedAt = DateTime.Now };
        }
        else
        {
            result = new { success = false, document = (object?)null, error = "Document not found or unsupported format", parsedAt = DateTime.Now };
        }

        return JsonSerializer.Serialize(result);
    }

    [Description("Search and index documents from SharePoint or file system locations")]
    public static string IndexDocuments([Description("Location path (SharePoint URL or file system path)")] string location)
    {
        // Mock document indexing
        var indexResults = new Dictionary<string, object>
        {
            ["/sharepoint/policies"] = new 
            {
                indexed = 23,
                documents = new[] 
                {
                    new { name = "Remote Work Policy v2.1.pdf", size = "245 KB", lastModified = "2024-08-30" },
                    new { name = "Safety Procedures Manual.pdf", size = "1.2 MB", lastModified = "2024-09-07" },
                    new { name = "Purchase Authorization Matrix.xlsx", size = "156 KB", lastModified = "2024-09-15" }
                },
                indexedAt = DateTime.Now
            },
            ["/filesystem/docs"] = new 
            {
                indexed = 18,
                documents = new[]
                {
                    new { name = "Employee Handbook.docx", size = "892 KB", lastModified = "2024-08-15" },
                    new { name = "Vendor Agreement Template.pdf", size = "324 KB", lastModified = "2024-07-22" },
                    new { name = "Data Security Guidelines.pdf", size = "567 KB", lastModified = "2024-08-05" }
                },
                indexedAt = DateTime.Now
            }
        };

        object result;
        if (indexResults.ContainsKey(location))
        {
            result = new { success = true, indexing = indexResults[location], indexed = (int?)null, message = (string?)null, indexedAt = (DateTime?)null };
        }
        else
        {
            result = new { success = true, indexing = (object?)null, indexed = 0, message = "No documents found at specified location", indexedAt = DateTime.Now };
        }

        return JsonSerializer.Serialize(result);
    }
}
