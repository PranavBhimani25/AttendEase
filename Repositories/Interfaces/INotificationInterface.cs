using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Repositories.Models; 
namespace Repositories.Interfaces
{
    public interface INotificationInterface
    {
        public Task<IConnection> GetConnectionAsync();
        Task AdminSend(IConnection con, MsgToEmp model);

        // public List<MsgToEmp> NotiEmpStore();
        public Task GetEmpNotification(IConnection con, string userId);
        public Task<List<MsgToEmp>> GetStoredNotifications(string userId);
        public Task<List<MsgToEmp>> GetAdminSentNotifications();
        public Task<bool> MarkNotificationAsRead(string userId, MsgToEmp model);

    }
}
