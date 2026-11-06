namespace PTCenter.Domain.Entities;

/// <summary>One actual insurer receipt. Payments are its allocations, not additional cash.</summary>
public class InsuranceChequeReceipt
{
    public int Id { get; set; }
    public int InsuranceCompanyId { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string ChequeIdentityHash { get; set; } = "";
    public string ChequeNumber { get; set; } = "";
    public string BankName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly ReceivedOn { get; set; }
    public DateOnly? ChequeDate { get; set; }
    public string? Notes { get; set; }
    public string RecordedBy { get; set; } = "";
    public DateTime RecordedAt { get; set; }
}
