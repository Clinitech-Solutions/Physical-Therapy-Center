using PTCenter.Application.Common.Constants;
using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Appointments
{
    public class AppointmentQueryRequest : PagedRequest
    {
        public string? Search { get; set; }
        public string? DoctorId { get; set; }
        public int? RoomId { get; set; }
        /// <summary>Clinic-local calendar date. Prefer YYYY-MM-DD; legacy ISO date labels ignore the time component.</summary>
        public DateTime? Date { get; set; }
        public AppointmentStatus? Status { get; set; } // أضف هذه لفلترة الحالات في جدول الحجوزات
    }
}
