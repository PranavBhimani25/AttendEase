using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAttendEase.Models
{
    public class RedisConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public bool Ssl { get; set; }
    }
}