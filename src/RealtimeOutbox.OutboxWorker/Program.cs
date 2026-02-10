using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.OutboxWorker;
using RealtimeOutbox.OutboxWorker.Data;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddDbContext<OutboxDbContext>(opt =>
            opt.UseNpgsql(ctx.Configuration.GetConnectionString("ChatDb")));

        services.AddHostedService<Worker>();
    })
    .Build();



await host.RunAsync();
