using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Assessments;
using PTCenter.Application.Contracts.TreatmentPlans;
using PTCenter.Application.Contracts.TreatmentPlanSession;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using Xunit;

namespace PTCenter.Tests;

public class LegacyClinicalAuthorizationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters={new JsonStringEnumConverter()} };
    [Fact]
    public async Task Unassigned_Senior_cannot_mutate_legacy_assessment_plan_or_sessions()
    {
        using var host = new BookingTestHost(); using var client = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        await DoctorWorkspaceFixture.SeedAsync(host); await DoctorWorkspaceFixture.BookAsync(host);
        using var scope = host.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var plan = await db.TreatmentPlans.Include(p=>p.Assessment).Include(p=>p.Sessions).SingleAsync(p=>p.Assessment.PatientId==1);
        var session = plan.Sessions.First();
        await DoctorWorkspaceFixture.LoginAsync(client,"clinical.otherSenior");
        var update = new UpdateAssessmentRequest { SeniorId=plan.Assessment.SeniorId,Diagnosis="Unauthorized",ConditionType="Physio",Priority="Routine" };
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/Assessments/Update/{plan.AssessmentId}",update)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync($"/api/Assessments/Delete/{plan.AssessmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/Assessments/Add",new CreateAssessmentRequest { PatientId=1,SeniorId=plan.Assessment.SeniorId,Diagnosis="Unauthorized",ConditionType="Physio",Priority="Routine" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/TreatmentPlans/Update/{plan.Id}",new UpdateTreatmentPlanRequest { DoctorId=plan.DoctorId,NumberOfSessions=3,FrequencyPerWeek=2,TreatmentType="Physio" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync($"/api/TreatmentPlans/Delete/{plan.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/TreatmentPlans/Add",new CreateTreatmentPlanRequest { AssessmentId=plan.AssessmentId,DoctorId=plan.DoctorId,NumberOfSessions=3,FrequencyPerWeek=2,TreatmentType="Physio" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/TreatmentPlanSessions/Add",new CreateTreatmentPlanSessionRequest { TreatmentPlanId=plan.Id,SessionNumber=3 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/TreatmentPlanSessions/Update/{session.Id}",new UpdateTreatmentPlanSessionRequest { SessionNumber=session.SessionNumber,Notes="Unauthorized" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.DeleteAsync($"/api/TreatmentPlanSessions/Delete/{session.Id}")).StatusCode);
        Assert.Equal("Local only",(await db.Assessments.AsNoTracking().SingleAsync(a=>a.Id==plan.AssessmentId)).Diagnosis);
        Assert.Equal(3,(await db.TreatmentPlans.AsNoTracking().SingleAsync(p=>p.Id==plan.Id)).NumberOfSessions);
        Assert.Single(await db.Appointments.ToListAsync()); Assert.Single(await db.Invoices.ToListAsync()); Assert.Single(await db.Payments.ToListAsync());
    }

    [Theory]
    [InlineData("clinical.senior")]
    [InlineData("access.ceo")]
    public async Task Signed_assessment_and_prescription_cannot_be_silently_changed_even_by_owner_or_supervisor(string user)
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await DoctorWorkspaceFixture.SeedAsync(host);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();var p=await db.TreatmentPlans.Include(p=>p.Assessment).SingleAsync(p=>p.Assessment.PatientId==1);
        await DoctorWorkspaceFixture.LoginAsync(client,user);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync($"/api/Assessments/Update/{p.AssessmentId}",new UpdateAssessmentRequest{SeniorId=p.Assessment.SeniorId,Diagnosis="Overwrite",ConditionType="Physio",Priority="Routine"})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync($"/api/Assessments/Delete/{p.AssessmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PutAsJsonAsync($"/api/TreatmentPlans/Update/{p.Id}",new UpdateTreatmentPlanRequest{DoctorId=p.DoctorId,NumberOfSessions=2,FrequencyPerWeek=1,TreatmentType="Physio"})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.DeleteAsync($"/api/TreatmentPlans/Delete/{p.Id}")).StatusCode);
    }

    [Fact]
    public async Task Draft_plan_rejects_non_Doctor_wrong_gender_and_locked_Doctor_but_allows_owner_and_supervisor()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await DoctorWorkspaceFixture.SeedAsync(host);
        int assessmentId;string senior,doctor,female,locked;
        using(var scope=host.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();senior=await db.Users.Where(u=>u.UserName=="clinical.senior").Select(u=>u.Id).SingleAsync();doctor=await db.Users.Where(u=>u.UserName=="access.doctor").Select(u=>u.Id).SingleAsync();
            female="access-senior";locked="access-locked";
            var a=new Assessment{PatientId=1,SeniorId=senior,Diagnosis="Draft",ConditionType="Physio",Priority="Routine"};db.Assessments.Add(a);await db.SaveChangesAsync();assessmentId=a.Id;
        }
        await DoctorWorkspaceFixture.LoginAsync(client,"clinical.senior");
        var create=new CreateTreatmentPlanRequest{AssessmentId=assessmentId,DoctorId=senior,NumberOfSessions=3,FrequencyPerWeek=2,TreatmentType="Physio"};
        foreach(var invalid in new[]{senior,female,locked,"missing"}){create.DoctorId=invalid;Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/TreatmentPlans/Add",create)).StatusCode);}
        create.DoctorId=doctor;var response=await client.PostAsJsonAsync("/api/TreatmentPlans/Add",create);Assert.Equal(HttpStatusCode.OK,response.StatusCode);var plan=(await response.Content.ReadFromJsonAsync<TreatmentPlanResponse>(Json))!;
        var update=new UpdateTreatmentPlanRequest{DoctorId=senior,NumberOfSessions=3,FrequencyPerWeek=2,TreatmentType="Physio"};Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync($"/api/TreatmentPlans/Update/{plan.Id}",update)).StatusCode);
        update.DoctorId=doctor;await DoctorWorkspaceFixture.LoginAsync(client,"access.ceo");Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/TreatmentPlans/Update/{plan.Id}",update)).StatusCode);
    }
}
