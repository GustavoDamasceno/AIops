namespace McpServer.Core;

public sealed record McpError
{
    public string Code { get; init; } = "error";
    public string Detail { get; init; } = string.Empty;
}


