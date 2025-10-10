using McpServer.Dotnet.Tools;
using McpServer.Dotnet.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<DocumentService>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<DocumentProcessingTools>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapMcp("/mcp");

app.Run();