using Agents.Dotnet.Models.Tools;

namespace Agents.Dotnet.Services;

/// <summary>
/// Core business logic for document management, policy lookup, and compliance checking
/// </summary>
public class DocumentService
{
    public List<Document> GetDocuments()
    {
        return new List<Document>
        {
            new Document
            {
                Id = "DOC001",
                Title = "Remote Work Policy",
                Type = DocumentType.Policy,
                Category = "HR",
                Content = "Employees may work remotely up to 3 days per week with manager approval. Remote work days must be scheduled in advance and communicated to the team. Employees must maintain regular working hours and be available during core business hours (9 AM - 3 PM local time).",
                LastModified = DateTime.Now.AddDays(-30),
                Version = "2.1",
                Author = "HR Department",
                Department = "Human Resources",
                Tags = new List<string> { "remote work", "flexibility", "scheduling", "approval" },
                IsActive = true
            },
            new Document
            {
                Id = "DOC002",
                Title = "Warehouse Safety Procedures",
                Type = DocumentType.SafetyProcedure,
                Category = "Safety",
                Content = "All warehouse personnel must wear safety equipment including hard hats, steel-toe boots, and high-visibility vests. Forklift operators must be certified and conduct daily equipment inspections. Emergency exits must remain clear at all times. Report any safety hazards immediately to supervisors.",
                LastModified = DateTime.Now.AddDays(-15),
                Version = "3.2",
                Author = "Safety Committee",
                Department = "Operations",
                Tags = new List<string> { "safety", "warehouse", "equipment", "emergency" },
                IsActive = true
            },
            new Document
            {
                Id = "DOC003",
                Title = "Purchase Approval Matrix",
                Type = DocumentType.Procedure,
                Category = "Finance",
                Content = "Purchase approvals required: Under $1,000 - Supervisor approval; $1,000-$5,000 - Department Manager approval; $5,000-$25,000 - Director approval; Over $25,000 - VP approval and CFO sign-off required. All purchases over $10,000 require three quotes.",
                LastModified = DateTime.Now.AddDays(-7),
                Version = "1.8",
                Author = "Finance Department",
                Department = "Finance",
                Tags = new List<string> { "procurement", "approval", "budget", "authorization" },
                IsActive = true
            },
            new Document
            {
                Id = "DOC004",
                Title = "Data Security Handbook",
                Type = DocumentType.Handbook,
                Category = "IT Security",
                Content = "All employees must use strong passwords with minimum 12 characters including numbers and symbols. Two-factor authentication is required for all business systems. Confidential data must not be stored on personal devices. Report security incidents within 2 hours of discovery.",
                LastModified = DateTime.Now.AddDays(-45),
                Version = "4.0",
                Author = "IT Security Team",
                Department = "Information Technology",
                Tags = new List<string> { "security", "passwords", "2FA", "confidentiality" },
                IsActive = true
            },
            new Document
            {
                Id = "DOC005",
                Title = "Vendor Agreement Template",
                Type = DocumentType.Contract,
                Category = "Legal",
                Content = "Standard vendor agreement template includes terms for payment (Net 30), liability limitations, confidentiality clauses, and termination procedures. All contracts must be reviewed by Legal department before execution. Minimum insurance requirements: $1M general liability.",
                LastModified = DateTime.Now.AddDays(-60),
                Version = "2.3",
                Author = "Legal Department",
                Department = "Legal",
                Tags = new List<string> { "contract", "vendor", "liability", "insurance" },
                IsActive = true
            }
        };
    }

    public List<Policy> GetPolicies()
    {
        return new List<Policy>
        {
            new Policy
            {
                Id = "POL001",
                Name = "Remote Work Policy",
                Description = "Guidelines for remote work arrangements and expectations",
                Category = PolicyCategory.RemoteWork,
                EffectiveDate = DateTime.Now.AddDays(-90),
                ReviewDate = DateTime.Now.AddDays(270),
                Owner = "HR Director",
                Approver = "Chief People Officer",
                Requirements = new List<string>
                {
                    "Manager approval required",
                    "Maximum 3 days per week remote",
                    "Available during core hours (9 AM - 3 PM)",
                    "Secure internet connection required"
                },
                Procedures = new List<string>
                {
                    "Submit remote work request 1 week in advance",
                    "Update calendar with remote work days",
                    "Join daily standups via video",
                    "Complete monthly remote work survey"
                },
                Exceptions = new List<string>
                {
                    "New employees (first 90 days)",
                    "Employees on performance improvement plans",
                    "Roles requiring physical presence"
                },
                IsActive = true
            },
            new Policy
            {
                Id = "POL002",
                Name = "Purchase Authorization Policy",
                Description = "Authorization levels and procedures for company purchases",
                Category = PolicyCategory.Financial,
                EffectiveDate = DateTime.Now.AddDays(-180),
                ReviewDate = DateTime.Now.AddDays(180),
                Owner = "CFO",
                Approver = "Board of Directors",
                Requirements = new List<string>
                {
                    "Budget approval before purchase",
                    "Three quotes for purchases over $10,000",
                    "Written justification for sole-source purchases"
                },
                Procedures = new List<string>
                {
                    "Submit purchase request form",
                    "Obtain required approvals based on amount",
                    "Create purchase order",
                    "Verify receipt and approve invoice"
                },
                Exceptions = new List<string>
                {
                    "Emergency purchases (with post-approval)",
                    "Recurring contractual payments",
                    "Pre-approved training and travel"
                },
                IsActive = true
            }
        };
    }

    public List<ComplianceRule> GetComplianceRules()
    {
        return new List<ComplianceRule>
        {
            new ComplianceRule
            {
                Id = "RULE001",
                Name = "Purchase Authorization Compliance",
                Description = "Ensures proper approval levels for purchase requests",
                RuleType = ComplianceRuleType.Financial,
                Severity = ComplianceSeverity.High,
                ApplicableTo = new List<string> { "All Employees", "Finance", "Procurement" },
                CheckCriteria = "Purchase amount vs approval level: <$1K=Supervisor, $1K-$5K=Manager, $5K-$25K=Director, >$25K=VP+CFO",
                Remediation = "Route purchase request to appropriate approver based on amount",
                LastUpdated = DateTime.Now.AddDays(-30),
                IsActive = true
            },
            new ComplianceRule
            {
                Id = "RULE002",
                Name = "Safety Equipment Compliance",
                Description = "Mandatory safety equipment requirements for warehouse operations",
                RuleType = ComplianceRuleType.Safety,
                Severity = ComplianceSeverity.Critical,
                ApplicableTo = new List<string> { "Warehouse Staff", "Operations" },
                CheckCriteria = "Hard hat, steel-toe boots, high-vis vest required in warehouse areas",
                Remediation = "Immediately stop work and obtain required safety equipment",
                LastUpdated = DateTime.Now.AddDays(-15),
                IsActive = true
            },
            new ComplianceRule
            {
                Id = "RULE003",
                Name = "Data Security Compliance",
                Description = "Password and authentication requirements",
                RuleType = ComplianceRuleType.Security,
                Severity = ComplianceSeverity.High,
                ApplicableTo = new List<string> { "All Employees" },
                CheckCriteria = "Password minimum 12 characters, 2FA enabled, no personal device storage",
                Remediation = "Update password, enable 2FA, remove data from personal devices",
                LastUpdated = DateTime.Now.AddDays(-10),
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Search for documents based on query, type, and category filters
    /// </summary>
    public IEnumerable<Document> SearchDocuments(string query, string? documentType = null, string? category = null)
    {
        var documents = GetDocuments();
        
        return documents.Where(d => 
            d.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            d.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            d.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            d.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            documentType == null || d.Type.ToString().Contains(documentType, StringComparison.OrdinalIgnoreCase) ||
            category == null || d.Category.Contains(category, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Search for policies based on query and category filters
    /// </summary>
    public IEnumerable<Policy> SearchPolicies(string query, string? category = null)
    {
        var policies = GetPolicies();
        
        return policies.Where(p => 
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Requirements.Any(req => req.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            p.Procedures.Any(proc => proc.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            category == null || p.Category.ToString().Contains(category, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Check compliance rules for a given operation, amount, and department
    /// </summary>
    public (IEnumerable<ComplianceRule> ApplicableRules, IEnumerable<object> RequiredApprovals, IEnumerable<object> Violations) 
        CheckCompliance(string operation, decimal? amount = null, string? department = null)
    {
        var rules = GetComplianceRules();
        
        // Find applicable compliance rules
        var applicableRules = rules.Where(r => 
            r.IsActive &&
            (r.ApplicableTo.Any(a => a.Contains("All", StringComparison.OrdinalIgnoreCase)) ||
             r.ApplicableTo.Any(a => department != null && a.Contains(department, StringComparison.OrdinalIgnoreCase)) ||
             r.CheckCriteria.Contains(operation, StringComparison.OrdinalIgnoreCase))
        );

        var violations = new List<object>();
        var approvals = new List<object>();

        foreach (var rule in applicableRules)
        {
            if (rule.RuleType == ComplianceRuleType.Financial && amount.HasValue)
            {
                // Check purchase approval levels
                if (operation.Contains("purchase", StringComparison.OrdinalIgnoreCase))
                {
                    var approvalLevel = GetRequiredApprovalLevel(amount.Value);
                    approvals.Add(new {
                        rule = rule.Name,
                        required = $"{approvalLevel.Level} approval required for ${amount:N2}",
                        approver = approvalLevel.Approver,
                        additionalRequirements = amount > 10000 ? "Three quotes required" : null
                    });
                }
            }
            else if (rule.RuleType == ComplianceRuleType.Safety && 
                    operation.Contains("warehouse", StringComparison.OrdinalIgnoreCase))
            {
                approvals.Add(new {
                    rule = rule.Name,
                    required = "Safety equipment mandatory",
                    equipment = new[] { "Hard hat", "Steel-toe boots", "High-visibility vest" },
                    severity = rule.Severity.ToString()
                });
            }
        }

        return (applicableRules, approvals, violations);
    }

    /// <summary>
    /// Get required approval level based on purchase amount
    /// </summary>
    public (string Level, string Approver) GetRequiredApprovalLevel(decimal amount)
    {
        return amount switch
        {
            < 1000 => ("Supervisor", "Direct Supervisor"),
            >= 1000 and < 5000 => ("Manager", "Department Manager"),
            >= 5000 and < 25000 => ("Director", "Department Director"),
            _ => ("Executive", "VP + CFO")
        };
    }

    /// <summary>
    /// Find a document by ID or title
    /// </summary>
    public Document? FindDocument(string documentId)
    {
        var documents = GetDocuments();
        return documents.FirstOrDefault(d => 
            d.Id.Equals(documentId, StringComparison.OrdinalIgnoreCase) ||
            d.Title.Contains(documentId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Generate version history for a document (mocked)
    /// </summary>
    public IEnumerable<object> GetDocumentVersionHistory(Document document)
    {
        return new[]
        {
            new { version = document.Version, date = document.LastModified, status = "Current", changes = "Latest approved version" },
            new { version = GetPreviousVersion(document.Version), date = document.LastModified.AddDays(-30), status = "Previous", changes = "Minor updates to procedures" },
            new { version = GetPreviousVersion(GetPreviousVersion(document.Version)), date = document.LastModified.AddDays(-90), status = "Archived", changes = "Initial version" }
        };
    }

    /// <summary>
    /// Truncate content around a search query for display
    /// </summary>
    public string TruncateContentAroundQuery(string content, string query)
    {
        var index = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var start = Math.Max(0, index - 50);
            var length = Math.Min(200, content.Length - start);
            return "..." + content.Substring(start, length) + "...";
        }
        return content.Length > 150 ? content.Substring(0, 150) + "..." : content;
    }

    private string GetPreviousVersion(string currentVersion)
    {
        if (Version.TryParse(currentVersion, out var version))
        {
            var newMinor = Math.Max(0, version.Minor - 1);
            return $"{version.Major}.{newMinor}";
        }
        return "1.0";
    }
}
