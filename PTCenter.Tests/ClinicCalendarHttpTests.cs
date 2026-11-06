using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using Xunit;

namespace PTCenter.Tests;

public class ClinicCalendarHttpTests
{
    [Theory]
    [InlineData("2030-02-05","2030-02-04T22:00:00Z","2030-02-05T22:00:00Z")]
    [InlineData("2026-04-24","2026-04-23T22:00:00Z","2026-04-24T21:00:00Z")]
    [InlineData("2026-10-29","2026-10-28T21:00:00Z","2026-10-29T22:00:00Z")]
    public async Task Appointment_Doctor_and_Reception_queries_share_Cairo_midnight_and_DST_bounds(string day,string first,string next)
    {
        using var host=new BookingTestHost();await DoctorWorkspaceFixture.SeedAsync(host);string doctor;int[] expected;
        var start=DateTime.Parse(first,null,System.Globalization.DateTimeStyles.AdjustToUniversal);var end=DateTime.Parse(next,null,System.Globalization.DateTimeStyles.AdjustToUniversal);
        using(var scope=host.Services.CreateScope()){
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();doctor=await db.Users.Where(u=>u.UserName=="access.doctor").Select(u=>u.Id).SingleAsync();
            var records=new[]{start.AddTicks(-1),start,end.AddTicks(-1),end}.Select(at=>new Appointment{PatientId=1,DoctorId=doctor,RoomId=1,AppointmentType="Session",StartTime=at,EndTime=at.AddMinutes(1),Status=AppointmentStatus.Pending}).ToArray();db.Appointments.AddRange(records);await db.SaveChangesAsync();expected=[records[1].Id,records[2].Id];
        }
        using var r=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.LoginAsync(r);
        var generic=await r.GetFromJsonAsync<JsonElement>($"/api/Appointments/GetAll?Date={day}&PageSize=100");Assert.Equal(expected,generic.GetProperty("items").EnumerateArray().Select(a=>a.GetProperty("id").GetInt32()));
        var reception=await r.GetFromJsonAsync<JsonElement>($"/api/Receptionist/today-sessions?Date={day}&PageSize=100");Assert.Equal(expected,reception.GetProperty("items").EnumerateArray().Select(a=>a.GetProperty("id").GetInt32()));
        await DoctorWorkspaceFixture.LoginAsync(r,"clinical.senior");var senior=await r.GetFromJsonAsync<JsonElement>($"/api/Appointments/GetAll?Date={day}&PageSize=100");Assert.Equal(expected,senior.GetProperty("items").EnumerateArray().Select(a=>a.GetProperty("id").GetInt32()));
        await DoctorWorkspaceFixture.LoginAsync(r,"access.doctor");var schedule=await r.GetFromJsonAsync<JsonElement>($"/api/Doctor/schedule?date={day}");Assert.Equal(expected,schedule.GetProperty("appointments").EnumerateArray().Select(a=>a.GetProperty("appointmentId").GetInt32()));
        using var verify=host.Services.CreateScope();var stored=await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>().Appointments.OrderBy(a=>a.StartTime).ToListAsync();Assert.Equal(new[]{start.AddTicks(-1),start,end.AddTicks(-1),end},stored.Select(a=>a.StartTime));
    }
}
