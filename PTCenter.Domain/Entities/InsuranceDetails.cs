using PTCenter.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Domain.Entities
{
    public enum InsuranceStatus
    {
<<<<<<<< HEAD:PTCenter.Domain/Entities/InsuranceDetails.cs
        Approved,
        Pending,
        Rejected,
        Expired
    }

    public class InsuranceDetails
    {
========
>>>>>>>> 4fde23253b12503dbdb0b15f4232ac1c99811409:PTCenter.Application/Contracts/Patients/InsuranceDetailsDto.cs
        public InsuranceStatus? Status { get; set; }
        public decimal? CopayPercentage { get; set; }
        public int? ApprovedSessions { get; set; }
        public string? MemberId { get; set; }
        public string? Employer { get; set; }
    }
}
