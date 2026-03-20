using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVCAttendEase.Filters;
using MVCAttendEase.Models;
using MVCAttendEase.Services;

namespace MVCAttendEase.Controllers
{
    [Route("[controller]")]
    [ServiceFilter(typeof(AdminFilter))]
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("stream")]
        public async Task Stream(CancellationToken cancellationToken)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no");
            Response.ContentType = "text/event-stream";

            using var subscription = _notificationService.Subscribe(out var reader);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    NotificationMessage message;

                    try
                    {
                        message = await reader.ReadAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (message == null)
                    {
                        continue;
                    }

                    var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (Exception)
            {
                // The stream is expected to stop when the client disconnects.
            }
        }

        [HttpGet("unreadcount")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetGlobalUnreadAsync();
            return Ok(count);
        }

        [HttpPost("resetunread")]
        public async Task<IActionResult> ResetUnread()
        {
            await _notificationService.ResetGlobalUnreadAsync();
            return Ok();
        }

        [HttpPost("markread/{index}")]
        public async Task<IActionResult> MarkRead(int index)
        {
            var success = await _notificationService.MarkAsReadAsync(index);
            if (success)
            {
                return Ok(new { success = true });
            }
            return BadRequest(new { success = false });
        }

        [HttpPost("markallread")]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notificationService.MarkAllReadAsync();
            return Ok(new { success = true });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(int count = 10)
        {
            var history = await _notificationService.LoadRecentAsync(count);
            return Ok(history);
        }
    }
}
