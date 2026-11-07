using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Infrastructure.Persistence;
using Xunit;
namespace PTCenter.Tests;
public class PatientReceiptAuditHttpTests
{
    [Fact]
    public async Task Patient_deposit_and_collection_record_the_authenticated_employee_without_trusting_client_actor_fields()
    {
        using var host=new BookingTestHost();await DoctorWorkspaceFixture.SeedAsync(host);
        using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.LoginAsync(client);
        var response=await client.PostAsJsonAsync("/api/Patients/Add",new{fullName="UAT receipt audit",phoneNumber="01012345678",birthDate="1990-01-01",gender=0,paymentType="Cash",idempotencyKey=Guid.NewGuid().ToString()});Assert.Equal(HttpStatusCode.OK,response.StatusCode);var patient=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        string senior,actor;using(var scope=host.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();senior=await db.Users.Where(u=>u.UserName=="clinical.senior").Select(u=>u.Id).SingleAsync();actor=await db.Users.Where(u=>u.UserName==ReceptionAccessFixture.UserName).Select(u=>u.Id).SingleAsync();}
        var booking=await client.PostAsJsonAsync("/api/Bookings",new{idempotencyKey=Guid.NewGuid().ToString(),patientId=patient,clinicianId=senior,roomId=1,startsAt=new[]{"2030-02-04T10:00:00Z"},assessmentCharge="Paid",unitPrice=100,deposit=40,paymentMethod="Cash",createdBy="forged-client-actor"});Assert.Equal(HttpStatusCode.OK,booking.StatusCode);var id=(await booking.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("appointments")[0].GetProperty("id").GetInt32();
        var body=new{idempotencyKey=Guid.NewGuid().ToString(),amount=60,method="Cash",createdBy="forged-client-actor"};Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync($"/api/Finance/appointments/{id}/payments",body)).StatusCode);
        using var check=host.Services.CreateScope();var context=check.ServiceProvider.GetRequiredService<ApplicationDbContext>();var payments=await context.Payments.Where(p=>p.Invoice.PatientId==patient).AsNoTracking().ToListAsync();Assert.Equal(2,payments.Count);Assert.All(payments,p=>Assert.Equal(actor,p.CreatedBy));Assert.Equal(100,payments.Sum(p=>p.Amount));
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync($"/api/Finance/appointments/{id}/payments",body)).StatusCode);Assert.Equal(2,await context.Payments.CountAsync(p=>p.Invoice.PatientId==patient));
        var invoice=await context.Invoices.SingleAsync(i=>i.PatientId==patient);
        var history=await client.GetFromJsonAsync<JsonElement>($"/api/Billing/invoices/{invoice.Id}/payments");
        Assert.All(history.EnumerateArray(),p=>{Assert.Equal(actor,p.GetProperty("recordedBy").GetString());Assert.Equal("Local Receptionist",p.GetProperty("recordedByName").GetString());});
    }
}
