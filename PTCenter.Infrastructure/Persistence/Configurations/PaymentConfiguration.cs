
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.PayerType).HasMaxLength(20); b.Property(x => x.CollectionMethod).HasMaxLength(30);
        b.HasOne<InsuranceClaim>().WithMany().HasForeignKey(x => x.InsuranceClaimId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<InsuranceChequeReceipt>().WithMany().HasForeignKey(x => x.InsuranceChequeReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.InsuranceChequeReceiptId,x.InsuranceClaimId}).IsUnique().HasFilter("[InsuranceChequeReceiptId] IS NOT NULL");
        b.Property(x=>x.InsurerBalanceBefore).HasPrecision(18,2);
        b.Property(x => x.IdempotencyKey).HasMaxLength(120);
        b.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

        b.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        b.Property(x => x.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        b.Property(x => x.TransactionId)
            .HasMaxLength(150);

        b.HasOne(x => x.Invoice)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
