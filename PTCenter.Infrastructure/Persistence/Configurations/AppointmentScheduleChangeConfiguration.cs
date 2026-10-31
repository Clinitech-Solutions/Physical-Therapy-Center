using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class AppointmentScheduleChangeConfiguration : IEntityTypeConfiguration<AppointmentScheduleChange>
{
    public void Configure(EntityTypeBuilder<AppointmentScheduleChange> b)
    {
        b.HasKey(x=>x.Id);
        b.Property(x=>x.OperationKey).HasMaxLength(100).IsRequired();
        b.Property(x=>x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x=>x.ActorId).HasMaxLength(450).IsRequired();
        b.Property(x=>x.ActorName).HasMaxLength(150).IsRequired();
        b.Property(x=>x.ClinicianId).HasMaxLength(450).IsRequired();
        b.Property(x=>x.PreviousRoomName).HasMaxLength(100).IsRequired();
        b.Property(x=>x.NewRoomName).HasMaxLength(100).IsRequired();
        b.Property(x=>x.Reason).HasMaxLength(500);
        b.HasIndex(x=>x.OperationKey).IsUnique();
        b.HasIndex(x=>new{x.PatientId,x.TreatmentPlanId,x.ChangedAt});
        b.HasOne<Appointment>().WithMany().HasForeignKey(x=>x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Patient>().WithMany().HasForeignKey(x=>x.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}
