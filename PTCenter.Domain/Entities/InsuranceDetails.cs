using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Domain.Entities
{
    public enum InsuranceStatus
    {
        Approved,
        Pending,
        Rejected,
        Expired
    }

    public class InsuranceDetails
    {
        public InsuranceStatus? Status { get; set; }
        public decimal? CopayPercentage { get; set; }
        public int? ApprovedSessions { get; set; }
        public string? MemberId { get; set; }
        public string? Employer { get; set; }
    }
}
