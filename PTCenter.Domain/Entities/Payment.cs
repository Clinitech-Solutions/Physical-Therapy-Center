using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Domain.Entities;

public class Payment:BaseEntity
{
    public int Id{get;set;}
    public int InvoiceId{get;set;} 
    public decimal Amount{get;set;} 
    public string PaymentMethod{get;set;}=null!;
    public string? TransactionId{get;set;} 
    public DateTime PaymentDate{get;set;}
    public PaymentStatus Status{get;set;}=PaymentStatus.Pending;
}