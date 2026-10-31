using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Application.Contracts.Billing;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using Xunit;

namespace PTCenter.Tests;

public class FinancialApiAuditTests
{
    [Fact] public async Task Real_reception_login_has_explicit_financial_errors_and_idempotent_Online_receipts()
    {
        using var host=new BookingTestHost();using var client=host.CreateClient(new(){BaseAddress=new("https://localhost")});
        await ReceptionAccessFixture.SeedAsync(host);await ReceptionAccessFixture.LoginAsync(client);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync("/api/Billing/invoices/999999")).StatusCode);
        var booking=await client.PostAsJsonAsync("/api/Bookings",new BookingRequest{IdempotencyKey=Guid.NewGuid().ToString(),PatientId=1,ClinicianId="access-doctor",RoomId=1,TreatmentPlanId=1,StartsAt=[new DateTimeOffset(2030,2,4,10,0,0,TimeSpan.Zero)],UnitPrice=100});
        Assert.Equal(HttpStatusCode.OK,booking.StatusCode);var receipt=(await booking.Content.ReadFromJsonAsync<BookingReceipt>())!;
        var payment=new CreatePaymentRequest{InvoiceId=receipt.InvoiceIds.Single(),IdempotencyKey="online-audit",Amount=40,Method=PaymentMethod.Online};
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Billing/payments",payment)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Billing/payments",payment)).StatusCode);
        payment.Amount=41;Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync("/api/Billing/payments",payment)).StatusCode);
        await ReceptionAccessFixture.LoginAsync(client,"Patient");Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/Billing/payments",payment)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/Finance/insurance/accounts")).StatusCode);
        using var scope=host.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(40,await db.Payments.SumAsync(p=>p.Amount));Assert.Single(await db.Payments.ToListAsync());
    }
}
