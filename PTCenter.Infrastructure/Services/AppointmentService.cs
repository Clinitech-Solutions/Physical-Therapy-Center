//using Microsoft.EntityFrameworkCore;
//using PTCenter.Application.Contracts.Common;
//using PTCenter.Application.Contracts.Appointments;
//using PTCenter.Application.Interfaces;
//using PTCenter.Domain.Entities;
//using PTCenter.Domain.Errors;
//using PTCenter.Infrastructure.Persistence;
//using PTCenter.Domain.Entities.Enums;

//namespace PTCenter.Infrastructure.Services;

//public class AppointmentService(ApplicationDbContext context) : IAppointmentService
//{
//    private readonly ApplicationDbContext _context = context;

//    public async Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(AppointmentQueryRequest request)
//    {
//        if (request.PageNumber < 1) request.PageNumber = 1;
//        if (request.PageSize < 1) request.PageSize = 10;

//        var query = _context.Appointments
//            .Include(x => x.Patient)
//                .ThenInclude(p => p.ApplicationUser)
//            .Include(x => x.Doctor)
//            .Include(x => x.Room)
//            .AsNoTracking()
//            .AsQueryable();

//        if (!string.IsNullOrWhiteSpace(request.Search))
//        {
//            var search = request.Search.Trim();
//            query = query.Where(x => x.Patient.ApplicationUser.FullName.Contains(search) ||
//                                     x.Patient.ApplicationUser.PhoneNumber!.Contains(search));
//        }

//        if (!string.IsNullOrWhiteSpace(request.DoctorId))
//        {
//            query = query.Where(x => x.DoctorId == request.DoctorId);
//        }

//        if (request.RoomId.HasValue)
//        {
//            query = query.Where(x => x.RoomId == request.RoomId.Value);
//        }

//        if (request.Date.HasValue)
//        {
//            query = query.Where(x => x.StartTime.Date == request.Date.Value.Date);
//        }
//        if (request.Status.HasValue)
//        {
//            query = query.Where(x => x.Status == request.Status.Value);
//        }

//        var totalCount = await query.CountAsync();
//        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

//        var items = await query
//            .OrderByDescending(x => x.StartTime)
//            .Skip((request.PageNumber - 1) * request.PageSize)
//            .Take(request.PageSize)
//            .Select(x => new AppointmentResponse
//            {
//                Id = x.Id,
//                PatientId = x.PatientId,
//                PatientName = x.Patient.ApplicationUser.FullName,
//                DoctorId = x.DoctorId,
//                DoctorName = x.Doctor != null ? x.Doctor.FullName : null,
//                SeniorId = x.SeniorId,
//                RoomId = x.RoomId,
//                RoomName = x.Room != null ? x.Room.Name : null,
//                TreatmentPlanSessionId = x.TreatmentPlanSessionId,
//                AppointmentType = x.AppointmentType,
//                StartTime = x.StartTime,
//                EndTime = x.EndTime,
//                Status = x.Status,
//                CreatedAt = x.CreatedAt
//            })
//            .ToListAsync();

//        var pagedResponse = new PagedResponse<AppointmentResponse>
//        {
//            Items = items,
//            PageNumber = request.PageNumber,
//            PageSize = request.PageSize,
//            TotalCount = totalCount,
//            TotalPages = totalPages
//        };

//        return Result.Success(pagedResponse);
//    }

//    public async Task<Result<AppointmentResponse>> GetByIdAsync(int id)
//    {
//        var x = await _context.Appointments
//            .Include(x => x.Patient)
//                .ThenInclude(p => p.ApplicationUser)
//            .Include(x => x.Doctor)
//            .Include(x => x.Room)
//            .AsNoTracking()
//            .FirstOrDefaultAsync(x => x.Id == id);

//        if (x is null)
//            return Result.Failure<AppointmentResponse>(AppointmentErrors.AppointmentNotFound);

//        var response = new AppointmentResponse
//        {
//            Id = x.Id,
//            PatientId = x.PatientId,
//            PatientName = x.Patient.ApplicationUser.FullName,
//            DoctorId = x.DoctorId,
//            DoctorName = x.Doctor?.FullName,
//            SeniorId = x.SeniorId,
//            RoomId = x.RoomId,
//            RoomName = x.Room?.Name,
//            TreatmentPlanSessionId = x.TreatmentPlanSessionId,
//            AppointmentType = x.AppointmentType,
//            StartTime = x.StartTime,
//            EndTime = x.EndTime,
//            Status = x.Status,
//            CreatedAt = x.CreatedAt
//        };

//        return Result.Success(response);
//    }

//    public async Task<Result<AppointmentResponse>> AddAsync(CreateAppointmentRequest request)
//    {
//        var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId);
//        if (!patientExists)
//            return Result.Failure<AppointmentResponse>(AppointmentErrors.PatientNotFound);

//        // التحقق من تعارض الغرفة
//        if (request.RoomId.HasValue)
//        {
//            var roomConflict = await _context.Appointments
//                .AnyAsync(x => x.RoomId == request.RoomId.Value &&
//                               x.Status != AppointmentStatus.Cancelled &&
//                               ((request.StartTime >= x.StartTime && request.StartTime < x.EndTime) ||
//                                (request.EndTime > x.StartTime && request.EndTime <= x.EndTime)));

//            if (roomConflict)
//                return Result.Failure<AppointmentResponse>(AppointmentErrors.RoomConflict);
//        }

//        // التحقق من تعارض الدكتور
//        if (!string.IsNullOrWhiteSpace(request.DoctorId))
//        {
//            var doctorConflict = await _context.Appointments
//                .AnyAsync(x => x.DoctorId == request.DoctorId &&
//                               x.Status != AppointmentStatus.Cancelled &&
//                               ((request.StartTime >= x.StartTime && request.StartTime < x.EndTime) ||
//                                (request.EndTime > x.StartTime && request.EndTime <= x.EndTime)));

//            if (doctorConflict)
//                return Result.Failure<AppointmentResponse>(AppointmentErrors.DoctorConflict);
//        }

//        var appointment = new Appointment
//        {
//            PatientId = request.PatientId,
//            DoctorId = request.DoctorId,
//            SeniorId = request.SeniorId,
//            RoomId = request.RoomId,
//            TreatmentPlanSessionId = request.TreatmentPlanSessionId,
//            AppointmentType = request.AppointmentType,
//            StartTime = request.StartTime,
//            EndTime = request.EndTime,
//            Status = AppointmentStatus.Pending,
//            CreatedAt = DateTime.UtcNow
//        };

//        await _context.Appointments.AddAsync(appointment);
//        await _context.SaveChangesAsync();

//        return await GetByIdAsync(appointment.Id);
//    }

//    public async Task<Result<AppointmentResponse>> UpdateAsync(int id, UpdateAppointmentRequest request)
//    {
//        var appointment = await _context.Appointments.FindAsync(id);
//        if (appointment is null)
//            return Result.Failure<AppointmentResponse>(AppointmentErrors.AppointmentNotFound);

//        appointment.DoctorId = request.DoctorId;
//        appointment.SeniorId = request.SeniorId;
//        appointment.RoomId = request.RoomId;
//        appointment.AppointmentType = request.AppointmentType;
//        appointment.StartTime = request.StartTime;
//        appointment.EndTime = request.EndTime;
//        appointment.Status = request.Status;

//        _context.Appointments.Update(appointment);
//        await _context.SaveChangesAsync();

//        return await GetByIdAsync(id);
//    }

//    public async Task<Result> DeleteAsync(int id)
//    {
//        var appointment = await _context.Appointments.FindAsync(id);
//        if (appointment is null)
//            return Result.Failure(AppointmentErrors.AppointmentNotFound);

//        _context.Appointments.Remove(appointment);
//        await _context.SaveChangesAsync();

//        return Result.Success();
//    }

//    public async Task<Result> UpdateStatusAsync(int id, string status)
//    {
//        var appointment = await _context.Appointments.FindAsync(id);
//        if (appointment is null)
//            return Result.Failure(AppointmentErrors.AppointmentNotFound);

//        appointment.Status = (AppointmentStatus)Enum.Parse(typeof(AppointmentStatus), status);
//        _context.Appointments.Update(appointment);
//        await _context.SaveChangesAsync();

//        return Result.Success();
//    }
//}
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Appointments;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class AppointmentService(ApplicationDbContext context)
    : IAppointmentService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<PagedResponse<AppointmentResponse>>> GetAllAsync(
        AppointmentQueryRequest request)
    {
        if (request.PageNumber < 1)
            request.PageNumber = 1;

        if (request.PageSize < 1)
            request.PageSize = 10;

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

            query = query.Where(x =>
                x.Patient.ApplicationUser.FullName.Contains(search) ||
                (x.Patient.ApplicationUser.PhoneNumber != null &&
                 x.Patient.ApplicationUser.PhoneNumber.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.DoctorId))
            query = query.Where(x => x.DoctorId == request.DoctorId);

        if (request.RoomId.HasValue)
            query = query.Where(x => x.RoomId == request.RoomId.Value);

        if (request.Date.HasValue)
        {
            var date = request.Date.Value.Date;
            var nextDate = date.AddDays(1);

            query = query.Where(x =>
                x.StartTime >= date && x.StartTime < nextDate);
        }

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        var items = await query
            .OrderBy(x => x.StartTime)
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

        return Result.Success(new PagedResponse<AppointmentResponse>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    public async Task<Result<AppointmentResponse>> GetByIdAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(x => x.Patient)
                .ThenInclude(p => p.ApplicationUser)
            .Include(x => x.Doctor)
            .Include(x => x.Room)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment is null)
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.AppointmentNotFound);

        return Result.Success(MapResponse(appointment));
    }

    public async Task<Result<AppointmentResponse>> AddAsync(
        CreateAppointmentRequest request)
    {
        if (request.EndTime <= request.StartTime)
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.InvalidTime);

        if (string.IsNullOrWhiteSpace(request.AppointmentType))
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.InvalidAppointmentType);

        var patientExists = await _context.Patients
            .AnyAsync(x => x.Id == request.PatientId);

        if (!patientExists)
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.PatientNotFound);

        var validationError = await ValidateBookingAsync(
            request.DoctorId,
            request.RoomId,
            request.StartTime,
            request.EndTime);

        if (validationError is not null)
            return Result.Failure<AppointmentResponse>(validationError);

        var appointment = new Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            SeniorId = request.SeniorId,
            RoomId = request.RoomId,
            TreatmentPlanSessionId = request.TreatmentPlanSessionId,
            AppointmentType = request.AppointmentType.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(appointment.Id);
    }

    public async Task<Result<AppointmentResponse>> UpdateAsync(
        int id,
        UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment is null)
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.AppointmentNotFound);

        if (request.EndTime <= request.StartTime)
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.InvalidTime);

        if (string.IsNullOrWhiteSpace(request.AppointmentType))
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.InvalidAppointmentType);

        if (!Enum.IsDefined(typeof(AppointmentStatus), request.Status))
            return Result.Failure<AppointmentResponse>(
                AppointmentErrors.InvalidStatus);

        if (request.Status != AppointmentStatus.Cancelled)
        {
            var validationError = await ValidateBookingAsync(
                request.DoctorId,
                request.RoomId,
                request.StartTime,
                request.EndTime,
                id);

            if (validationError is not null)
                return Result.Failure<AppointmentResponse>(validationError);
        }

        appointment.DoctorId = request.DoctorId;
        appointment.SeniorId = request.SeniorId;
        appointment.RoomId = request.RoomId;
        appointment.AppointmentType = request.AppointmentType.Trim();
        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        appointment.Status = request.Status;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment is null)
            return Result.Failure(AppointmentErrors.AppointmentNotFound);

        // ApplicationDbContext يحوّل الحذف إلى Soft Delete.
        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateStatusAsync(int id, string status)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment is null)
            return Result.Failure(AppointmentErrors.AppointmentNotFound);

        if (!Enum.TryParse<AppointmentStatus>(
                status, true, out var parsedStatus) ||
            !Enum.IsDefined(typeof(AppointmentStatus), parsedStatus))
        {
            return Result.Failure(AppointmentErrors.InvalidStatus);
        }

        appointment.Status = parsedStatus;
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<PTCenter.Domain.Errors.Error?> ValidateBookingAsync(
        string? doctorId,
        int? roomId,
        DateTime startTime,
        DateTime endTime,
        int? excludeAppointmentId = null)
    {
        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            var doctorExists = await _context.Users
                .AnyAsync(x => x.Id == doctorId);

            if (!doctorExists)
                return AppointmentErrors.DoctorNotFound;
        }

        if (roomId.HasValue)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(x => x.Id == roomId.Value);

            if (room is null)
                return AppointmentErrors.RoomNotFound;

            if (!room.IsActive)
                return AppointmentErrors.RoomInactive;

            var roomConflict = await _context.Appointments.AnyAsync(x =>
                x.RoomId == roomId.Value &&
                x.Id != excludeAppointmentId &&
                x.Status != AppointmentStatus.Cancelled &&
                x.Status != AppointmentStatus.Completed &&
                x.Status != AppointmentStatus.NoShow &&
                x.StartTime < endTime &&
                x.EndTime > startTime);

            if (roomConflict)
                return AppointmentErrors.RoomConflict;
        }

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            var doctorConflict = await _context.Appointments.AnyAsync(x =>
                x.DoctorId == doctorId &&
                x.Id != excludeAppointmentId &&
                x.Status != AppointmentStatus.Cancelled &&
                x.Status != AppointmentStatus.Completed &&
                x.Status != AppointmentStatus.NoShow &&
                x.StartTime < endTime &&
                x.EndTime > startTime);

            if (doctorConflict)
                return AppointmentErrors.DoctorConflict;
        }

        return null;
    }

    private static AppointmentResponse MapResponse(Appointment x)
    {
        return new AppointmentResponse
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
    }
}