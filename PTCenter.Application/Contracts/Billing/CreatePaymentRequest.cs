using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PTCenter.Application.Contracts.Billing
{
    public class CreatePaymentRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int InvoiceId { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "999999999")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }
    }
}
