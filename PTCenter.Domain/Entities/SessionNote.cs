namespace PTCenter.Domain.Entities;
public class SessionNote:BaseEntity
{
    public int Id{get;set;} 
    public int AppointmentId{get;set;} 
    public string DoctorId{get;set;}=null!;
    public string Notes{get;set;}=null!;
}