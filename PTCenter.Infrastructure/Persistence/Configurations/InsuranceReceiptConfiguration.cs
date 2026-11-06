using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class InsuranceReceiptConfiguration : IEntityTypeConfiguration<InsuranceChequeReceipt>, IEntityTypeConfiguration<InsuranceClaimSubmission>
{
    public void Configure(EntityTypeBuilder<InsuranceChequeReceipt> b)
    {
        b.HasKey(x=>x.Id);
        b.HasIndex(x=>x.IdempotencyKey).IsUnique();
        b.HasIndex(x=>x.ChequeIdentityHash).IsUnique();
        b.HasIndex(x=>new {x.InsuranceCompanyId,x.ReceivedOn});
        b.Property(x=>x.IdempotencyKey).HasMaxLength(90);
        b.Property(x=>x.RequestHash).HasMaxLength(64);
        b.Property(x=>x.ChequeIdentityHash).HasMaxLength(64);
        b.Property(x=>x.ChequeNumber).HasMaxLength(100);
        b.Property(x=>x.BankName).HasMaxLength(150);
        b.Property(x=>x.Notes).HasMaxLength(1000);
        b.Property(x=>x.RecordedBy).HasMaxLength(450);
        b.Property(x=>x.Amount).HasPrecision(18,2);
        b.HasOne<InsuranceCompany>().WithMany().HasForeignKey(x=>x.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<InsuranceClaimSubmission> b)
    {
        b.HasKey(x=>x.Id);b.HasIndex(x=>x.IdempotencyKey).IsUnique();
        b.HasIndex(x=>new{x.InsuranceCompanyId,x.ServiceMonth});
        b.Property(x=>x.IdempotencyKey).HasMaxLength(90);b.Property(x=>x.RequestHash).HasMaxLength(64);
        b.Property(x=>x.Reference).HasMaxLength(150);b.Property(x=>x.RecordedBy).HasMaxLength(450);
        b.HasOne<InsuranceCompany>().WithMany().HasForeignKey(x=>x.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
