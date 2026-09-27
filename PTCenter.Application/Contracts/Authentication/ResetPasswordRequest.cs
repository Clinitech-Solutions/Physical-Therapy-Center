using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class ResetPasswordRequest
    {
        public string EmailOrUserName { get; set; } = null!;
        public string OTP { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
