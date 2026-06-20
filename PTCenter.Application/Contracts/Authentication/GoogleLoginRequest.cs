using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = default!;
    }
}
