# Validação rápida do MCP

## Curl
```
curl -X POST http://localhost:5024/mcp \
  -H "Content-Type: application/json" \
  -d '{ "tool": "rabbitmq.status", "payload": { "host": "localhost" } }'
```

## Esperado
- `200 OK` com `data.isConnected = true` em caso de sucesso.
- Logs no console indicando tool executada.

## Problemas comuns
- Broker indisponível → `400 BadRequest` + `rabbit_connection_error`.
- Tool não registrada → `404 NotFound`.
- Timeout/cancelamento → HTTP 499.


