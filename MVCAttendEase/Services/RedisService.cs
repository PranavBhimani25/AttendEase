using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text.Json;

namespace MVCAttendEase.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;
        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SetAsync(string key, string value)
        {
            await _db.StringSetAsync(key, value);
        }

        public async Task<string> GetAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            return await _db.StringIncrementAsync(key, value);
        }

        public async Task<long> GetInt64Async(string key)
        {
            var val = await _db.StringGetAsync(key);
            return val.HasValue ? (long)val : 0;
        }

        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        // Redis List operations for notifications
        public async Task<long> RPushAsync(string key, string value)
        {
            return await _db.ListRightPushAsync(key, value);
        }

        public async Task<RedisValue[]> LRangeAsync(string key, long start, long stop)
        {
            return await _db.ListRangeAsync(key, start, stop);
        }

        public async Task<long> LLenAsync(string key)
        {
            return await _db.ListLengthAsync(key);
        }

        public async Task TrimAsync(string key, long start, long stop)
        {
            await _db.ListTrimAsync(key, start, stop);
        }

        // Remove specific value from list (for individual read)
        public async Task<long> LRemAsync(string key, string value)
        {
            return await _db.ListRemoveAsync(key, value, flags: CommandFlags.None);
        }
    }
}
