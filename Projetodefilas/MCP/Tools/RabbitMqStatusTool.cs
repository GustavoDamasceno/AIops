using System.Text.Json.Nodes;
using McpServer.Core;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace McpServer.Tools;

public sealed class RabbitMqStatusTool : IMcpTool
{
    public string Name => "rabbit.status";
    public string Description => "Verifica conectividade básica com o broker RabbitMQ.";

    public Task<McpResponse> ExecuteAsync(McpRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        var host = request.Payload["host"]?.GetValue<string>() ?? "localhost";
        var port = request.Payload["port"]?.GetValue<int?>() ?? 5672;
        var user = request.Payload["user"]?.GetValue<string>() ?? "guest";
        var pass = request.Payload["password"]?.GetValue<string>() ?? "guest";
        var vhost = request.Payload["vhost"]?.GetValue<string>() ?? "/";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = user,
            Password = pass,
            VirtualHost = vhost,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
        };

        try
        {
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            var serverProps = connection.ServerProperties;

            var data = new JsonObject
            {
                ["isConnected"] = true,
                ["product"] = serverProps.TryGetValue("product", out var product) ? product?.ToString() : "unknown",
                ["version"] = serverProps.TryGetValue("version", out var version) ? version?.ToString() : "unknown",
                ["host"] = host,
                ["vhost"] = vhost
            };

            logger.LogInformation("RabbitMQ OK em {Host}:{Port} vhost {VHost}", host, port, vhost);
            return Task.FromResult(McpResponse.Ok(data, "Conectado ao broker.", request.CorrelationId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao conectar no RabbitMQ em {Host}:{Port}", host, port);
            var data = new JsonObject
            {
                ["isConnected"] = false,
                ["error"] = ex.Message
            };
            return Task.FromResult(McpResponse.Fail("Não foi possível conectar ao broker.", request.CorrelationId, "rabbit_connection_error") with
            {
                Data = data
            });
        }
    }
}

