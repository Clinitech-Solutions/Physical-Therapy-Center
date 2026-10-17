using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Receptionist
{
         public class TodaySessionResponse
        {
            public int Id { get; set; }
            public string Time { get; set; } = default!;
            public string PatientName { get; set; } = default!;
            public string? SessionNumber { get; set; } // مثال: Session 5 of 10
            public string DoctorName { get; set; } = default!;
            public string? RoomName { get; set; }
            public string Status { get; set; } = default!;
        }
    }

