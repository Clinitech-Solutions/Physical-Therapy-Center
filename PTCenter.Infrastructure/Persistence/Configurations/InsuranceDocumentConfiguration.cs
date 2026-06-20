using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Infrastructure.Persistence.Configurations
{
    public class InsuranceDocumentConfiguration : IEntityTypeConfiguration<InsuranceDocument>
    {
        public void Configure(EntityTypeBuilder<InsuranceDocument> builder)
        {

            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Patient).WithMany(x => x.InsuranceDocuments).HasForeignKey(x => x.PatientId);
            builder.HasQueryFilter(x => !x.IsDeleted);

        }
    }
}
