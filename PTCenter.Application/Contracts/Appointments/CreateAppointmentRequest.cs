using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Appointments
{
    public class CreateAppointmentRequest
    {
        public int PatientId { get; set; }
        public string? DoctorId { get; set; }
        public string? SeniorId { get; set; }
        public int? RoomId { get; set; }
        public int? TreatmentPlanSessionId { get; set; }
        public string AppointmentType { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
