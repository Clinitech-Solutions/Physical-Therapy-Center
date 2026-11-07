using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public class UnsignedAssessmentHttpTests
{
    [Fact]
    public async Task Departed_assessment_remains_actionable_across_dates_and_signoff_alone_delivers_it()
    {
        using var host=new BookingTestHost();using var r=host.CreateClient(new(){BaseAddress=new("https://localhost")});await DoctorWorkspaceFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(r);
        var patient=await r.PostAsJsonAsync("/api/Patients/Add",new{fullName="Unsigned regression",phoneNumber="01012345678",gender=0,birthDate="1990-01-01",paymentType="Cash"});Assert.Equal(HttpStatusCode.OK,patient.StatusCode);var pid=(await patient.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        string senior,doctor;using(var scope=host.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();senior=await db.Users.Where(u=>u.UserName=="clinical.senior").Select(u=>u.Id).SingleAsync();doctor=await db.Users.Where(u=>u.UserName=="access.doctor").Select(u=>u.Id).SingleAsync();}
        var booking=await r.PostAsJsonAsync("/api/Bookings",new BookingRequest{IdempotencyKey=Guid.NewGuid().ToString(),PatientId=pid,ClinicianId=senior,RoomId=1,StartsAt=[new(2030,2,4,10,0,0,TimeSpan.Zero)],AssessmentCharge="Free"});Assert.Equal(HttpStatusCode.OK,booking.StatusCode);var id=(await booking.Content.ReadFromJsonAsync<BookingReceipt>())!.Appointments[0].Id;
        using(var scope=host.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();var a=await db.Appointments.SingleAsync(a=>a.Id==id);a.StartTime=ClinicCalendar.DayStart(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,ClinicCalendar.Zone).DateTime).AddDays(-3)).AddHours(10);a.EndTime=a.StartTime.AddHours(1);await db.SaveChangesAsync();}
        host.Clock.Now=new DateTimeOffset(ClinicCalendar.DayStart(DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,ClinicCalendar.Zone).DateTime).AddDays(-3)).AddHours(10),TimeSpan.Zero);Assert.Equal(HttpStatusCode.OK,(await r.PutAsJsonAsync($"/api/Receptionist/appointments/{id}/status",AppointmentStatus.InProgress)).StatusCode);
        using var s=host.CreateClient(new(){BaseAddress=new("https://localhost")});await DoctorWorkspaceFixture.LoginAsync(s,"clinical.senior");
        var draft=new CompleteAssessmentRequest{AppointmentId=id,Diagnosis="Saved unsigned diagnosis",DoctorId=doctor,NumberOfSessions=0,FrequencyPerWeek=0,TreatmentType="Physio",Exercises="Draft exercise\nSecond line"};
        Assert.Equal(HttpStatusCode.OK,(await s.PutAsJsonAsync("/api/Bookings/assessments/draft",draft)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await r.PutAsJsonAsync($"/api/Receptionist/appointments/{id}/status",AppointmentStatus.Completed)).StatusCode);
        using(var scope=host.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();Assert.NotNull((await db.Appointments.SingleAsync(a=>a.Id==id)).ActualCheckOut);Assert.Null((await db.Assessments.SingleAsync(a=>a.AppointmentId==id)).CompletedAt);Assert.Null((await db.Invoices.SingleAsync(i=>i.AppointmentId==id)).DeliveredAt);Assert.False(await db.TreatmentPlans.AnyAsync(p=>p.Assessment.PatientId==pid));}
        var tasks=await s.GetAsync("/api/Bookings/assessments/tasks");Assert.Equal(HttpStatusCode.OK,tasks.StatusCode);Assert.Contains((await tasks.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray(),a=>a.GetProperty("id").GetInt32()==id && a.GetProperty("actualCheckOut").ValueKind==JsonValueKind.String);
        var workspace=await s.GetFromJsonAsync<JsonElement>($"/api/Bookings/assessments/appointments/{id}");Assert.Equal("Saved unsigned diagnosis",workspace.GetProperty("diagnosis").GetString());
        Assert.Equal(0,workspace.GetProperty("numberOfSessions").GetInt32());
        Assert.Equal("Draft exercise\nSecond line",workspace.GetProperty("exercises").GetString());
        await DoctorWorkspaceFixture.LoginAsync(s,"clinical.otherSenior");Assert.DoesNotContain((await s.GetFromJsonAsync<JsonElement>("/api/Bookings/assessments/tasks")).EnumerateArray(),a=>a.GetProperty("id").GetInt32()==id);Assert.Equal(HttpStatusCode.Forbidden,(await s.GetAsync($"/api/Bookings/assessments/appointments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await s.PutAsJsonAsync("/api/Bookings/assessments/draft",draft)).StatusCode);
        var complete=new CompleteAssessmentRequest{AppointmentId=id,Diagnosis="Saved unsigned diagnosis",DoctorId=doctor,NumberOfSessions=3,FrequencyPerWeek=2,TreatmentType="Physio",Exercises="Saved instructions"};Assert.Equal(HttpStatusCode.Forbidden,(await s.PostAsJsonAsync("/api/Bookings/assessments/complete",complete)).StatusCode);
        await DoctorWorkspaceFixture.LoginAsync(s,"clinical.senior");Assert.Equal(HttpStatusCode.OK,(await s.PostAsJsonAsync("/api/Bookings/assessments/complete",complete)).StatusCode);Assert.Equal(HttpStatusCode.OK,(await s.PostAsJsonAsync("/api/Bookings/assessments/complete",complete)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await s.PutAsJsonAsync("/api/Bookings/assessments/draft",draft)).StatusCode);
        Assert.DoesNotContain((await s.GetFromJsonAsync<JsonElement>("/api/Bookings/assessments/tasks")).EnumerateArray(),a=>a.GetProperty("id").GetInt32()==id);
        var context=await r.GetFromJsonAsync<JsonElement>($"/api/Bookings/patients/{pid}");
        var plan=context.GetProperty("plans").EnumerateArray().Single();
        Assert.Equal(2,plan.GetProperty("frequencyPerWeek").GetInt32());
        Assert.Empty(plan.GetProperty("bookedStartsAt").EnumerateArray());
        using(var scope=host.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();Assert.NotNull((await db.Invoices.SingleAsync(i=>i.AppointmentId==id)).DeliveredAt);Assert.Single(await db.TreatmentPlans.Where(p=>p.Assessment.PatientId==pid).ToListAsync());Assert.Empty(await db.Payments.ToListAsync());Assert.Single(await db.Invoices.ToListAsync());}
    }
}
