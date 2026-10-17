using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Billing
{
    public class BillingDashboardResponse
    {
        public decimal ExpectedToday { get; set; }

        public decimal CollectedToday { get; set; }

        public decimal CashCollected { get; set; }

        public decimal VisaAndEWalletCollected { get; set; }

        public decimal PendingCollection { get; set; }

        public int PendingInvoicesCount { get; set; }
    }
}
