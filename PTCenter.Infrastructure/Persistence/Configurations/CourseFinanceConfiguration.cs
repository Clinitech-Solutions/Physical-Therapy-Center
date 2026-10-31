using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class CourseFinanceConfiguration : IEntityTypeConfiguration<TreatmentCourseAgreement>, IEntityTypeConfiguration<InsuranceClaim>
{
    public void Configure(EntityTypeBuilder<TreatmentCourseAgreement> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.TreatmentPlanId).IsUnique();
        b.HasOne<TreatmentPlan>().WithMany().HasForeignKey(x => x.TreatmentPlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<InsuranceCompany>().WithMany().HasForeignKey(x => x.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.FundingType).HasMaxLength(20); b.Property(x => x.PricingMode).HasMaxLength(20);
        b.Property(x => x.DiscountType).HasMaxLength(20); b.Property(x => x.ApprovalReference).HasMaxLength(100);
        foreach (var name in new[] { "UnitPrice", "DiscountValue", "DiscountAmount", "GrossAmount", "TotalAmount", "PatientAmount", "InsuranceAmount", "CopayPercentage" }) b.Property<decimal>(name).HasPrecision(18, 2);
    }
    public void Configure(EntityTypeBuilder<InsuranceClaim> b)
    {
        b.HasKey(x => x.Id); b.HasIndex(x => x.InvoiceId).IsUnique(); b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.IdempotencyKey).HasMaxLength(100); b.Property(x => x.SubmissionReference).HasMaxLength(150);
        b.Property(x => x.ApprovalReference).HasMaxLength(100); b.Property(x => x.SubmittedBy).HasMaxLength(450); b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreatmentCourseAgreement>().WithMany().HasForeignKey(x => x.CourseAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TreatmentPlanSession>().WithMany().HasForeignKey(x => x.TreatmentPlanSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<InsuranceCompany>().WithMany().HasForeignKey(x => x.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
