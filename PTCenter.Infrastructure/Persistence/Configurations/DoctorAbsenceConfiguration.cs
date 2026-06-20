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
            b.HasQueryFilter(x => !x.IsDeleted); 
        }
    }
}
