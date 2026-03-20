using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;
using Repositories.Models;

namespace MVCAttendEase.Controllers
{
    [Route("[Controller]")]
    public class NotificationController : Controller
    {
        private readonly INotificationInterface _notificationRepo;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ILogger<NotificationController> logger, INotificationInterface notificationRepo)
        {
            _logger = logger;
            _notificationRepo = notificationRepo;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("SendNotificationAsync")]
        public async Task<IActionResult> SendNotificationAsync(string receiverName, string message)
        {
            var notification = new MsgToEmp
            {
                Sender = "Admin",
                Receiver = receiverName,
                Message = message,
                CreatedAt = DateTime.Now
            };
            var connection = await _notificationRepo.GetConnectionAsync();
            // RabbitMQ me send
            await _notificationRepo.AdminSend(connection, notification);


            return Ok(new { success = true, message = "Notification sent" });
        }

        [HttpGet("SentNotifications")]
        public async Task<IActionResult> SentNotifications()
        {
            var notifications = await _notificationRepo.GetAdminSentNotifications();
            return Ok(new
            {
                success = true,
                data = notifications
            });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
