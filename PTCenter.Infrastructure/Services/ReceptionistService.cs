using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.Receptionist;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class ReceptionistService(ApplicationDbContext context) : IReceptionistService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<ReceptionistDashboardStatsResponse>> GetDashboardStatsAsync()
    {
        var today = DateTime.Today;

        var todaysSessionsQuery = _context.Appointments
            .Where(x => x.StartTime.Date == today);

        var todaysSessionsCount = await todaysSessionsQuery.CountAsync();

        var completedSessionsCount = await todaysSessionsQuery
            .Where(x => x.Status == AppointmentStatus.Completed)
            .CountAsync();

        var pendingPaymentsCount = await _context.Invoices
            .Where(x => x.Status == InvoiceStatus.Pending)
            .CountAsync();

        var invoicesToday = await _context.Invoices
            .Where(x => x.CreatedAt.Date == today)
            .ToListAsync();

        var totalExpected = invoicesToday.Sum(x => x.TotalAmount);
        var totalCollected = invoicesToday.Sum(x => x.PatientAmount);

        double collectionRate = totalExpected > 0 ? (double)(totalCollected / totalExpected) * 100 : 0;

        var response = new ReceptionistDashboardStatsResponse
        {
            TodaysSessions = todaysSessionsCount,
            CompletedSessions = completedSessionsCount,
            PendingPayments = pendingPaymentsCount,
            TotalExpectedToday = totalExpected,
            TotalCollectedToday = totalCollected,
            CollectionRate = Math.Round(collectionRate, 2)
        };

        return Result.Success(response);
    }

    public async Task<Result<PagedResponse<TodaySessionResponse>>> GetTodaySessionsAsync(TodaySessionsQueryRequest request)
    {
        if (request.PageNumber < 1) request.PageNumber = 1;
        if (request.PageSize < 1) request.PageSize = 10;

        var today = DateTime.Today;

        var query = _context.Appointments
            .Include(x => x.Patient)
                .ThenInclude(p => p.ApplicationUser)
            .Include(x => x.Doctor)
            .Include(x => x.Room)
            .Include(x => x.TreatmentPlanSession)
            .Where(x => x.StartTime.Date == today)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Patient.ApplicationUser.FullName.Contains(search) ||
                                     x.Patient.ApplicationUser.PhoneNumber!.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.DoctorId))
        {
            query = query.Where(x => x.DoctorId == request.DoctorId);
        }

        if (request.RoomId.HasValue)
        {
            query = query.Where(x => x.RoomId == request.RoomId.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var sessions = await query
            .OrderBy(x => x.StartTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new TodaySessionResponse
            {
                Id = x.Id,
                Time = x.StartTime.ToString("hh:mm tt"),
                PatientName = x.Patient.ApplicationUser.FullName,
                SessionNumber = x.TreatmentPlanSession != null ? $"Session {x.TreatmentPlanSession.SessionNumber}" : null,
                DoctorName = x.Doctor != null ? x.Doctor.FullName : "Not Assigned",
                RoomName = x.Room != null ? x.Room.Name : "No Room",
                Status = x.Status.ToString()
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<TodaySessionResponse>
        {
            Items = sessions,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Result.Success(pagedResponse);
    }

  

    public async Task<Result> UpdateAppointmentStatusAsync(int appointmentId, AppointmentStatus newStatus)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
        {
            return Result.Failure(ReceptionistErrors.AppointmentNotFound);
        }

        appointment.Status = newStatus;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> CancelAppointmentAsAbsentAsync(int appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
        {
            return Result.Failure(ReceptionistErrors.AppointmentNotFound);
        }

        appointment.Status = AppointmentStatus.Cancelled;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Result.Success();
    }
}