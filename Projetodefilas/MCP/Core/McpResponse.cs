using System.Text.Json.Nodes;

namespace McpServer.Core;

public sealed record McpResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public McpError? Error { get; init; }
    public JsonObject Data { get; init; } = [];
    public string? CorrelationId { get; init; }

    public static McpResponse Ok(JsonObject data, string? message = null, string? correlationId = null) =>
        new()
        {
            Success = true,
            Data = data,
            Message = message,
            CorrelationId = correlationId
        };

    public static McpResponse Fail(string message, string? correlationId = null, string? code = null) =>
        new()
        {
            Success = false,
            Message = message,
            CorrelationId = correlationId,
            Error = new McpError { Code = code ?? "error", Detail = message }
        };
}


