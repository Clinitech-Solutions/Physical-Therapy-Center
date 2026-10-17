using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Billing
{
    public class CreateInvoiceRequest
    {
        public int AppointmentId { get; set; }

        public int PatientId { get; set; }

        public InvoiceType Type { get; set; } = InvoiceType.Session;

        public decimal TotalAmount { get; set; }

        public decimal InsuranceAmount { get; set; }

        public decimal PatientAmount { get; set; }
    }
}
