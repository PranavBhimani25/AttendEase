using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAttendEase.Models
{
    public class ElasticsearchConfig
    {
        public string Url { get; set; }          // Elastic Cloud endpoint
        public string Username { get; set; }     // usually "elastic"
        public string Password { get; set; }     // generated password
        public string DefaultIndex { get; set; } // e.g. "attendance_logs"
    }
}