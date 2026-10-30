namespace PTCenter.Domain.Entities;

/// <summary>Durable booking intent, financial agreement and retry identity. Payments remain separate.</summary>
public class BookingBatch : BaseEntity
{
    public int Id { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public int PatientId { get; set; }
    public string PricingMode { get; set; } = "";
    public decimal GrossAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PatientAmount { get; set; }
    public decimal InsuranceAmount { get; set; }
    public string? WaiverReason { get; set; }
    public string InstallmentsJson { get; set; } = "[]";
}
