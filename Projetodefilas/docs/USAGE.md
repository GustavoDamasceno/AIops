# Como usar a API

## Publicar mensagem
```
POST /api/publish
{
  "queueName": "fila.teste",
  "message": "conteudo aqui"
}
```
- Retorno: `202 Accepted` em sucesso; erros geram ProblemDetails.

## Health
- `GET /healthz`

## Configurar RabbitMQ
- Ajuste `RabbitMq` em `appsettings.json` ou via environment:
  - `RabbitMq__HostName`, `RabbitMq__Port`, `RabbitMq__UserName`, `RabbitMq__Password`, `RabbitMq__VirtualHost`.

## OpenTelemetry / OpenSearch
- Logs e traces saem no console.
- Para enviar ao OTLP: defina `OpenTelemetry__Otlp__Endpoint` (ex.: `http://otel-collector:4317`).
- No collector, exporte para OpenSearch ou Data Prepper. Consulte a tool `elastic.search-example` para query sugerida.


