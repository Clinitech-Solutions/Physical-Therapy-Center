using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Patients
{
    public class PatientResponse
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public DateTime BrithDay { get; set; }

        public string? PaymentType { get; set; }

        public string? EmergencyContact { get; set; }

        public int? InsuranceCompanyId { get; set; }
        public Gender Gender { get; set; }

        public string? InsuranceCompanyName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
