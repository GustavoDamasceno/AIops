from functools import lru_cache
from pydantic import Field
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    gemini_api_key: str = Field("AIzaSyBGGsWMT_dizv8W2TkPLnYPzG4qBMOFSEg", env="GEMINI_API_KEY")
    gemini_model: str = Field("gemini-2.5-flash", env="GEMINI_MODEL")
    mcp_server_url: str = Field("http://localhost:5184/mcp", env="MCP_SERVER_URL")
    cache_ttl_seconds: int = Field(60, env="CACHE_TTL_SECONDS")
    cache_enabled: bool = Field(True, env="CACHE_ENABLED")

    class Config:
        env_file = ".env"
        case_sensitive = False


@lru_cache()
def get_settings() -> Settings:
    return Settings()


settings = get_settings()

