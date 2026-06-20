using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class ForgetPasswordRequest
    {
        public string EmailORUserName { get; set; } = null!;
    }
}
