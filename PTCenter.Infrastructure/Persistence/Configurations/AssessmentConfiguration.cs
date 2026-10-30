using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PTCenter.Domain.Entities;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.AppointmentId).IsUnique().HasFilter("[AppointmentId] IS NOT NULL");
        builder.HasOne<Appointment>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Diagnosis)
            .IsRequired();

        builder.Property(x => x.ConditionType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Senior)
            .WithMany()
            .HasForeignKey(x => x.SeniorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PatientId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
