using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace McpServer.Core;

public sealed class McpServerMiddleware
{
    private static readonly ActivitySource ActivitySource = new("McpServer");
    private readonly RequestDelegate _next;
    private readonly McpToolRegistry _registry;
    private readonly ILogger<McpServerMiddleware> _logger;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public McpServerMiddleware(RequestDelegate next, McpToolRegistry registry, ILogger<McpServerMiddleware> logger)
    {
        _next = next;
        _registry = registry;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            await _next(context);
            return;
        }

        using var activity = ActivitySource.StartActivity("mcp.request", ActivityKind.Server);
        var request = new McpRequest();

        try
        {
            request = await JsonSerializer.DeserializeAsync<McpRequest>(context.Request.Body, _serializerOptions, context.RequestAborted)
                      ?? new McpRequest();
            activity?.SetTag("mcp.tool", request.Tool);
            activity?.SetTag("mcp.correlation_id", request.CorrelationId);

            if (!_registry.TryResolve(request.Tool, out var tool))
            {
                var notFound = McpResponse.Fail($"Tool '{request.Tool}' não encontrada.", request.CorrelationId, "tool_not_found");
                await WriteResponse(context, StatusCodes.Status404NotFound, notFound);
                activity?.SetStatus(ActivityStatusCode.Error, "tool_not_found");
                return;
            }

            var result = await tool!.ExecuteAsync(request, _logger, context.RequestAborted);
            var status = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
            await WriteResponse(context, status, result);
            activity?.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Requisição MCP cancelada. CorrelationId: {CorrelationId}", request?.CorrelationId);
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha inesperada ao processar MCP. CorrelationId: {CorrelationId}", request?.CorrelationId);
            var fail = McpResponse.Fail("Erro interno ao processar MCP.", request?.CorrelationId, "server_error");
            await WriteResponse(context, StatusCodes.Status500InternalServerError, fail);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }

    private async Task WriteResponse(HttpContext context, int statusCode, McpResponse response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await JsonSerializer.SerializeAsync(context.Response.Body, response, _serializerOptions, context.RequestAborted);
    }
}

