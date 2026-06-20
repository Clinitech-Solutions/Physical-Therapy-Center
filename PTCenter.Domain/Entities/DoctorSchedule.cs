namespace PTCenter.Domain.Entities;

public class DoctorSchedule : BaseEntity
{
    public int Id { get; set; }
    public string DoctorId { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public ApplicationUser Doctor { get; set; } = null!;
}
