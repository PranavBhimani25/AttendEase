using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAttendEase.Models
{
    public class RabbitMQConfig
    {
        public string Uri { get; set; }
        public string QueueName { get; set; }
        public string? NotificationQueueName { get; set; }
        public string? AttendanceNotificationQueueName { get; set; }  // ← add this
    }
}
