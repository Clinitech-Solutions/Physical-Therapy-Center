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
        Completed,
        Cancelled
    }
    public enum AppointmentStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        NoShow
    }
    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Cancelled
    }
    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}