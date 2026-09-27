using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PTCenter.Application.Contracts.Patients
{
    public class UpdatePatientRequest
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
       
        public Gender Gender { get; set; } = Gender.Male;


        public DateTime BirthDate { get; set; }
       
        public string PhoneNumber { get; set; } = null!;
        [MaxLength(20)]
        public string? PaymentType { get; set; }

        [MaxLength(20)]
        public string? EmergencyContact { get; set; }

        public int? InsuranceCompanyId { get; set; }
    }
}
