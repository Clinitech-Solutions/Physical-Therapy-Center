namespace PTCenter.Domain.Entities;
public class Notification:BaseEntity
{
    public int Id{get;set;}
    public string UserId{get;set;}=null!;
    public string Title{get;set;}=null!;
    public string Message{get;set;}=null!;
    public bool IsRead{get;set;}
}