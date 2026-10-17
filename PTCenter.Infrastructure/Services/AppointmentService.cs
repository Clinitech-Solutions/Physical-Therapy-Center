using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.Appointments;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Infrastructure.Services;

public class AppointmentService(ApplicationDbContext context) : IAppointmentService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(AppointmentQueryRequest request)
    {
        if (request.PageNumber < 1) request.PageNumber = 1;
        if (request.PageSize < 1) request.PageSize = 10;

        var query = _context.Appointments
            .Include(x => x.Patient)
                .ThenInclude(p => p.ApplicationUser)
            .Include(x => x.Doctor)
            .Include(x => x.Room)
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

        if (request.Date.HasValue)
        {
            query = query.Where(x => x.StartTime.Date == request.Date.Value.Date);
        }
        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var items = await query
            .OrderByDescending(x => x.StartTime)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AppointmentResponse
            {
                Id = x.Id,
                PatientId = x.PatientId,
                PatientName = x.Patient.ApplicationUser.FullName,
                DoctorId = x.DoctorId,
                DoctorName = x.Doctor != null ? x.Doctor.FullName : null,
                SeniorId = x.SeniorId,
                RoomId = x.RoomId,
                RoomName = x.Room != null ? x.Room.Name : null,
                TreatmentPlanSessionId = x.TreatmentPlanSessionId,
                AppointmentType = x.AppointmentType,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<AppointmentResponse>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Result.Success(pagedResponse);
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(int id)
    {
        var x = await _context.Appointments
            .Include(x => x.Patient)
                .ThenInclude(p => p.ApplicationUser)
            .Include(x => x.Doctor)
            .Include(x => x.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (x is null)
            return Result.Failure<AppointmentResponse>(AppointmentErrors.AppointmentNotFound);

        var response = new AppointmentResponse
        {
            Id = x.Id,
            PatientId = x.PatientId,
            PatientName = x.Patient.ApplicationUser.FullName,
            DoctorId = x.DoctorId,
            DoctorName = x.Doctor?.FullName,
            SeniorId = x.SeniorId,
            RoomId = x.RoomId,
            RoomName = x.Room?.Name,
            TreatmentPlanSessionId = x.TreatmentPlanSessionId,
            AppointmentType = x.AppointmentType,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            Status = x.Status,
            CreatedAt = x.CreatedAt
        };

        return Result.Success(response);
    }

    public async Task<Result<AppointmentResponse>> AddAsync(CreateAppointmentRequest request)
    {
        var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId);
        if (!patientExists)
            return Result.Failure<AppointmentResponse>(AppointmentErrors.PatientNotFound);

        // التحقق من تعارض الغرفة
        if (request.RoomId.HasValue)
        {
            var roomConflict = await _context.Appointments
                .AnyAsync(x => x.RoomId == request.RoomId.Value &&
                               x.Status != AppointmentStatus.Cancelled &&
                               ((request.StartTime >= x.StartTime && request.StartTime < x.EndTime) ||
                                (request.EndTime > x.StartTime && request.EndTime <= x.EndTime)));

            if (roomConflict)
                return Result.Failure<AppointmentResponse>(AppointmentErrors.RoomConflict);
        }

        // التحقق من تعارض الدكتور
        if (!string.IsNullOrWhiteSpace(request.DoctorId))
        {
            var doctorConflict = await _context.Appointments
                .AnyAsync(x => x.DoctorId == request.DoctorId &&
                               x.Status != AppointmentStatus.Cancelled &&
                               ((request.StartTime >= x.StartTime && request.StartTime < x.EndTime) ||
                                (request.EndTime > x.StartTime && request.EndTime <= x.EndTime)));

            if (doctorConflict)
                return Result.Failure<AppointmentResponse>(AppointmentErrors.DoctorConflict);
        }

        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            SeniorId = request.SeniorId,
            RoomId = request.RoomId,
            TreatmentPlanSessionId = request.TreatmentPlanSessionId,
            AppointmentType = request.AppointmentType,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(appointment.Id);
    }

    public async Task<Result<AppointmentResponse>> UpdateAsync(int id, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
            return Result.Failure<AppointmentResponse>(AppointmentErrors.AppointmentNotFound);

        appointment.DoctorId = request.DoctorId;
        appointment.SeniorId = request.SeniorId;
        appointment.RoomId = request.RoomId;
        appointment.AppointmentType = request.AppointmentType;
        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        appointment.Status = request.Status;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
            return Result.Failure(AppointmentErrors.AppointmentNotFound);

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateStatusAsync(int id, string status)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
            return Result.Failure(AppointmentErrors.AppointmentNotFound);

        appointment.Status = (AppointmentStatus)Enum.Parse(typeof(AppointmentStatus), status);
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        return Result.Success();
    }
}