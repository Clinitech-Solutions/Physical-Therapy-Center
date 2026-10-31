using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Bookings;

public record CourseFinancialSummary(int? AgreementId, int TreatmentPlanId, string FundingType, string PricingMode,
    int PrescribedSessions, int CoveredSessions, int? ApprovedSessions, decimal UnitPrice, string DiscountType, decimal DiscountValue,
    decimal GrossAmount, decimal DiscountAmount, decimal TotalAmount, decimal PatientAmount, decimal InsuranceAmount,
    decimal CopayPercentage, decimal PaidAmount, decimal RemainingAmount, int ConsumedSessions, decimal NextSessionPatientAmount,
    decimal AdditionalPaymentRequired, bool Reused, string? ApprovalReference);
public record PaymentGate(int AppointmentId, bool CanStart, string AccountKey, int? InvoiceId, string PricingMode,
    decimal PatientPaid, decimal RequiredCumulative, decimal AdditionalPaymentRequired, decimal PatientBalance, int ConsumedSessions);
public record SubmitInsuranceClaimRequest(string IdempotencyKey, int InvoiceId, string SubmissionReference);
public record SettleInsuranceClaimRequest(string IdempotencyKey, decimal Amount, PaymentMethod Method, string SettlementReference);
public record InsuranceReceivable(int InvoiceId, int? ClaimId, int? CourseAgreementId, int? TreatmentPlanSessionId, int PatientId,
    string PatientName, int InsuranceCompanyId, string CompanyName, string? ApprovalReference, DateTime? DeliveredAt,
    decimal PlannedAmount, decimal EarnedAmount, decimal SubmittedAmount, decimal CollectedAmount, decimal OutstandingAmount, string? SubmissionReference)
{
    public bool CanSubmitClaim { get; init; }
}
public record InsurerPatientBalance(int PatientId, string PatientName, decimal Planned, decimal Earned, decimal Submitted, decimal Collected, decimal Outstanding);
public record InsurerCompanyBalance(int InsuranceCompanyId, string CompanyName, decimal Planned, decimal Earned, decimal Submitted,
    decimal Collected, decimal Outstanding, List<InsurerPatientBalance> Patients);
public record InsurerAccounts(List<InsurerCompanyBalance> Companies, List<InsuranceReceivable> Receivables);
