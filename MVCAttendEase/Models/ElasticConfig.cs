using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAttendEase.Models
{
    public class ElasticConfig
    {
        public string Uri { get; set; }
        public string ApiKey { get; set; }
        public string DefaultIndex { get; set; }
    }
}