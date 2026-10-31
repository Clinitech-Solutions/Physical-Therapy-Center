using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Domain.Entities.Enums
{
    public enum Gender
    {
        Male,
        Female
    }
    public enum Status
    {
        Low,
        Medium,
        High
    }
    public enum SessionStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
    public enum AppointmentStatus
    {
        Pending,
        Completed,
        Cancelled,
        NoShow,
        InProgress = 4
    }
    
    public enum StatusPlan
    {
        Active,
        Inactive,
        
    }
    
    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Cancelled,
        Waived
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        PackageCovered
    }

    public enum InvoiceType
    {
        Assessment,
        Session,
        Installment
    }

    public enum PaymentMethod
    {
        Cash,
        Visa,
        EWallet,
        BankTransfer,
        Insurance,
        Online
    }
}
