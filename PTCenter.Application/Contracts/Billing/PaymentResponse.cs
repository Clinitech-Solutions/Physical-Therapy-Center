using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Billing
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public string? IdempotencyKey { get; set; }

        public int InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string? TransactionId { get; set; }

        public DateTime PaymentDate { get; set; }

        public PaymentStatus Status { get; set; }
    }
}
