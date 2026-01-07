using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client.Exceptions;

namespace Senff.EspelhoBancoDados.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(RabbitMqPublisher publisher, ILogger<MessagesController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Publica uma mensagem em uma fila RabbitMQ.
    /// </summary>
    /// <param name="request">Nome da fila e conteúdo da mensagem.</param>
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishMessageRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Recebida solicitação de publicação via controller para fila {Queue}", request.QueueName);

        try
        {
            await _publisher.PublishAsync(request.QueueName, request.Message, ct);
            return Accepted();
        }
        catch (BrokerUnreachableException ex)
        {
            _logger.LogError(ex, "RabbitMQ indisponível ao publicar via controller na fila {Queue}", request.QueueName);
            return Problem(
                title: "Falha ao publicar mensagem",
                detail: $"Não foi possível conectar ao RabbitMQ: {ex.Message}",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao publicar via controller na fila {Queue}", request.QueueName);
            return Problem(
                title: "Erro interno ao publicar mensagem",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}


