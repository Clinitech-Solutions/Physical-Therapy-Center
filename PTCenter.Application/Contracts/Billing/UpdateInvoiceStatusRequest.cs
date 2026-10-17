using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Billing
{
    public class UpdateInvoiceStatusRequest
    {
        public InvoiceStatus Status { get; set; }

    }
}
