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
        NoShow
    }
    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Cancelled
    }
    public enum StatusPlan
    {
        Active,
        Inactive,
        
    }
    public enum PaymentStatus
    {
        Pending,
        Paid,
        PackageCovered
    }
}