using Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

//MassTransit Configuration
builder.Services.AddMassTransit(busConfigurator =>
{
    busConfigurator.AddBus(provider => Bus.Factory.CreateUsingRabbitMq());
});

//Project Context Configuration
builder.Services.AddDbContext<ProjectContext>(opt =>
    opt.UseSqlServer("Server=localhost;Database=BusManagerWithOutboxPatternDb;User=sa;Password=P@55w0rd")
);

builder.Services.AddHostedService<ReadOutboxMessageService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

#region etc | DEFINITIONS


namespace Events
{
    public class UserRegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

public class ProjectContext : DbContext
{
    public ProjectContext(DbContextOptions<ProjectContext> dbContextOptions)
        : base(dbContextOptions)
    {

    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
}

public class ReadOutboxMessageService : IHostedService
{
    private readonly IBus _bus;
    private readonly ILogger<ReadOutboxMessageService> _logger;
    private readonly ProjectContext _projectContext;

    public ReadOutboxMessageService(IBus bus
        , IServiceScopeFactory serviceScopeFactoryfactory
        , ILogger<ReadOutboxMessageService> logger)
    {
        _bus = bus;
        _logger = logger;
        _projectContext = serviceScopeFactoryfactory.CreateScope().ServiceProvider.GetRequiredService<ProjectContext>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Started DistributorApp");

        while (!cancellationToken.IsCancellationRequested)
        {
            var outBoxMessages = await _projectContext.OutboxMessages
                .ToListAsync(cancellationToken);

            foreach (var outboxMessage in outBoxMessages)
            {
                Type type = Assembly.GetAssembly(typeof(UserRegisterRequest)).GetType(outboxMessage.EventType);
                object eventBusData = System.Text.Json.JsonSerializer.Deserialize(outboxMessage.Payload, type);

                if (eventBusData is not null)
                    await _bus.Publish(eventBusData);

                _projectContext.Remove(outboxMessage);
            }

            await _projectContext.SaveChangesAsync(cancellationToken);

            await Task.Delay(1000);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogError($"Stoped DistributorApp");

        await Task.CompletedTask;
    }
}

#endregion
