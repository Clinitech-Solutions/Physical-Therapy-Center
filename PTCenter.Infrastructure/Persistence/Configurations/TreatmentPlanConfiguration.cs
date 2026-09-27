using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class TreatmentPlanConfiguration
    : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Assessment)
            .WithOne(x => x.TreatmentPlan)
            .HasForeignKey<TreatmentPlan>(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AssessmentId);

        builder.HasIndex(x => x.DoctorId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}