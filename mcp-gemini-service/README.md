# MCP + Gemini CDC Firefighter 🚒

Serviço FastAPI que integra Gemini e o MCP Server (.NET) para responder perguntas em português sobre o pipeline de Change Data Capture (CDC), sempre incluindo dados técnicos de suporte.

## Requisitos

- Python 3.10+
- Variáveis de ambiente:
  - `GEMINI_API_KEY` (obrigatório)
  - `GEMINI_MODEL` (opcional, padrão `gemini-1.5-pro`)
  - `MCP_SERVER_URL` (padrão `http://localhost:5184/mcp`)
  - `CACHE_TTL_SECONDS` (padrão `60`)
  - `CACHE_ENABLED` (padrão `true`)

## Instalação

```bash
cd mcp-gemini-service
python -m venv .venv
source .venv/bin/activate  # ou .venv\\Scripts\\activate no Windows
pip install -r requirements.txt
```

## Execução

```bash
uvicorn main:app --reload --port 8000
```

## Endpoint principal

- `POST /perguntar` (alias `/ask`)
  - Body: `{"pergunta": "ocorreu algum problema hoje?"}`
  - Resposta:
    ```json
    {
      "resposta_ia": "🤖 ...",
      "logs_tecnicos": [ ... ],
      "ferramentas_usadas": ["logs.recent_errors", "errors.summary"],
      "tempo_resposta_ms": 1200
    }
    ```

- `GET /health` para verificação rápida.

## Como funciona

1. A pergunta é analisada por um roteador simples de palavras-chave que decide quais ferramentas MCP chamar.
2. O cliente MCP envia requisições HTTP POST no formato `{"tool": "...", "payload": {...}}` para `MCP_SERVER_URL`.
3. O MCP retorna respostas no formato `{"success": true, "error": null, "data": {...}, "correlationId": null}`.
4. Os resultados coletados são enviados ao Gemini para compor uma resposta amigável em português.
5. A resposta sempre inclui logs/dados técnicos e a lista de ferramentas usadas.

## Formato de requisição ao MCP

O serviço envia requisições no seguinte formato:

```json
{
  "tool": "logs.search",
  "payload": {
    "query": "*",
    "sort": {"@timestamp": "desc"},
    "size": 1
  }
}
```

### Exemplos de requisições MCP

**Buscar erros recentes:**
```bash
curl --request POST \
  --url http://localhost:5184/mcp \
  --header 'Content-Type: application/json' \
  --data '{"tool":"logs.search","payload":{"query":"error OR exception OR fail","sort":{"@timestamp":"desc"},"size":10}}'
```

**Status do RabbitMQ:**
```bash
curl --request POST \
  --url http://localhost:5184/mcp \
  --header 'Content-Type: application/json' \
  --data '{"tool":"rabbit.status","payload":{}}'
```

**Último registro inserido:**
```bash
curl --request POST \
  --url http://localhost:5184/mcp \
  --header 'Content-Type: application/json' \
  --data '{"tool":"logs.search","payload":{"query":"*","sort":{"@timestamp":"desc"},"size":1}}'
```

### Formato de resposta do MCP

O MCP retorna respostas no seguinte formato:

```json
{
  "success": true,
  "message": null,
  "error": null,
  "data": {
    "total": 13,
    "hits": [...]
  },
  "correlationId": null
}
```

## Ferramentas esperadas no MCP

- `logs.search`, `logs.count`, `logs.recent_errors`, `logs.from_database`
- `worker.status`, `data.last_inserted`, `data.count_today`, `data.count_by_operation`
- `errors.list`, `errors.summary`, `rabbitmq.status`, `cdc.listeners_status`

## Observações

- Há cache simples (TTL) para chamadas MCP frequentes.
- Erros do MCP ou do Gemini são registrados em log; respostas do Gemini retornam 502 se falharem.
- Ajuste o roteamento em `choose_tools` conforme a disponibilidade real das ferramentas no MCP.

