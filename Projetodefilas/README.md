# espelho-banco-de-dados

API mínima em .NET 9 para publicar mensagens no RabbitMQ, totalmente instrumentada com OpenTelemetry para envio de logs/traces para OpenSearch (via OTLP). Inclui camada MCP para ferramentas de inspeção (ex.: checar saúde do Rabbit) a ser consumida por um serviço de “bombeiro” com Gemini.

## Como rodar rapidamente
1. Instale o .NET 9 SDK e um broker RabbitMQ acessível (`localhost:5672` por padrão).
2. Ajuste `src/ProjetoDeFilas.WebApi/appsettings.json` para o seu host Rabbit e endpoint OTLP (coletor → OpenSearch).
3. Restaure e rode a API:
   ```powershell
   dotnet restore
   dotnet run --project src/ProjetoDeFilas.WebApi
   ```
4. Publique uma mensagem:
   ```bash
   curl -X POST http://localhost:5024/api/publish \
     -H "Content-Type: application/json" \
     -d '{ "queueName": "fila.teste", "message": "hello" }'
   ```
5. Verifique `/healthz` para health check. Logs/traces aparecem no console e (se configurado) no OTLP.

## MCP
- Middleware HTTP em `/mcp` que despacha para ferramentas registradas.
- Tools incluídas: `rabbitmq.status` (valida conexão) e `elastic.search-example` (dica de consulta).
- Para testar via script: veja `scripts/test-mcp.ps1` ou `docs/VALIDACAO_MCP.md`.

## Estrutura
Segue a hierarquia solicitada (API, MCP, docs, scripts, workers) com projetos adicionados à solução `ProjetoDeFilas.sln`. Consulte `docs/technical-overview.md` para detalhes de design.


