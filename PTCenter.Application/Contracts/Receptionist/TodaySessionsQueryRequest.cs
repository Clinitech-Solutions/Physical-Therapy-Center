using PTCenter.Application.Common.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Contracts.Receptionist
{
    public class TodaySessionsQueryRequest : PagedRequest
    {
        public string? Search { get; set; }
        public string? DoctorId { get; set; }
        public int? RoomId { get; set; }
    }
}
