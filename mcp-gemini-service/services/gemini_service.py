import asyncio
import logging
from typing import Any, Dict, List

import google.generativeai as genai

from config.settings import settings

logger = logging.getLogger(__name__)


class GeminiService:
    def __init__(self) -> None:
        if not settings.gemini_api_key:
            raise ValueError("GEMINI_API_KEY não configurada.")
        genai.configure(api_key=settings.gemini_api_key)
        self.model = genai.GenerativeModel(model_name=settings.gemini_model)

    async def answer(self, pergunta: str, dados_coletados: Dict[str, Any]) -> str:
        """
        Chama o Gemini de forma assíncrona, fornecendo contexto dos dados coletados.
        """

        prompt = self._build_prompt(pergunta, dados_coletados)
        try:
            logger.debug("Chamando Gemini com modelo: %s", settings.gemini_model)
            logger.debug("Prompt (primeiros 500 chars): %s", prompt[:500])
            response = await asyncio.to_thread(self.model.generate_content, prompt)
            if not response:
                logger.error("Resposta vazia do Gemini")
                raise ValueError("Resposta vazia do Gemini")
            if not response.text:
                logger.error("Resposta do Gemini sem texto. Candidatos: %s", response.candidates)
                raise ValueError("Resposta do Gemini sem texto")
            logger.debug("Resposta do Gemini recebida: %s", response.text[:200])
            return response.text.strip()
        except Exception as exc:  # broad catch to fallback gracefully
            logger.exception("Falha ao chamar Gemini: %s", exc)
            raise

    def _build_prompt(self, pergunta: str, dados_coletados: Dict[str, Any]) -> str:
        return (
            "Você é um assistente de monitoramento de um sistema que insere dados no rabbitmq, você consumira o MCP Server para obter informações sobre o sistema."
            "RabbitMQ e uma api .NET que faz o insert dos dados no rabbitmq. Responda em português brasileiro, de forma clara e amigável, sempre "
            "apresentando evidências concisas. Use os dados técnicos fornecidos, que podem vir do MCP Server. "
            "Se algo estiver vazio ou faltar, deixe claro. E sempre apresente os logs técnicos coletados.\n\n"
            f"Pergunta do usuário: {pergunta}\n\n"
            f"Dados coletados (JSON): {dados_coletados}\n\n"
            "Devolva apenas o texto da resposta final, sem JSON."
        )

