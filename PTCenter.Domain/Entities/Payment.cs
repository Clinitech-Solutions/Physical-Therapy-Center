
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Domain.Entities;

public class Payment : BaseEntity
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? TransactionId { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public virtual Invoice Invoice { get; set; } = null!;
}
