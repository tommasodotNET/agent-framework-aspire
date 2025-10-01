using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Agents.Dotnet.Tests;

public class AzureOpenAIOptions
{
    public string DeploymentName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? TenantId { get; set; }
}

public static class ConfigurationHelper
{
    public static AzureOpenAIOptions ReadOpenAIConfig()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.Configure<AzureOpenAIOptions>(options => configuration.GetSection("AzureOpenAI").Bind(options));
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    }
}