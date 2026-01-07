import asyncio
import logging
import time
from typing import Any, Dict, List, Tuple

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from config.settings import settings
from models.response import AskRequest, AskResponse
from services.cache import TTLCache
from services.gemini_service import GeminiService
from services.mcp_client import MCPClient, MCPError


logging.basicConfig(
    level=logging.DEBUG,
    format="%(asctime)s %(levelname)s [%(name)s] %(message)s",
)
logger = logging.getLogger("mcp-gemini-service")

app = FastAPI(
    title="MCP + Gemini CDC Firefighter",
    description="Assistente de monitoramento para CDC (PostgreSQL + RabbitMQ) usando Gemini e MCP.",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

gemini_service = GeminiService()
mcp_client = MCPClient(settings.mcp_server_url)
cache = TTLCache(ttl_seconds=settings.cache_ttl_seconds, max_items=200)


def choose_tools(pergunta: str) -> List[Tuple[str, Dict[str, Any]]]:
    """
    Roteador simples baseado em palavras-chave para escolher ferramentas MCP.
    Pode ser substituído por uma cadeia mais sofisticada (LLM planner).
    """

    p = pergunta.lower()
    tools: List[Tuple[str, Dict[str, Any]]] = []

    if any(k in p for k in ["erro", "problema", "falha"]):
        tools.append(("logs.recent_errors", {"tool":"logs.search","payload":{"query":"error OR exception OR fail","sort":{"@timestamp":"desc"},"size":10}}))
        tools.append(("errors.summary", {"tool":"logs.search","payload":{"query":"error OR exception OR fail","sort":{"@timestamp":"desc"},"size":10}}))

    if any(k in p for k in ["último registro", "ultimo registro", "última operação", "ultima operação"]):
        tools.append(("data.last_inserted", {"tool":"logs.search","payload":{"query":"*","sort":{"@timestamp":"desc"},"size":1}}))

    if any(k in p for k in ["quantos", "quantidade", "qtd", "contagem", "dados hoje"]):
        tools.append(("data.count_today", {"tool":"logs.search","payload":{"query":"*","range":{"@timestamp":{"gte":"now/d"}},"size":0}}))
        tools.append(("data.count_by_operation", {"tool":"logs.search","payload":{"query":"*","range":{"@timestamp":{"gte":"now/d"}},"size":0}}))

    if "rabbit" in p:
        tools.append(("rabbitmq.status", {"tool":"rabbit.status","payload":{}}))

    if "worker" in p or "status" in p:
        tools.append(("worker.status", {}))

    if "cdc" in p or "listener" in p:
        tools.append(("cdc.listeners_status", {}))

    # fallback mínimo
    if not tools:
        tools.append(("logs.search", {"tool": "logs.search","payload": {"query": "*"}}))
    return tools


async def call_with_cache(tool: str, params: Dict[str, Any]) -> Dict[str, Any]:
    cache_key = f"{tool}:{str(params)}"
    if settings.cache_enabled:
        cached = cache.get(cache_key)
        if cached is not None:
            return cached

    result = await mcp_client.call_tool(tool, params)

    if settings.cache_enabled:
        cache.set(cache_key, result)

    return result


@app.post("/perguntar", response_model=AskResponse)
@app.post("/ask", response_model=AskResponse)
async def perguntar(body: AskRequest) -> AskResponse:
    try:
        start = time.perf_counter()
        logger.info("Recebida pergunta: %s", body.pergunta)
        tools = choose_tools(body.pergunta)
        logger.info("Ferramentas escolhidas: %s", [t[0] for t in tools])
        ferramentas_usadas: List[str] = []
        dados_coletados: Dict[str, Any] = {}
        logs_tecnicos: List[Dict[str, Any]] = []

        # Coleta paralela das ferramentas MCP
        async def run_tool(tool_name: str, params: Dict[str, Any]) -> None:
            nonlocal dados_coletados, logs_tecnicos, ferramentas_usadas
            try:
                logger.info("Chamando ferramenta MCP: %s com params: %s", tool_name, params)
                result = await call_with_cache(tool_name, params)
                logger.info("Resposta da ferramenta %s recebida", tool_name)
                dados_coletados[tool_name] = result
                ferramentas_usadas.append(tool_name)
                if isinstance(result, list):
                    logs_tecnicos.extend(result)
                elif isinstance(result, dict):
                    logs_tecnicos.append(result)
            except MCPError as exc:
                logger.error("Erro ao executar %s: %s", tool_name, exc, exc_info=True)
                dados_coletados[tool_name] = {"erro": str(exc)}
            except Exception as exc:
                logger.error("Erro inesperado ao executar %s: %s", tool_name, exc, exc_info=True)
                dados_coletados[tool_name] = {"erro": f"Erro inesperado: {str(exc)}"}

        await asyncio.gather(*(run_tool(t, p) for t, p in tools))
        logger.info("Dados coletados: %s", dados_coletados)

        try:
            logger.info("Chamando Gemini com pergunta e dados coletados")
            resposta_ia = await gemini_service.answer(body.pergunta, dados_coletados)
            logger.info("Resposta do Gemini recebida com sucesso")
        except Exception as exc:
            logger.error("Falha ao obter resposta do Gemini: %s", exc, exc_info=True)
            raise HTTPException(status_code=502, detail=f"Erro ao gerar resposta com Gemini: {str(exc)}")

        elapsed_ms = int((time.perf_counter() - start) * 1000)
        return AskResponse(
            resposta_ia=resposta_ia,
            logs_tecnicos=logs_tecnicos,
            ferramentas_usadas=ferramentas_usadas,
            tempo_resposta_ms=elapsed_ms,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Erro não tratado no endpoint /perguntar: %s", exc, exc_info=True)
        raise HTTPException(status_code=500, detail=f"Erro interno do servidor: {str(exc)}")


@app.get("/health")
async def health() -> Dict[str, str]:
    return {"status": "ok", "mcp_url": settings.mcp_server_url}

