param(
    [string]$Endpoint = "http://localhost:5024/mcp"
)

$payload = @{
    tool = "rabbitmq.status"
    payload = @{
        host = "localhost"
    }
    correlationId = "ps1-demo"
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Method Post -Uri $Endpoint -ContentType "application/json" -Body $payload | ConvertTo-Json -Depth 6


