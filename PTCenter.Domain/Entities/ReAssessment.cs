namespace PTCenter.Domain.Entities;
public class ReAssessment:BaseEntity
{
    public int Id{get;set;} 
    public int TreatmentPlanId{get;set;}
    public string SeniorId{get;set;}=null!;
    public string Progress{get;set;}=null!;
    public string Recommendation{get;set;}=null!;
}