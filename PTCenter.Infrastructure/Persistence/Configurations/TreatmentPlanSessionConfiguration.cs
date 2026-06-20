using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Infrastructure.Persistence.Configurations
{
    public class TreatmentPlanSessionConfiguration : IEntityTypeConfiguration<TreatmentPlanSession>
    {
        public void Configure(EntityTypeBuilder<TreatmentPlanSession> builder)
        {

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.TreatmentPlan)
                   .WithMany(x => x.Sessions)
                   .HasForeignKey(x => x.TreatmentPlanId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(x => !x.IsDeleted);

        }
    }
}
