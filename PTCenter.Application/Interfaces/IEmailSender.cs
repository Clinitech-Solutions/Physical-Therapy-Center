using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Interfaces
{
    public interface IEmailSender
    {
        public  Task<bool> SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
