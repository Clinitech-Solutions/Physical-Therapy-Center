using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.Receptionist;
using PTCenter.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Interfaces
{
    public interface IReceptionistService
    {
        Task<Result<ReceptionistDashboardStatsResponse>> GetDashboardStatsAsync();
        Task<Result<PagedResponse<TodaySessionResponse>>> GetTodaySessionsAsync(TodaySessionsQueryRequest request);
        Task<Result> UpdateAppointmentStatusAsync(int appointmentId, AppointmentStatus newStatus);
        Task<Result> CancelAppointmentAsAbsentAsync(int appointmentId);
    }
}
