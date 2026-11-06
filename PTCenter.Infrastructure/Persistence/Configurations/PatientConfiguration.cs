using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Infrastructure.Persistence.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {

            builder.HasKey(x => x.Id);
            builder.Property(x=>x.RegistrationOperationKey).HasMaxLength(100);
            builder.Property(x=>x.RegistrationRequestHash).HasMaxLength(64);
            builder.HasIndex(x=>x.RegistrationOperationKey).IsUnique().HasFilter("[RegistrationOperationKey] IS NOT NULL");
            builder.HasOne(x => x.ApplicationUser).WithMany(x => x.Patients).HasForeignKey(x => x.ApplicationUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.InsuranceCompany).WithMany(x => x.Patients).HasForeignKey(x => x.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasQueryFilter(x => !x.IsDeleted);
            builder.OwnsOne(p => p.InsuranceDetails);

        }
    }
}
