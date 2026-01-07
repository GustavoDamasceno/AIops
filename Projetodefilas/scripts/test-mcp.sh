#!/usr/bin/env bash

ENDPOINT=${1:-http://localhost:5024/mcp}

curl -s -X POST "$ENDPOINT" \
  -H "Content-Type: application/json" \
  -d '{ "tool": "rabbitmq.status", "payload": { "host": "localhost" }, "correlationId": "sh-demo" }' | jq


