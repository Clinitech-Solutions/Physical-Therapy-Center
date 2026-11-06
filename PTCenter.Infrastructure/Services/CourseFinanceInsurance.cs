using System.Data;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Bookings;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Infrastructure.Services;

public partial class CourseFinanceService
{
    public async Task<InsurerAccounts> InsuranceAccountsAsync()
    {
        var invoices = await db.Invoices.AsNoTracking().Include(i => i.Payments).Include(i => i.Patient).ThenInclude(p => p.ApplicationUser)
            .Include(i => i.Patient).ThenInclude(p => p.InsuranceCompany).Include(i => i.Appointment).Where(i => i.InsuranceAmount > 0).ToListAsync();
        var agreements = await db.TreatmentCourseAgreements.AsNoTracking().ToDictionaryAsync(a => a.Id);
        var companiesById = await db.InsuranceCompanies.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name);
        var claims = await db.InsuranceClaims.AsNoTracking().ToDictionaryAsync(c => c.InvoiceId);
        var rows = new List<InsuranceReceivable>();
        foreach (var i in invoices)
        {
            var agreement = i.CourseAgreementId is int id ? agreements.GetValueOrDefault(id) : null;
            var claim = claims.GetValueOrDefault(i.Id);
            var companyId = agreement?.InsuranceCompanyId ?? claim?.InsuranceCompanyId;
            var delivered = i.DeliveredAt ?? (i.Appointment?.Status == AppointmentStatus.Completed ? i.Appointment.ActualCheckOut : null);
            var collected = i.Payments.Where(p => InsurerReceipt(p)).Sum(p => p.Amount);
            var earned = delivered != null && i.Status is not (InvoiceStatus.Waived or InvoiceStatus.Cancelled) ? i.InsuranceAmount : 0;
            rows.Add(new(i.Id, claim?.Id, i.CourseAgreementId, i.TreatmentPlanSessionId, i.PatientId, i.Patient.ApplicationUser.FullName,
                companyId ?? 0, companyId is {} cid ? companiesById.GetValueOrDefault(cid) ?? "Historical company unavailable" : "Historical company unavailable", agreement?.ApprovalReference, delivered, i.InsuranceAmount, earned,
                claim?.Amount ?? 0, collected, Math.Max(0, earned - collected), claim?.SubmissionReference) {
                    CanSubmitClaim = i.DeliveredAt != null && agreement?.ApprovalReference != null && i.TreatmentPlanSessionId != null && claim == null });
        }
        var companies = rows.GroupBy(i => new { i.InsuranceCompanyId, i.CompanyName }).Select(group => new InsurerCompanyBalance(group.Key.InsuranceCompanyId,
            group.Key.CompanyName, group.Sum(i => i.PlannedAmount), group.Sum(i => i.EarnedAmount), group.Sum(i => i.SubmittedAmount), group.Sum(i => i.CollectedAmount), group.Sum(i => i.OutstandingAmount),
            group.GroupBy(i => new { i.PatientId, i.PatientName }).Select(patient => new InsurerPatientBalance(patient.Key.PatientId, patient.Key.PatientName,
                patient.Sum(i => i.PlannedAmount), patient.Sum(i => i.EarnedAmount), patient.Sum(i => i.SubmittedAmount), patient.Sum(i => i.CollectedAmount), patient.Sum(i => i.OutstandingAmount))).ToList())).ToList();
        return new(companies, rows);
    }

    public async Task<InsuranceClaim> SubmitClaimAsync(SubmitInsuranceClaimRequest request, string actor)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(db);
        var claim=await SubmitClaimCoreAsync(request,actor);
        await transaction.CommitAsync();return claim;
    }

    internal async Task<InsuranceClaim> SubmitClaimCoreAsync(SubmitInsuranceClaimRequest request, string actor)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 90 || string.IsNullOrWhiteSpace(request.SubmissionReference) || request.SubmissionReference.Length > 150)
            throw Rule("ClaimReferenceRequired", "Enter the submitted claim reference and a stable retry key.");
        var submissionReference = request.SubmissionReference.Trim();
        var previous = await db.InsuranceClaims.SingleOrDefaultAsync(c => c.IdempotencyKey == request.IdempotencyKey);
        if (previous != null)
        {
            if (previous.InvoiceId != request.InvoiceId || previous.SubmissionReference != submissionReference) throw Rule("RetryKeyConflict", "This claim key belongs to a different submission.", 409);
            return previous;
        }
        var invoice = await db.Invoices.SingleOrDefaultAsync(i => i.Id == request.InvoiceId) ?? throw Rule("InvoiceNotFound", "Invoice was not found.", 404);
        if (invoice.Status is InvoiceStatus.Waived or InvoiceStatus.Cancelled || invoice.CourseAgreementId == null || invoice.TreatmentPlanSessionId == null || invoice.DeliveredAt == null || invoice.InsuranceAmount <= 0)
            throw Rule("DeliveredServiceRequired", "Only verified delivered insured sessions can be submitted. Legacy records need financial review.", 409);
        var agreement = await db.TreatmentCourseAgreements.SingleAsync(a => a.Id == invoice.CourseAgreementId);
        if (agreement.InsuranceCompanyId == null || agreement.ApprovalReference == null) throw Rule("ApprovalRequired", "The saved approval is required.", 409);
        if (await db.InsuranceClaims.AnyAsync(c => c.InvoiceId == invoice.Id)) throw Rule("ClaimAlreadySubmitted", "This invoice already has a submitted claim.", 409);
        var claim = new InsuranceClaim { InvoiceId = invoice.Id, CourseAgreementId = agreement.Id, TreatmentPlanSessionId = invoice.TreatmentPlanSessionId.Value,
            PatientId = invoice.PatientId, InsuranceCompanyId = agreement.InsuranceCompanyId.Value, ApprovalReference = agreement.ApprovalReference,
            Amount = invoice.InsuranceAmount, SubmissionReference = submissionReference, IdempotencyKey = request.IdempotencyKey, SubmittedAt = DateTime.UtcNow, SubmittedBy = actor };
        db.InsuranceClaims.Add(claim); await db.SaveChangesAsync(); return claim;
    }

    public async Task<int> SettleClaimAsync(int claimId, SettleInsuranceClaimRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 90 || !Money(request.Amount) || request.Amount <= 0 || !Enum.IsDefined(request.Method) ||
            request.Method == PaymentMethod.Insurance || string.IsNullOrWhiteSpace(request.SettlementReference) || request.SettlementReference.Length > 150)
            throw Rule("SettlementDetailsRequired", "Enter the actual insurer receipt amount, collection method and settlement reference.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(db);
        var key = $"insurer:{request.IdempotencyKey}";
        var previous = await db.Payments.SingleOrDefaultAsync(p => p.IdempotencyKey == key);
        if (previous != null)
        {
            if (previous.InsuranceClaimId != claimId || previous.Amount != request.Amount || previous.CollectionMethod != request.Method.ToString() || previous.TransactionId != request.SettlementReference)
                throw Rule("RetryKeyConflict", "This settlement key belongs to different receipt details.", 409);
            return previous.Id;
        }
        var paymentId=await AllocateInsurerReceiptAsync(claimId,request.Amount,request.Method.ToString(),request.SettlementReference,key,DateTime.UtcNow);
        await transaction.CommitAsync();return paymentId;
    }

    internal async Task<int> AllocateInsurerReceiptAsync(int claimId,decimal amount,string method,string reference,string key,DateTime paymentDate,int? chequeId=null,DateOnly? serviceMonth=null,DateTime? deliveredAt=null)
    {
        if(!Money(amount) || amount<=0)throw Rule("SettlementDetailsRequired","Enter a positive actual insurer receipt.");
        var claim = await db.InsuranceClaims.SingleOrDefaultAsync(c => c.Id == claimId) ?? throw Rule("ClaimNotFound", "The submitted claim was not found.", 404);
        var invoice = await db.Invoices.Include(i => i.Payments).SingleAsync(i => i.Id == claim.InvoiceId);
        if (invoice.Status is InvoiceStatus.Waived or InvoiceStatus.Cancelled) throw Rule("CannotReceivePayment", "A waived or cancelled invoice cannot receive insurer money.", 409);
        var received = invoice.Payments.Where(p => InsurerReceipt(p)).Sum(p => p.Amount);
        if (amount > claim.Amount - received) throw Rule("SettlementExceedsBalance", "The insurer receipt exceeds this claim's outstanding balance.");
        var payment = new Payment { InvoiceId = invoice.Id, InsuranceClaimId = claim.Id, Amount = amount, PaymentMethod = PaymentMethod.Insurance,
            PayerType = "Insurer", CollectionMethod = method, IdempotencyKey = key, TransactionId = reference,
            PaymentDate = paymentDate, InsuranceChequeReceiptId=chequeId, Status = PaymentStatus.Paid,
            InsurerBalanceBefore=chequeId!=null ? Math.Max(0,claim.Amount-received) : null, InsuranceServiceMonth=serviceMonth, InsuranceServiceDeliveredAt=deliveredAt };
        invoice.Payments.Add(payment); RefreshInvoice(invoice);
        var session = await db.TreatmentPlanSessions.SingleAsync(s => s.Id == claim.TreatmentPlanSessionId);
        session.PaymentStatus = invoice.Status == InvoiceStatus.Paid ? PaymentStatus.Paid : PaymentStatus.Pending;
        await db.SaveChangesAsync();return payment.Id;
    }
}
