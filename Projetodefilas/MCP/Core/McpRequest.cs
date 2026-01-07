using System.Text.Json.Nodes;

namespace McpServer.Core;

public sealed record McpRequest
{
    public string Tool { get; init; } = string.Empty;
    public JsonObject Payload { get; init; } = [];
    public string? CorrelationId { get; init; }
}


