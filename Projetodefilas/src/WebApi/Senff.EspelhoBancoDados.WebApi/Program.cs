using McpDotNet;
using McpServer;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;
using Serilog.Sinks.OpenSearch;
using System.Diagnostics;
using System.Text;

// -----------------------------------------------------
// 1️⃣ Configurar Serilog para enviar logs para OpenSearch
// -----------------------------------------------------

Serilog.Debugging.SelfLog.Enable(msg =>
{
    File.AppendAllText("serilog-selflog.txt", msg);
});

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.OpenSearch(new OpenSearchSinkOptions(new Uri("http://localhost:9200"))
    {
        IndexFormat = "projetofila-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
        NumberOfShards = 1,
        NumberOfReplicas = 1,
        BatchPostingLimit = 50,
        Period = TimeSpan.FromSeconds(5)
    })
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// -----------------------------------------------------
// 2️⃣ Serviços e configurações padrão
// -----------------------------------------------------
builder.Services.AddMcpServer();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();


builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

// -----------------------------------------------------
// 3️⃣ Middleware
// -----------------------------------------------------
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseMcpServer();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapHealthChecks("/healthz");
app.MapControllers();

// -----------------------------------------------------
// 4️⃣ Endpoint para publicar mensagem no RabbitMQ
// -----------------------------------------------------
app.MapPost("/api/publish", async Task<IResult> (PublishMessageRequest request, RabbitMqPublisher publisher, CancellationToken ct) =>
{
    try
    {
        await publisher.PublishAsync(request.QueueName, request.Message, ct);
        return Results.Accepted();
    }
    catch (BrokerUnreachableException ex)
    {
        Log.Error(ex, "RabbitMQ indisponível ao publicar via controller na fila {Queue}", request.QueueName);
        return Results.Problem(
            title: "Falha ao conectar ao RabbitMQ",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro interno ao publicar via controller na fila {Queue}", request.QueueName);
        return Results.Problem(
            title: "Erro interno ao publicar mensagem",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
})
.WithName("PublishMessage")
.WithSummary("Publica uma mensagem em uma fila RabbitMQ.")
.WithOpenApi();

app.Run();

// -----------------------------------------------------
// 5️⃣ Models e classes auxiliares
// -----------------------------------------------------
public record PublishMessageRequest(string QueueName, string Message);

public record RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public bool DurableQueue { get; init; } = true;
}

public sealed class RabbitMqPublisher
{
    public const string ActivitySourceName = "RabbitMqPublisher";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
    }

    public Task PublishAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", queueName);

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName,
                durable: _options.DurableQueue,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);
            var props = channel.CreateBasicProperties();
            props.Persistent = _options.DurableQueue;

            channel.BasicPublish(string.Empty, queueName, props, body);

            Log.Information("Mensagem publicada na fila {Queue} (tamanho {Length})", queueName, message.Length);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao publicar na fila {Queue}", queueName);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }
}
