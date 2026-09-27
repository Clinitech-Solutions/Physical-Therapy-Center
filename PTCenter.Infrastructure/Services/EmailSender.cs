using System.Net;
using System.Net.Mail;

namespace PTCenter.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task<bool> SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        "clinitechsolutions65@gmail.com",
                        "fqqc rggu nlio zmrt"
                    )
                };

                using var message = new MailMessage
                {
                    From = new MailAddress("clinitechsolutions65@gmail.com"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                message.To.Add(email);

                await client.SendMailAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}