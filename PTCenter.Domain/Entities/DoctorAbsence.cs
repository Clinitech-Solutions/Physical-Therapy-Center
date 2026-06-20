namespace PTCenter.Domain.Entities;
public class DoctorAbsence:BaseEntity
{ 
    public int Id{get;set;} 
    public string DoctorId{get;set;}=null!;
    public DateOnly AbsentDate{get;set;} 
    public string? Reason{get;set;}
}