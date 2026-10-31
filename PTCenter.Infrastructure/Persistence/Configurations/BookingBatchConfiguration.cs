using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class BookingBatchConfiguration : IEntityTypeConfiguration<BookingBatch>
{
    public void Configure(EntityTypeBuilder<BookingBatch> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne<TreatmentCourseAgreement>().WithMany().HasForeignKey(x => x.CourseAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Property(x => x.RequestHash).HasMaxLength(64);
        b.Property(x => x.PricingMode).HasMaxLength(30);
        b.Property(x => x.WaiverReason).HasMaxLength(100);
        b.Property(x => x.GrossAmount).HasPrecision(18, 2);
        b.Property(x => x.Discount).HasPrecision(18, 2);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.PatientAmount).HasPrecision(18, 2);
        b.Property(x => x.InsuranceAmount).HasPrecision(18, 2);
        b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}
