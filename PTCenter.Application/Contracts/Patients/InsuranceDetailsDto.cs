using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Patients
{
    public class InsuranceDetailsDto
    {
        public string? Status { get; set; }
        public decimal? CopayPercentage { get; set; }
        public int? ApprovedSessions { get; set; }
        public string? MemberId { get; set; }
        public string? Employer { get; set; }
    }
}
