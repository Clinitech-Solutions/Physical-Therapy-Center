
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Domain.Entities;

public class Invoice : BaseEntity
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public int? AppointmentId { get; set; }
    public int? CourseAgreementId { get; set; }
    public int? TreatmentPlanSessionId { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public int PatientId { get; set; }

    public InvoiceType Type { get; set; } = InvoiceType.Session;

    public decimal TotalAmount { get; set; }

    public decimal InsuranceAmount { get; set; }

    public decimal PatientAmount { get; set; }

    // المبلغ اللي دفعه المريض بالفعل
    public decimal PaidAmount { get; set; }

    // المتبقي على المريض
    public decimal RemainingAmount => Math.Max(0, PatientAmount - PaidAmount);

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Appointment? Appointment { get; set; }

    public virtual ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
