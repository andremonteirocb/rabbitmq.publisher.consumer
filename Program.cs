using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Fundamentos.RabbitMQ.Generico.Core.Infrastructure.Queue;
using Fundamentos.RabbitMQ.Generico.Extensions;
using Fundamentos.RabbitMQ.Generico.Core.Infrastructure;
using Fundamentos.RabbitMQ.Generico.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton((sp) =>
    new ConnectionFactory()
    {
        //HostName = builder.Configuration["RabbitMqConfig:Host"],
        Uri = new Uri(builder.Configuration["RabbitMqConfig:Host"])
    }
);

builder.Services.AddSingletonWithRetry<IConnection, BrokerUnreachableException>(sp => sp.GetRequiredService<ConnectionFactory>().CreateConnection());
builder.Services.AddTransientWithRetry<IModel, Exception>(sp => sp.GetRequiredService<IConnection>().CreateModel());

builder.Services.AddTransient<Publisher>();
builder.Services.AddTransient<Consumer<Message>>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var model = app.GetService<IModel>();
model.ExchangeDeclare(app.Configuration["RabbitMqConfig:FanoutExchange"], ExchangeType.Fanout, true);
model.QueueDeclare(app.Configuration["RabbitMqConfig:Queue"], true, false, false, null);
model.QueueBind(app.Configuration["RabbitMqConfig:Queue"], app.Configuration["RabbitMqConfig:FanoutExchange"], string.Empty);

app.GetService<Consumer<Message>>()
    .QueueBind(app.Configuration["RabbitMqConfig:Queue"], 2, Dispatch);

void Dispatch(Message message)
{
    Console.WriteLine(message.Serialize().ToByteArray().ToReadOnlyMemory());
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();