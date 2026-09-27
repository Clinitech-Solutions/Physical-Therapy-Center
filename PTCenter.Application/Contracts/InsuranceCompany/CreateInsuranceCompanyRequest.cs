using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PTCenter.Application.Contracts.InsuranceCompany
{
    public class CreateInsuranceCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Url]
        public string? PortalUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
