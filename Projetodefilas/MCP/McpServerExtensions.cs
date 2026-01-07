using McpServer.Core;
using McpServer.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer;

public static class McpServerExtensions
{
    public static IServiceCollection AddMcpServer(this IServiceCollection services)
    {
        services.AddSingleton<IMcpTool, ExampleElasticTool>();
        services.AddSingleton<IMcpTool, RabbitMqStatusTool>();
        services.AddSingleton(sp =>
        {
            var registry = new McpToolRegistry();
            foreach (var tool in sp.GetServices<IMcpTool>())
            {
                registry.Register(tool);
            }

            return registry;
        });

        return services;
    }

    public static IApplicationBuilder UseMcpServer(this IApplicationBuilder app) =>
        app.UseMiddleware<McpServerMiddleware>();
}

