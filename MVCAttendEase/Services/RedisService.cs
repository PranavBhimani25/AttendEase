using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using StackExchange.Redis;

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
    }
}