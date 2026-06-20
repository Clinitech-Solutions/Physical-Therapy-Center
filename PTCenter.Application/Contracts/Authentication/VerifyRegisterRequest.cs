using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class VerifyRegisterRequest
    {
        public string EmailORUserName { get; set; } = null!;
        public string OTP { get; set; } = null!;
    }
}
