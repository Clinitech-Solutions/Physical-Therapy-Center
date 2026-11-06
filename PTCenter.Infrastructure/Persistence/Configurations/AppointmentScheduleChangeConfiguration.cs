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
        b.Property(x=>x.Action).HasMaxLength(40).HasDefaultValue("Reschedule");
        b.HasIndex(x=>x.AppointmentId);
        b.Property(x=>x.PreviousClinicianId).HasMaxLength(450);
        b.Property(x=>x.PreviousClinicianName).HasMaxLength(150);
        b.Property(x=>x.ClinicianName).HasMaxLength(150);
        b.Property(x=>x.ContactOutcome).HasMaxLength(40);
        b.Property(x=>x.ConsentMethod).HasMaxLength(20);
        b.Property(x=>x.ConsentReason).HasMaxLength(500);
        b.HasIndex(x=>new{x.AppointmentId,x.AbsenceId,x.ChangedAt});
        b.HasOne<DoctorAbsence>().WithMany().HasForeignKey(x=>x.AbsenceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>x.OperationKey).IsUnique();
        b.HasIndex(x=>new{x.PatientId,x.TreatmentPlanId,x.ChangedAt});
        b.HasOne<Appointment>().WithMany().HasForeignKey(x=>x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Patient>().WithMany().HasForeignKey(x=>x.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}
