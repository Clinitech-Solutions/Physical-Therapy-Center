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
            builder.HasOne(x => x.ApplicationUser).WithMany(x => x.Patients).HasForeignKey(x => x.ApplicationUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.InsuranceCompany).WithMany(x => x.Patients).HasForeignKey(x => x.InsuranceCompanyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasQueryFilter(x => !x.IsDeleted);

        }
    }
}
