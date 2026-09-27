using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class VerifyForgotPasswordRequest
    {
        public string EmailOrUserName { get; set; } = null!;
        public string OTP { get; set; } = null!;
    }
}
