# Visão técnica

## API
- ASP.NET Core 9 minimal API.
- Endpoint `POST /api/publish`: recebe `queueName` e `message` e publica no RabbitMQ (durável opcional).
- Health check em `/healthz`.

## Observabilidade
- OpenTelemetry para logs e traces (AspNetCore + HttpClient + publisher).
- Exporters habilitados: console; OTLP opcional via `OpenTelemetry:Otlp:Endpoint`.
- Sugestão de coletor → pipeline OTLP → OpenSearch (Data Prepper / AOT).

## MCP
- Middleware HTTP em `/mcp` com contrato simples `McpRequest` → `McpResponse`.
- Registry registra ferramentas (ex.: `rabbitmq.status`, `elastic.search-example`).
- Pensado para ser consumido por um serviço “bombeiro” com Gemini, evitando acoplamento direto.

## Workers
- Templates de Worker e Hangfire criados como placeholder para evoluções futuras.

## Dependências chave
- `RabbitMQ.Client`, `OpenTelemetry.*`, `Microsoft.AspNetCore.OpenApi`.


