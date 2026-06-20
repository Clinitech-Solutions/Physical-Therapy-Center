using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PTCenter.Application.Contracts.Authentication
{
    public class RegisterRequest
    {
        public string UserName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public Gender Gender { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        public string Email { get; set; } = null!;  

    }
}
