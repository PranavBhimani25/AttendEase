using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MVCAttendEase.Models;

namespace MVCAttendEase.Services
{
    public class NotificationService
    {
        private readonly object _lock = new();
        private readonly List<Channel<NotificationMessage>> _subscribers = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private const string NOTIFICATIONS_KEY = "admin:notifications";

        public NotificationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // Helper to get RedisService in a fresh scope
        private RedisService GetRedis(IServiceScope scope)
            => scope.ServiceProvider.GetRequiredService<RedisService>();

        public IDisposable Subscribe(out ChannelReader<NotificationMessage> reader)
        {
            var channel = Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

            lock (_lock)
            {
                _subscribers.Add(channel);
            }

            reader = channel.Reader;
            return new NotificationSubscription(this, channel);
        }

        public async Task PublishAsync(NotificationMessage message)
        {
            using var scope = _scopeFactory.CreateScope();
            var redis = GetRedis(scope);

            var json = JsonSerializer.Serialize(message);
            var len = await redis.RPushAsync(NOTIFICATIONS_KEY, json);

            if (len > 100)
                await redis.TrimAsync(NOTIFICATIONS_KEY, 100, -1);

            await redis.IncrementAsync("admin:global:unread");

            // Push to all live SSE subscribers
            List<Channel<NotificationMessage>> subscribersCopy;
            lock (_lock)
            {
                subscribersCopy = new List<Channel<NotificationMessage>>(_subscribers);
            }

            foreach (var channel in subscribersCopy)
            {
                channel.Writer.TryWrite(message);
            }
        }

        public async Task<long> GetGlobalUnreadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            return await GetRedis(scope).GetInt64Async("admin:global:unread");
        }

        public async Task ResetGlobalUnreadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            await GetRedis(scope).DeleteAsync("admin:global:unread");
        }

        public async Task<bool> MarkAsReadAsync(int index)
        {
            using var scope = _scopeFactory.CreateScope();
            var redis = GetRedis(scope);

    var allValues = await redis.LRangeAsync(NOTIFICATIONS_KEY, -6, -1);
            if (index < 0 || index >= allValues.Length) return false;

// Mirror the index: frontend 0 = Redis last item
    int redisIndex = allValues.Length - 1 - index;

            var targetJson = allValues[redisIndex].ToString();
            if (string.IsNullOrEmpty(targetJson)) return false;

            var removed = await redis.LRemAsync(NOTIFICATIONS_KEY, targetJson);
            if (removed > 0)
            {
                await redis.IncrementAsync("admin:global:unread", -1);
                return true;
            }
            return false;
        }

        public async Task MarkAllReadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var redis = GetRedis(scope);
            await redis.DeleteAsync("admin:global:unread");
            await redis.DeleteAsync(NOTIFICATIONS_KEY);
        }

        public async Task<List<NotificationMessage>> LoadRecentAsync(int maxCount = 10)
        {
            using var scope = _scopeFactory.CreateScope();
            var values = await GetRedis(scope).LRangeAsync(NOTIFICATIONS_KEY, -maxCount, -1);

            var notifications = new List<NotificationMessage>();
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    var json = value.ToString();
                    if (string.IsNullOrWhiteSpace(json)) continue;  // ← empty skip karo

                    try
                    {
                        var msg = JsonSerializer.Deserialize<NotificationMessage>(json);
                        if (msg != null) notifications.Add(msg);
                    }
                    catch
                    {
                        // Corrupt JSON entry skip karo — baaki load hote rahein
                        continue;
                    }
                }
            }
            notifications.Reverse();
            return notifications;
        }

        private void Unsubscribe(Channel<NotificationMessage> channel)
        {
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }
            channel.Writer.TryComplete();
        }

        private class NotificationSubscription : IDisposable
        {
            private readonly NotificationService _service;
            private readonly Channel<NotificationMessage> _channel;
            private bool _disposed;

            public NotificationSubscription(NotificationService service, Channel<NotificationMessage> channel)
            {
                _service = service;
                _channel = channel;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _service.Unsubscribe(_channel);
            }
        }
    }
}