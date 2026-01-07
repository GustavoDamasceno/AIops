using McpServer.Core;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpServer.Tools;

public sealed class ExampleElasticTool : IMcpTool
{
    private readonly OpenSearchClient _client;

    public string Name => "logs.search";
    public string Description => "Consulta logs no OpenSearch no índice projetofila-*";

    public ExampleElasticTool()
    {
        var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
            .DefaultIndex("projetofila-*");

        _client = new OpenSearchClient(settings);
    }

    public async Task<McpResponse> ExecuteAsync(
        McpRequest request,
        ILogger logger,
        CancellationToken ct)
    {
        // --------- ler parâmetro "query" ----------
        string query = "*";

        if (request.Payload is JsonObject obj &&
            obj.TryGetPropertyValue("query", out var node) &&
            node is JsonValue value &&
            value.TryGetValue<string>(out var q))
        {
            query = q;
        }

        logger.LogInformation("Executando busca no OpenSearch com query: {Query}", query);

        // --------- consulta ----------
        var result = await _client.SearchAsync<object>(s => s
            .Index("projetofila-*")
            .Query(q => q.QueryString(qs => qs.Query(query)))
            .Size(50),
            ct);

        if (!result.IsValid)
        {
            logger.LogError("Erro ao consultar OpenSearch: {Error}",
                result.ServerError?.Error.Reason);

            return McpResponse.Fail("Erro ao consultar o OpenSearch",request.CorrelationId,"opensearch_error");

        }

        // --------- retorno ----------
        return McpResponse.Ok(
            new JsonObject
            {
                ["total"] = result.Total,
                ["hits"] = new JsonArray(
                    result.Hits
                        .Select(h => JsonNode.Parse(
                            JsonSerializer.Serialize(h.Source)
                        )!)
                        .ToArray()
                )
            }
        );
    }
}
