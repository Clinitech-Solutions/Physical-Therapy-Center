using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using Xunit;

namespace PTCenter.Tests;

public sealed class BookingTestHost : WebApplicationFactory<Program>
{
    internal const string Key = "Local-test-key-only-never-valid-in-production-123456789";
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"ptcenter-booking-tests-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["Jwt:Issuer"] = "ptcenter-local-tests", ["Jwt:Audience"] = "ptcenter-local-tests", ["Jwt:Key"] = Key, ["Jwt:ExpiryYears"] = "1",
            ["Logging:LogLevel:Default"] = "Warning"
        }));
        builder.ConfigureServices(services => {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuerSigningKey=true, IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                    ValidateIssuer=true, ValidIssuer="ptcenter-local-tests", ValidateAudience=true, ValidAudience="ptcenter-local-tests",
                    ValidateLifetime=true, ClockSkew=TimeSpan.Zero, RoleClaimType=ClaimTypes.Role
                };
            });
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(new SqliteConnectionStringBuilder { DataSource=databasePath, DefaultTimeout=15, Pooling=false, ForeignKeys=true }.ToString()));
        });
    }
    internal static string Token(string role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: "ptcenter-local-tests", audience: "ptcenter-local-tests", claims: [new(ClaimTypes.NameIdentifier, "local-staff"), new(ClaimTypes.Role, role)],
        expires: DateTime.UtcNow.AddHours(1), signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256)));
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(databasePath + suffix)) File.Delete(databasePath + suffix);
    }
}

public class BookingHttpTests
{
    [Fact] public async Task Booking_requires_authenticated_staff_and_preserves_role_restrictions()
    {
        using var host = new BookingTestHost();
        using var client = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Bookings/patients/1")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", BookingTestHost.Token("Patient"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Bookings", new BookingRequest())).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", BookingTestHost.Token("Receptionist"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Bookings/assessments/complete", new CompleteAssessmentRequest())).StatusCode);
    }

    [Fact] public async Task Authenticated_preview_booking_and_reconciliation_use_real_local_database_records()
    {
        using var host = new BookingTestHost();
        using var client = host.CreateClient(new() { BaseAddress = new("https://localhost") });
        int id;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(new ApplicationUser { Id="http-patient",UserName="http.patient",FullName="Local test patient",Gender=Gender.Female },
                new ApplicationUser { Id="http-senior",UserName="http.senior",FullName="Local Senior",Gender=Gender.Female });
            db.UserRoles.Add(new() { UserId="http-senior",RoleId=(await db.Roles.SingleAsync(r=>r.Name=="Senior")).Id });
            var patient=new Patient{ApplicationUserId="http-patient",PaymentType="Cash"};db.Patients.Add(patient);
            db.Rooms.Add(new(){Id=1,Name="Test room",Capacity=1});
            for(var day=0;day<7;day++) db.DoctorSchedules.Add(new(){DoctorId="http-senior",DayOfWeek=day,StartTime=new(8,0),EndTime=new(20,0)});
            await db.SaveChangesAsync();id=patient.Id;
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BookingTestHost.Token("Receptionist"));
        var options = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/Bookings/patients");
        var option = options.GetProperty("items")[0];
        Assert.False(option.TryGetProperty("insuranceDetails", out _)); Assert.False(option.TryGetProperty("assessments", out _));
        var context = await client.GetFromJsonAsync<BookingContext>($"/api/Bookings/patients/{id}"); Assert.NotNull(context); Assert.False(context.HasCompletedAssessment);
        var request = new BookingRequest { IdempotencyKey=Guid.NewGuid().ToString(),PatientId=id,ClinicianId="http-senior",RoomId=1,
            StartsAt=[new DateTimeOffset(2030,2,4,10,0,0,TimeSpan.Zero)],AssessmentCharge="Paid",UnitPrice=100 };
        var quote = await client.PostAsJsonAsync("/api/Bookings/preview",request); Assert.Equal(HttpStatusCode.OK,quote.StatusCode);
        Assert.True((await quote.Content.ReadFromJsonAsync<BookingPreview>())!.CanSave);
        var saves = await Task.WhenAll(client.PostAsJsonAsync("/api/Bookings",request), client.PostAsJsonAsync("/api/Bookings",request));
        Assert.All(saves, save => Assert.Equal(HttpStatusCode.OK,save.StatusCode));
        var save = saves[0];
        var receipt = (await save.Content.ReadFromJsonAsync<BookingReceipt>())!; Assert.Equal(0,receipt.PaidAmount); Assert.Equal(100,receipt.RemainingAmount);
        var retry = await client.PostAsJsonAsync("/api/Bookings",request); Assert.Equal(HttpStatusCode.OK,retry.StatusCode);
        var reconciled = (await client.GetFromJsonAsync<BookingReceipt>($"/api/Bookings/operations/{request.IdempotencyKey}"))!;
        Assert.Equal(receipt.Appointments[0].Id,reconciled.Appointments[0].Id);
        var directory = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/Appointments/GetAll");
        var appointmentJson = directory.GetProperty("items")[0];
        Assert.EndsWith("Z", appointmentJson.GetProperty("startTime").GetString());
        Assert.Equal("Pending", appointmentJson.GetProperty("status").GetString());
        using var verify = host.Services.CreateScope();var savedDb=verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1,await savedDb.Appointments.CountAsync()); Assert.Equal(1,await savedDb.Invoices.CountAsync()); Assert.Equal(0,await savedDb.Payments.CountAsync());
    }
}
