using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using RabbitMQ.Client;

namespace MVCAttendEase.Services
{
    public class RabbitMQService
    {
        private readonly IConfiguration _config;

        public RabbitMQService(IConfiguration config)
        {
            _config = config;
        }

        private async Task<IChannel> CreateChannelAsync()
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_config["RabbitMQ:Uri"])
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            return channel;
        }

        public async Task RegisterUserAsync(string username)
        {
            var channel = await CreateChannelAsync();

            var queueName = $"chat_{username}";

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );

            await channel.ExchangeDeclareAsync(
                exchange: "broadcast_exchange",
                type: ExchangeType.Fanout
            );

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: "broadcast_exchange",
                routingKey: ""
            );
        }

        public async Task SendMessageAsync<T>(string receiver, T message)
        {
            var channel = await CreateChannelAsync();

            var queueName = $"chat_{receiver}";

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body
            );
        }

        // ✅ Receive Message (Pull)
        public async Task<T?> ReceiveMessageAsync<T>(string username)
        {
            var channel = await CreateChannelAsync();

            var queueName = $"chat_{username}";

            var result = await channel.BasicGetAsync(queueName, autoAck: true);

            if (result == null)
                return default;

            var json = Encoding.UTF8.GetString(result.Body.ToArray());

            return JsonSerializer.Deserialize<T>(json);
        }

        // ✅ Broadcast Message (Fanout)
        public async Task SendBroadcastMessageAsync<T>(T message)
        {
            var channel = await CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: "broadcast_exchange",
                type: ExchangeType.Fanout
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(
                exchange: "broadcast_exchange",
                routingKey: "",
                body: body
            );
        }

    }
}