using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using RabbitMQ.Client;

namespace MVCAttendEase.Services
{
    public class NotificationPublisher
    {
        private readonly ConnectionFactory _factory;
        private readonly RabbitMQConfig _config;

        public NotificationPublisher(IOptions<RabbitMQConfig> options)
        {
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_config.Uri))
            {
                throw new ArgumentException("RabbitMQ Uri must be configured.", nameof(options));
            }

            _factory = new ConnectionFactory
            {
                Uri = new Uri(_config.Uri),
                AutomaticRecoveryEnabled = true
            };
        }

        private string NotificationQueue =>
            string.IsNullOrWhiteSpace(_config.NotificationQueueName)
                ? _config.QueueName
                : _config.NotificationQueueName!;

        public async Task PublishAsync(NotificationMessage message)
        {
            await using var connection = await _factory.CreateConnectionAsync(CancellationToken.None);
            await using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(false, false, null, null), CancellationToken.None);

            await channel.QueueDeclareAsync(
                queue: NotificationQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                passive: false,
                noWait: false,
                cancellationToken: CancellationToken.None);

            var payload = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(payload);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: NotificationQueue,
                body: body.AsMemory(),
                cancellationToken: CancellationToken.None);
        }
        public async Task PublishAttendanceAsync(NotificationMessage message)
        {
            await using var connection = await _factory.CreateConnectionAsync(CancellationToken.None);
            await using var channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(false, false, null, null), CancellationToken.None);

            var queueName = string.IsNullOrWhiteSpace(_config.AttendanceNotificationQueueName)
                ? "attendance_notifications"
                : _config.AttendanceNotificationQueueName!;

            await channel.QueueDeclareAsync(
                queue:      queueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null,
                passive:    false,
                noWait:     false,
                cancellationToken: CancellationToken.None);

            var payload = JsonSerializer.Serialize(message);
            var body    = Encoding.UTF8.GetBytes(payload);

            await channel.BasicPublishAsync(
                exchange:   string.Empty,
                routingKey: queueName,
                body:       body.AsMemory(),
                cancellationToken: CancellationToken.None);
        }
    }
}
