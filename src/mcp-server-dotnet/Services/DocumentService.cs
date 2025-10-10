using McpServer.Dotnet.Models;

namespace McpServer.Dotnet.Services;

/// <summary>
/// Core business logic for document management, policy lookup, and compliance checking
/// </summary>
public class DocumentService
{
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
}
