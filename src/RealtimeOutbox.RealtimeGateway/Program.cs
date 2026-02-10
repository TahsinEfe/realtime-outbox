using RealtimeOutbox.RealtimeGateway;
using RealtimeOutbox.OutboxWorker;
using RealtimeOutbox.RealtimeGateway.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<RabbitMqConsumerWorker>();
builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true) 
            .AllowCredentials();
    });
});



var app = builder.Build();

app.UseCors("DevCors");
app.MapHub<ChatHub>("/hubs/chat");
app.MapGet("/", () => "RealtimeGateway running");
app.Run();
