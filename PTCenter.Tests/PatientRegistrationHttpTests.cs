using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTCenter.Application.Contracts.Patients;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Infrastructure.Services;
using Xunit;

namespace PTCenter.Tests;

public class PatientRegistrationHttpTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters={new JsonStringEnumConverter()} };
    [Fact]
    public async Task No_username_registration_preserves_real_email_and_concurrent_retries_create_one_patient()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        var body=new{fullName="Registration regression",phoneNumber="01012345678",birthDate="1990-01-01",gender=0,paymentType="Cash",email="real.patient@example.invalid",idempotencyKey=Guid.NewGuid().ToString()};
        var results=await Task.WhenAll(client.PostAsJsonAsync("/api/Patients/Add",body),client.PostAsJsonAsync("/api/Patients/Add",body));Assert.All(results,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));
        var a=(await results[0].Content.ReadFromJsonAsync<PatientResponse>(Json))!;var b=(await results[1].Content.ReadFromJsonAsync<PatientResponse>(Json))!;Assert.Equal(a.Id,b.Id);Assert.Equal(body.email,a.Email);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Patients/Add",body with {fullName="Changed intent"})).StatusCode);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();var p=await db.Patients.Include(p=>p.ApplicationUser).SingleAsync(p=>p.Id==a.Id);
        Assert.Matches("^patient\\.[a-f0-9]{32}$",p.ApplicationUser.UserName!);Assert.Equal(body.email,p.ApplicationUser.Email);Assert.Equal(3,await db.Patients.CountAsync());
        Assert.Equal("reception.selfpay",(await db.Users.FindAsync("access-selfpay"))!.UserName);Assert.Empty(await db.Appointments.ToListAsync());Assert.Empty(await db.Invoices.ToListAsync());
    }

    private sealed class CollisionGenerator : IPatientUsernameGenerator
    {
        private int calls;
        public string Next()=>Interlocked.Increment(ref calls)==1?"access.doctor":"patient."+Guid.NewGuid().ToString("N");
    }

    [Fact]
    public async Task Internal_username_collision_is_retried_without_changing_existing_accounts_or_fabricating_email()
    {
        using var host=new BookingTestHost();await ReceptionAccessFixture.SeedAsync(host);
        using var factory=host.WithWebHostBuilder(b=>b.ConfigureServices(s=>s.AddSingleton<IPatientUsernameGenerator>(new CollisionGenerator())));
        using var client=factory.CreateClient(new(){BaseAddress=new("https://localhost")});await ReceptionAccessFixture.LoginAsync(client);
        var response=await client.PostAsJsonAsync("/api/Patients/Add",new{fullName="Collision patient",phoneNumber="01012345678",birthDate="1990-01-01",gender=0,paymentType="Cash",userName="SuperAdmin"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var p=(await response.Content.ReadFromJsonAsync<PatientResponse>(Json))!;Assert.Equal("",p.Email);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();var user=await db.Users.SingleAsync(u=>u.Id==p.ApplicationUserId);Assert.StartsWith("patient.",user.UserName);Assert.Null(user.Email);
        Assert.Equal("doctor@example.invalid",(await db.Users.SingleAsync(u=>u.UserName=="access.doctor")).Email);
    }
}
