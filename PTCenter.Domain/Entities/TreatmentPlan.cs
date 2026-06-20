namespace PTCenter.Domain.Entities;
public enum StatusPlan
{
    Active,
    NotActive,
}
public class TreatmentPlan : BaseEntity
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public string DoctorId { get; set; } = null!;
    public int NumberOfSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int FrequencyPerWeek { get; set; }
    public string TreatmentType { get; set; } = null!;
    public string? Exercises { get; set; }
    public StatusPlan Status { get; set; } 

    public Assessment Assessment { get; set; } = null!;
    public ApplicationUser Doctor { get; set; } = null!;
    public ICollection<TreatmentPlanSession> Sessions { get; set; } = new List<TreatmentPlanSession>();
}
