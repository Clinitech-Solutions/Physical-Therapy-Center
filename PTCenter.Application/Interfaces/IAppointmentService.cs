using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Appointments;
using PTCenter.Application.Contracts.Common;

namespace PTCenter.Application.Interfaces;

public interface IAppointmentService
{
    Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(AppointmentQueryRequest request);
    Task<Result<AppointmentResponse>> GetByIdAsync(int id);
    Task<Result<AppointmentResponse>> AddAsync(CreateAppointmentRequest request);
    Task<Result<AppointmentResponse>> UpdateAsync(int id, UpdateAppointmentRequest request);
    Task<Result> DeleteAsync(int id);
    Task<Result> UpdateStatusAsync(int id, string status);
}