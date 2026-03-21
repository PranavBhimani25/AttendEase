using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Repositories.Interfaces;
using RabbitMQ.Client;
using System.Threading.Tasks;
using Repositories.Models;
using System.Text;
using StackExchange.Redis;


namespace Repositories.Implementation
{
    public class NotificationRepository : INotificationInterface
    {

        private readonly IConfiguration _configuration;
        public NotificationRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_configuration["RabbitMQ:Uri"]),
                AutomaticRecoveryEnabled = true
                // DispatchConsumersAsync = true
            };

            return await factory.CreateConnectionAsync();
        }

        public async Task AdminSend(IConnection con, MsgToEmp model)
        {
            await using var channel = await con.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("AdminToEmp", ExchangeType.Direct);

            await channel.QueueDeclareAsync(
                queue: model.Receiver,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            await channel.QueueBindAsync(model.Receiver, "AdminToEmp", model.Receiver, null);

            var json = JsonSerializer.Serialize(model);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: "AdminToEmp",
                routingKey: model.Receiver,
                mandatory: false,
                basicProperties: properties,
                body: body
            );
        }




        public async Task GetEmpNotification(IConnection con, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            await using var channel = await con.CreateChannelAsync();

            // Ensure the queue exists
            await channel.QueueDeclareAsync(userId, false, false, false, null);

            await using var redis = await ConnectionMultiplexer.ConnectAsync(GetRedisOptions());
            var db = redis.GetDatabase();
            string redisKey = $"AdminToEmp:{userId}";

            BasicGetResult result;

            // Loop until the queue is empty
            while ((result = await channel.BasicGetAsync(userId, autoAck: true)) != null)
            {
                byte[] bodyBytes = result.Body.ToArray();
                var msgString = Encoding.UTF8.GetString(bodyBytes);
                await db.ListRightPushAsync(redisKey, msgString);
            }
        }

        public async Task<List<MsgToEmp>> GetStoredNotifications(string userId)
        {
            var notifications = new List<MsgToEmp>();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return notifications;
            }

            await using var redis = await ConnectionMultiplexer.ConnectAsync(GetRedisOptions());
            var db = redis.GetDatabase();
            string redisKey = $"AdminToEmp:{userId}";
            var values = await db.ListRangeAsync(redisKey);

            foreach (var value in values)
            {
                if (value.IsNullOrEmpty)
                {
                    continue;
                }

                var message = JsonSerializer.Deserialize<MsgToEmp>(value!);
                if (message != null)
                {
                    notifications.Add(message);
                }
            }

            return notifications
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public async Task<List<MsgToEmp>> GetAdminSentNotifications()
        {
            await using var redis = await ConnectionMultiplexer.ConnectAsync(GetRedisOptions());
            var db = redis.GetDatabase();
            var values = await db.ListRangeAsync("AdminSentNotifications");

            return values
                .Where(x => !x.IsNullOrEmpty)
                .Select(x => JsonSerializer.Deserialize<MsgToEmp>(x!))
                .Where(x => x != null)
                .OrderByDescending(x => x!.CreatedAt)
                .Cast<MsgToEmp>()
                .ToList();
        }

        public async Task<bool> MarkNotificationAsRead(string userId, MsgToEmp model)
        {
            if (string.IsNullOrWhiteSpace(userId) || model == null)
            {
                return false;
            }

            await using var redis = await ConnectionMultiplexer.ConnectAsync(GetRedisOptions());
            var db = redis.GetDatabase();
            string redisKey = $"AdminToEmp:{userId}";
            var values = await db.ListRangeAsync(redisKey);

            foreach (var value in values)
            {
                if (value.IsNullOrEmpty)
                {
                    continue;
                }

                var storedMessage = JsonSerializer.Deserialize<MsgToEmp>(value!);
                if (storedMessage == null)
                {
                    continue;
                }

                if (storedMessage.Sender == model.Sender &&
                    storedMessage.Receiver == model.Receiver &&
                    storedMessage.Message == model.Message &&
                    Math.Abs((storedMessage.CreatedAt - model.CreatedAt).TotalSeconds) < 1)
                {
                    var removedCount = await db.ListRemoveAsync(redisKey, value, 1);
                    return removedCount > 0;
                }
            }

            return false;
        }

        private ConfigurationOptions GetRedisOptions()
        {
            var redisOptions = new ConfigurationOptions
            {
                Password = _configuration["Redis:Password"],
                Ssl = bool.TryParse(_configuration["Redis:Ssl"], out var useSsl) && useSsl,
                AbortOnConnectFail = false,
                ConnectTimeout = 10000,
                SyncTimeout = 10000
            };

            redisOptions.EndPoints.Add(
                _configuration["Redis:Host"],
                int.TryParse(_configuration["Redis:Port"], out var redisPort) ? redisPort : 6379
            );

            return redisOptions;
        }

    }

}
