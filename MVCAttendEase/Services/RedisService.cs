using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using StackExchange.Redis;
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

        // FIX: Cast TimeSpan to Expiration for newer StackExchange.Redis versions
        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            if (expiry.HasValue)
                await _db.StringSetAsync(key, value, (Expiration)expiry.Value);
            else
                await _db.StringSetAsync(key, value);
        }

        // Get value by key — returns null if key does not exist or has expired
        public async Task<string?> GetAsync(string key)
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        // Delete a key from Redis
        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        // Check if a key exists in Redis
        public async Task<bool> ExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }
// ─────────────────────────────────────────────
        //  Generic GetOrSet<T>
        //  Returns cached value; if missing, calls factory,
        //  stores result, then returns it.
        // ─────────────────────────────────────────────

        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiry = null) where T : class
        {
            // 1. Try cache first
            var cached = await GetAsync(key);
            if (cached != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(cached);
                }
                catch
                {
                    // Corrupt cache entry — fall through to DB
                    await DeleteAsync(key);
                }
            }

            // 2. Cache miss → fetch from DB
            var data = await factory();
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                await SetAsync(key, json, expiry ?? TimeSpan.FromHours(1));
            }
            return data;
        }

        // Synchronous overload (for non-async repository methods)
        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<T> factory,
            TimeSpan? expiry = null) where T : class
        {
            var cached = await GetAsync(key);
            if (cached != null)
            {
                try { return JsonSerializer.Deserialize<T>(cached); }
                catch { await DeleteAsync(key); }
            }

            var data = factory();
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data);
                await SetAsync(key, json, expiry ?? TimeSpan.FromHours(1));
            }
            return data;
        }

        // ─────────────────────────────────────────────
        //  Pattern-based bulk invalidation
        //  Scans for keys matching a pattern and deletes them.
        //  Use when an employee's data changes.
        // ─────────────────────────────────────────────

        /// <summary>
        /// Deletes every Redis key whose name starts with <paramref name="prefix"/>.
        /// Example: InvalidateByPrefixAsync("emp:42:") removes all cached data for employee 42.
        /// </summary>
        public async Task InvalidateByPrefixAsync(string prefix)
        {
            var server  = _db.Multiplexer.GetServers().FirstOrDefault();
            if (server == null) return;

            var pattern = $"{prefix}*";
            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                await _db.KeyDeleteAsync(key);
            }
        }

        /// <summary>
        /// Convenience: invalidate ALL cached data for a single employee.
        /// Call this after CheckIn, CheckOut, or any attendance write.
        /// </summary>
        public Task InvalidateEmployeeAsync(int empId)
            => InvalidateByPrefixAsync(RedisKeys.EmployeePrefix(empId));
    }

    // ─────────────────────────────────────────────
    //  Centralised key names — one place to change them
    // ─────────────────────────────────────────────

    public static class RedisKeys
    {
        // Prefix for ALL keys belonging to one employee
        public static string EmployeePrefix(int empId) => $"emp:{empId}:";

        // ── Employee Panel – Dashboard ──
        public static string DashboardData(int empId)
            => $"emp:{empId}:dashboard";

        // ── Employee Panel – Report: chart + grid for a given year ──
        public static string ReportYearData(int empId, int year)
            => $"emp:{empId}:report:year:{year}";

        // ── Employee Panel – Report: available years ──
        public static string AttendanceYears(int empId)
            => $"emp:{empId}:years";

        // ── Attendance Module – grid list (all records) ──
        public static string AttendanceGrid(int empId)
            => $"emp:{empId}:attendance:grid";

        // ── Attendance Module – yearly chart (working hours per month) ──
        public static string YearlyWorkingHours(int empId, int year)
            => $"emp:{empId}:working:yearly:{year}";

        // ── Attendance Module – monthly chart (working hours per day) ──
        public static string MonthlyWorkingHours(int empId, int month, int year)
            => $"emp:{empId}:working:monthly:{year}:{month}";

        // ── Attendance Module – calendar view ──
        public static string AttendanceByYear(int empId, int year)
            => $"emp:{empId}:attendance:year:{year}";
    }
}
