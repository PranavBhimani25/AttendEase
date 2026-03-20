using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MVCAttendEase.Services
{
    public class RabbitMqNotificationConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqNotificationConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMQConfig _config;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMqNotificationConsumer(
            IOptions<RabbitMQConfig> config,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMqNotificationConsumer> logger)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_config.Uri),
                    AutomaticRecoveryEnabled = true
                };

                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(new CreateChannelOptions(false, false, null, null), stoppingToken);

                var queueName = string.IsNullOrWhiteSpace(_config.NotificationQueueName)
                    ? _config.QueueName
                    : _config.NotificationQueueName!;

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonSerializer.Deserialize<NotificationMessage>(json);

                        if (message != null)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                            await notificationService.PublishAsync(message);
                            _logger.LogInformation("✅ Processed notification for {FullName} ({Email}) - persisted to Redis", message.FullName, message.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process RabbitMQ notification message.");
                    }
                    finally
                    {
                        if (!_channel.IsClosed)
                        {
                            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        }
                    }
                    _logger.LogInformation("Consumer started on queue {QueueName}", queueName);

                };

                await _channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // expected when stoppingToken is triggered
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer stopped unexpectedly.");
            }
            finally
            {
                await StopConsumerAsync();
            }
        }

        private async Task StopConsumerAsync()
        {
            try
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync(200, "Stopping", true, CancellationToken.None);
                }

                if (_connection != null)
                {
                    await _connection.CloseAsync(200, "Stopping", TimeSpan.FromSeconds(5), true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while shutting down RabbitMQ consumer.");
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
