using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Infrastructure.Persistence.Configurations
{
    public class DoctorAbsenceConfiguration : IEntityTypeConfiguration<DoctorAbsence> 
    {
        public void Configure(EntityTypeBuilder<DoctorAbsence> b) 
        { 
            b.HasKey(x => x.Id);
            b.Property(x => x.Version).IsConcurrencyToken();
            b.Property(x => x.Version).HasDefaultValue(1);
            b.Property(x => x.HistoryJson).HasDefaultValue("[]");
            b.Property(x => x.OperationKey).HasMaxLength(100);
            b.HasIndex(x => x.OperationKey).IsUnique().HasFilter("[OperationKey] IS NOT NULL");
            b.HasIndex(x => new { x.StartsAt, x.EndsAt });
            b.HasQueryFilter(x => !x.IsDeleted); 
        }
    }
}
