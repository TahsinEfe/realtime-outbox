using Microsoft.EntityFrameworkCore;
using RealtimeOutbox.ChatService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ChatDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("ChatDb")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok("ChatService is running..."));



app.MapControllers();
app.Run();
