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
        public void SendEmail(string toEmail, string subject, string body,  byte[] pdfBytes = null)
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

            // ✅ Attach only if PDF exists
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                var stream = new MemoryStream(pdfBytes);
                var attachment = new Attachment(stream, "AttendanceReport.pdf", "application/pdf");
                message.Attachments.Add(attachment);
            }

            smtp.Send(message);
        }

        // It is Created By Het Patel . So For any Query Contact Him. 
        // public async Task SendEmail(string toEmail, string subject, string body)
        // {
            
        // // ✅ Validation (VERY IMPORTANT)
        // if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
        //     throw new ArgumentException("Sender email is not configured.");

        // if (string.IsNullOrWhiteSpace(toEmail))
        //     throw new ArgumentException("Recipient email is required.");

        // using var smtp = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port)
        // {
        //     Credentials = new NetworkCredential(
        //         _emailSettings.SenderEmail,
        //         _emailSettings.Password
        //     ),
        //     EnableSsl = true
        // };

        // using var message = new MailMessage
        // {
        //     From = new MailAddress(_emailSettings.SenderEmail),
        //     Subject = subject,
        //     Body = body,
        //     IsBodyHtml = true
        // };

        // message.To.Add(toEmail);

        // try
        // {
        //     await smtp.SendMailAsync(message); // ✅ Async
        // }
        // catch (SmtpException ex)
        // {
        //     throw new Exception($"SMTP Error: {ex.Message}", ex);
        // }
        // catch (Exception ex)
        // {
        //     throw new Exception($"Email sending failed: {ex.Message}", ex);
        // }
        // }

    }
}