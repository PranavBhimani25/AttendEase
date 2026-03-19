using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Repositories.Models;

namespace MVCAttendEase.Services
{
    public class MailService
    {
        private readonly EmailModal _emailSettings;

        public MailService(IOptions<EmailModal> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        

        // It is Created By Het Patel . So For any Query Contact Him. 
        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
            {
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.Password),
                EnableSsl = true
            };

            var message = new MailMessage(_emailSettings.SenderEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            smtp.Send(message);
        }


    }
}