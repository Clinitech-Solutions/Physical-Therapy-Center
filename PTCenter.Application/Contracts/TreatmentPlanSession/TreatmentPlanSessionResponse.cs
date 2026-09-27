using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.TreatmentPlanSession
{

    public class TreatmentPlanSessionResponse
    {
        public int Id { get; set; }
        public int TreatmentPlanId { get; set; }
        public int SessionNumber { get; set; }
        public DateTime? SessionDate { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public SessionStatus Status { get; set; }
        public string? Notes { get; set; }
    }
}
