from typing import Any, Dict, Optional

from pydantic import BaseModel, Field


class JSONRPCRequest(BaseModel):
    jsonrpc: str = "2.0"
    method: str
    id: str
    params: Dict[str, Any] = Field(default_factory=dict)


class JSONRPCError(BaseModel):
    code: int = -32000
    message: str
    data: Optional[Dict[str, Any]] = None


class JSONRPCResponse(BaseModel):
    jsonrpc: str = "2.0"
    result: Optional[Dict[str, Any]] = None
    error: Optional[JSONRPCError] = None
    id: str

