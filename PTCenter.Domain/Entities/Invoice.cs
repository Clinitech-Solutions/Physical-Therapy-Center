using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Domain.Entities;

public class Invoice:BaseEntity
{
    public int Id{get;set;} 
    public int AppointmentId{get;set;} 
    public int PatientId{get;set;}
    public decimal TotalAmount{get;set;}
    public decimal InsuranceAmount{get;set;} 
    public decimal PatientAmount{get;set;}
    public InvoiceStatus Status {get;set;}
}