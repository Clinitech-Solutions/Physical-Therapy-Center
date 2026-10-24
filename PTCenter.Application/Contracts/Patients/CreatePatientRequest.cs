using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PTCenter.Application.Contracts.Patients
{
    public class CreatePatientRequest
    {
        [Required]
        public string UserName { get; set; } = null!;
        //[Required]
        //public string Email { get; set; } = null!;
        [Required]
        public string FullName { get; set; } = null!;
        [Required]
        public Gender Gender { get; set; } = Gender.Male;
        [Required]

        public DateTime BirthDate { get; set; }
        [Required]
        public string PhoneNumber { get; set; } = null!;

        [MaxLength(20)]
        public string? PaymentType { get; set; }

        [MaxLength(20)]
        public string? EmergencyContact { get; set; }
        public InsuranceDetailsDto? InsuranceDetails { get; set; }
        public int? InsuranceCompanyId { get; set; }
    }
}
