using Microsoft.Extensions.Logging;

namespace McpServer.Core;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    Task<McpResponse> ExecuteAsync(McpRequest request, ILogger logger, CancellationToken cancellationToken);
}


