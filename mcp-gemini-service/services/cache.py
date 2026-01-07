import time
from typing import Any, Dict, Tuple


class TTLCache:
    """
    Simple in-memory TTL cache. Suitable for small datasets and single-process
    usage. Not thread-safe by design; FastAPI's default async server model is
    sufficient for this lightweight usage.
    """

    def __init__(self, ttl_seconds: int = 60, max_items: int = 100) -> None:
        self.ttl_seconds = ttl_seconds
        self.max_items = max_items
        self._store: Dict[str, Tuple[float, Any]] = {}

    def get(self, key: str) -> Any:
        now = time.time()
        item = self._store.get(key)
        if not item:
            return None
        expires_at, value = item
        if expires_at < now:
            self._store.pop(key, None)
            return None
        return value

    def set(self, key: str, value: Any) -> None:
        if len(self._store) >= self.max_items:
            self._evict_oldest()
        self._store[key] = (time.time() + self.ttl_seconds, value)

    def _evict_oldest(self) -> None:
        if not self._store:
            return
        # Remove the first expired item or the oldest entry.
        oldest_key = min(self._store, key=lambda k: self._store[k][0])
        self._store.pop(oldest_key, None)

