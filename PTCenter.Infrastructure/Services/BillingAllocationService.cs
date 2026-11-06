using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public record BillingAllocationItem(int InvoiceId,string Reference,decimal Balance,decimal Amount);
public record BillingAllocation(string AccountKey,decimal Amount,decimal Remaining,string Hash,IReadOnlyList<BillingAllocationItem> Items);
public sealed class BillingAllocationService(ApplicationDbContext db)
{
    public async Task<BillingAllocation> PreviewAsync(string key,decimal amount,CancellationToken ct=default)
    {
        if(!CourseFinanceService.Money(amount) || amount<=0) throw new BookingRuleException("Billing.InvalidAmount","Enter a positive currency amount.",400);
        int? courseId=key.StartsWith("course:") && int.TryParse(key[7..],out var id) ? id : await db.BookingBatches.Where(b=>b.IdempotencyKey==key).Select(b=>b.CourseAgreementId).SingleOrDefaultAsync(ct);
        var query=db.Invoices.AsNoTracking().Where(i=>i.Status!=InvoiceStatus.Waived && i.Status!=InvoiceStatus.Cancelled);
        query=courseId is {} cid ? query.Where(i=>i.CourseAgreementId==cid) : query.Where(i=>i.Appointment!=null && db.BookingBatches.Any(b=>b.Id==i.Appointment.BookingBatchId && b.IdempotencyKey==key));
        var invoices=await query.Include(i=>i.Payments).OrderBy(i=>i.Id).ToListAsync(ct);
        if(invoices.Count==0) throw new BookingRuleException("Billing.AccountNotFound","No collectible invoices exist for this account.",404);
        var balances=invoices.Select(i=>new BillingAllocationItem(i.Id,i.InvoiceNumber,Math.Max(0,i.PatientAmount-i.Payments.Where(CourseFinanceService.PatientReceipt).Sum(p=>p.Amount)),0)).ToList();
        var balance=balances.Sum(i=>i.Balance);
        if(amount>balance) throw new BookingRuleException("Billing.PaymentExceedsBalance","Payment exceeds the remaining patient balance.",400);
        var remaining=amount;var items=new List<BillingAllocationItem>();
        foreach(var item in balances) { var allocation=Math.Min(remaining,item.Balance);if(allocation>0)items.Add(item with { Amount=allocation });remaining-=allocation; }
        var fingerprint=key+"|"+amount.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"|"+string.Join("|",balances.Select(i=>i.InvoiceId+":"+i.Balance.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)));
        return new(key,amount,balance,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint))),items);
    }
}
