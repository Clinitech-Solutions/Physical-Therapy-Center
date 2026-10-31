using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Billing
{
    public class InvoiceResponse
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = null!;

        public int? AppointmentId { get; set; }
        public int? CourseAgreementId { get; set; }
        public int? TreatmentPlanSessionId { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public int PatientId { get; set; }

        public string PatientName { get; set; } = null!;

        public string? PatientPhone { get; set; }

        public string? InsuranceCompany { get; set; }

        public InvoiceType Type { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal InsuranceAmount { get; set; }

        public decimal PatientAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public InvoiceStatus Status { get; set; }

        public DateTime? AppointmentDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? PaymentMethod { get; set; }
    }

}
