using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Domain.Entities;

public class TreatmentPlanSession : BaseEntity
{
    public int Id { get; set; }

    public int TreatmentPlanId { get; set; }

    public int SessionNumber { get; set; }

    public DateTime? SessionDate { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Pending;

    public string? Notes { get; set; }

    public TreatmentPlan TreatmentPlan { get; set; } = null!;
}