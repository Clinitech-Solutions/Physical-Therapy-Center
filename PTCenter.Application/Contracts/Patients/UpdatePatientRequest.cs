using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Serialization;

namespace PTCenter.Application.Contracts.Patients
{
    public class UpdatePatientRequest
    {
        [StringLength(50, MinimumLength = 3)] public string? UserName { get; set; }
        public string? Email { get; set; }
        [StringLength(150, MinimumLength = 2)] public string? FullName { get; set; }
       
        public Gender? Gender { get; set; }


        public DateTime? BirthDate { get; set; }
       
        [StringLength(20, MinimumLength = 1)] public string? PhoneNumber { get; set; }
        private string? paymentType;
        private string? emergencyContact;
        private InsuranceDetailsResponse? insuranceDetails;
        private int? insuranceCompanyId;
        [MaxLength(20)]
        public string? PaymentType { get => paymentType; set { paymentType=value; PaymentTypeProvided=true; } }

        [MaxLength(20)]
        public string? EmergencyContact { get => emergencyContact; set { emergencyContact=value; EmergencyContactProvided=true; } }
        public InsuranceDetailsResponse? InsuranceDetails { get => insuranceDetails; set { insuranceDetails=value; InsuranceDetailsProvided=true; } }
        public int? InsuranceCompanyId { get => insuranceCompanyId; set { insuranceCompanyId=value; InsuranceCompanyIdProvided=true; } }
        [JsonIgnore] public bool PaymentTypeProvided { get; private set; }
        [JsonIgnore] public bool EmergencyContactProvided { get; private set; }
        [JsonIgnore] public bool InsuranceDetailsProvided { get; private set; }
        [JsonIgnore] public bool InsuranceCompanyIdProvided { get; private set; }
    }
}
