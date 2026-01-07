from typing import Any, Dict, List, Optional

from pydantic import BaseModel


class AskRequest(BaseModel):
    pergunta: str


class AskResponse(BaseModel):
    resposta_ia: str
    logs_tecnicos: List[Dict[str, Any]]
    ferramentas_usadas: List[str]
    tempo_resposta_ms: int
    erro: Optional[str] = None

