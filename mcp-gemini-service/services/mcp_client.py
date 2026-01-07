import json
import logging
from typing import Any, Dict, Optional

import httpx


logger = logging.getLogger(__name__)


class MCPError(Exception):
    """Raised when the MCP server responds with an error."""


class MCPClient:
    def __init__(self, base_url: str, timeout: float = 10.0) -> None:
        self.base_url = base_url
        self.timeout = timeout
        # Aceita certificados SSL auto-assinados para desenvolvimento local
        self._client = httpx.AsyncClient(timeout=self.timeout, verify=False)

    async def call_tool(self, method: str, params: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        # O payload já vem no formato correto: {"tool": "...", "payload": {...}}
        # Envia diretamente sem wrapper JSON-RPC 2.0
        payload = params or {}

        try:
            logger.debug("Enviando requisição MCP para %s: %s", self.base_url, payload)
            response = await self._client.post(self.base_url, json=payload)
            logger.debug("Resposta MCP recebida: status=%s, body=%s", response.status_code, response.text[:500])
        except httpx.ConnectError as exc:
            logger.error("Erro de conexão ao MCP (%s): %s", self.base_url, exc)
            raise MCPError(f"Falha ao conectar ao MCP em {self.base_url}: {exc}") from exc
        except httpx.TimeoutException as exc:
            logger.error("Timeout ao chamar MCP: %s", exc)
            raise MCPError(f"Timeout ao chamar MCP: {exc}") from exc
        except httpx.RequestError as exc:
            logger.error("Erro ao chamar MCP: %s", exc, exc_info=True)
            raise MCPError(f"Falha na requisição ao MCP: {exc}") from exc

        if response.status_code >= 400:
            logger.error("MCP retornou status HTTP %s: %s", response.status_code, response.text)
            raise MCPError(f"MCP HTTP {response.status_code}: {response.text}")

        try:
            data = response.json()
        except json.JSONDecodeError as exc:
            logger.error("Resposta inválida do MCP: %s", response.text)
            raise MCPError(f"Resposta inválida do MCP (JSON malformado): {response.text[:200]}") from exc

        # Verifica o formato de resposta do MCP: {"success": bool, "error": null/obj, "data": {...}}
        if isinstance(data, dict):
            # Verifica se a requisição foi bem-sucedida
            success = data.get("success", True)
            error_obj = data.get("error")
            
            # Se success for False ou error não for null, trata como erro
            if not success or error_obj is not None:
                error_message = "Erro desconhecido do MCP"
                if error_obj:
                    if isinstance(error_obj, dict):
                        error_message = error_obj.get("message", str(error_obj))
                    else:
                        error_message = str(error_obj)
                else:
                    error_message = data.get("message", "Erro no MCP")
                
                logger.error("Erro do MCP (%s): %s", method, error_message)
                raise MCPError(f"MCP Error: {error_message}")
            
            # Extrai o resultado de "data" se existir, senão retorna o objeto inteiro
            if "data" in data:
                result = data["data"]
                logger.debug("Resultado MCP para %s: %s", method, str(result)[:200])
                return result
            else:
                # Se não tiver "data", retorna o objeto inteiro (caso o formato seja diferente)
                logger.debug("Resultado MCP para %s: %s", method, str(data)[:200])
                return data
        
        # Se não for dict, retorna diretamente (pode ser list, etc)
        logger.debug("Resultado MCP para %s: %s", method, str(data)[:200])
        return data

    async def close(self) -> None:
        await self._client.aclose()

