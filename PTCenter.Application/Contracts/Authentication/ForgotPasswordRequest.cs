using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class ForgotPasswordRequest
    {
        public string EmailOrUserName { get; set; } = null!;

    }
}
