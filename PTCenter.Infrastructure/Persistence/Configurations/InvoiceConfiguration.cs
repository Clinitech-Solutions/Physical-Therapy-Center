
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.InvoiceNumber)
            .HasMaxLength(50)
            .IsRequired();

        b.HasIndex(x => x.InvoiceNumber)
            .IsUnique();

        b.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.InsuranceAmount).HasPrecision(18, 2);
        b.Property(x => x.PatientAmount).HasPrecision(18, 2);
        b.Property(x => x.PaidAmount).HasPrecision(18, 2);

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Payments)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
