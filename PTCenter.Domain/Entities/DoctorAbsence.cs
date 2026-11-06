namespace PTCenter.Domain.Entities;
public class DoctorAbsence:BaseEntity
{ 
    public int Id{get;set;} 
    public string DoctorId{get;set;}=null!;
    public DateOnly AbsentDate{get;set;} 
    public string? Reason{get;set;}
    // Legacy rows retain their clinic-day meaning when interval columns are null.
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool Withdrawn { get; set; }
    public int Version { get; set; } = 1;
    public string HistoryJson { get; set; } = "[]";
    public string? OperationKey { get; set; }
}
