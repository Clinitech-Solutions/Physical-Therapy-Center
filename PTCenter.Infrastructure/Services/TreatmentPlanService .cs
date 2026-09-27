using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.TreatmentPlans;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class TreatmentPlanService(
    ApplicationDbContext context)
    : ITreatmentPlanService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<TreatmentPlanResponse>> AddAsync(
        CreateTreatmentPlanRequest request)
    {
        // Check Assessment
        var assessmentExists = await _context.Assessments
            .AnyAsync(x => x.Id == request.AssessmentId);

        if (!assessmentExists)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.AssessmentNotFound);

        // One TreatmentPlan per Assessment
        var treatmentPlanExists = await _context.TreatmentPlans
            .AnyAsync(x => x.AssessmentId == request.AssessmentId);

        if (treatmentPlanExists)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.TreatmentPlanAlreadyExists);

        // Check Doctor
        var doctorExists = await _context.Users
            .AnyAsync(x => x.Id == request.DoctorId);

        if (!doctorExists)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.DoctorNotFound);

        // Validate Number Of Sessions
        if (request.NumberOfSessions <= 0)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.InvalidNumberOfSessions);

        // Validate Frequency
        if (request.FrequencyPerWeek <= 0)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.InvalidFrequency);

        var treatmentPlan = new TreatmentPlan
        {
            AssessmentId = request.AssessmentId,
            DoctorId = request.DoctorId,
            NumberOfSessions = request.NumberOfSessions,
            CompletedSessions = 0,
            FrequencyPerWeek = request.FrequencyPerWeek,
            TreatmentType = request.TreatmentType,
            Exercises = request.Exercises,
            Status = request.Status
        };

        await _context.TreatmentPlans.AddAsync(treatmentPlan);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(treatmentPlan.Id);
    }

    public async Task<Result<List<TreatmentPlanResponse>>> GetAllAsync()
    {
        var treatmentPlans = await _context.TreatmentPlans
            .Include(x => x.Doctor)
            .Include(x => x.Sessions)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var response = treatmentPlans
            .Select(MapToResponse)
            .ToList();

        return Result.Success(response);
    }

    public async Task<Result<TreatmentPlanResponse>> GetByIdAsync(int id)
    {
        var treatmentPlan = await _context.TreatmentPlans
            .Include(x => x.Doctor)
            .Include(x => x.Sessions)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (treatmentPlan is null)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.TreatmentPlanNotFound);

        return Result.Success(MapToResponse(treatmentPlan));
    }

    public async Task<Result<TreatmentPlanResponse>> UpdateAsync(
        int id,
        UpdateTreatmentPlanRequest request)
    {
        var treatmentPlan = await _context.TreatmentPlans
            .Include(x => x.Sessions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (treatmentPlan is null)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.TreatmentPlanNotFound);

        // Check Doctor
        var doctorExists = await _context.Users
            .AnyAsync(x => x.Id == request.DoctorId);

        if (!doctorExists)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.DoctorNotFound);

        // Validate Number Of Sessions
        if (request.NumberOfSessions <= 0)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.InvalidNumberOfSessions);

        // Validate Frequency
        if (request.FrequencyPerWeek <= 0)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.InvalidFrequency);

        // Calculate completed sessions from actual sessions
        var completedSessions = treatmentPlan.Sessions
            .Count(x => x.Status == SessionStatus.Completed);

        if (request.NumberOfSessions < completedSessions)
            return Result.Failure<TreatmentPlanResponse>(
                TreatmentPlanErrors.SessionsAlreadyCompleted);

        treatmentPlan.DoctorId = request.DoctorId;
        treatmentPlan.NumberOfSessions = request.NumberOfSessions;
        treatmentPlan.FrequencyPerWeek = request.FrequencyPerWeek;
        treatmentPlan.TreatmentType = request.TreatmentType;
        treatmentPlan.Exercises = request.Exercises;
        treatmentPlan.Status = request.Status;

        // Keep it synchronized with the actual sessions
        treatmentPlan.CompletedSessions = completedSessions;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var treatmentPlan = await _context.TreatmentPlans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (treatmentPlan is null)
            return Result.Failure(
                TreatmentPlanErrors.TreatmentPlanNotFound);

        try
        {
            _context.TreatmentPlans.Remove(treatmentPlan);

            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch
        {
            return Result.Failure(
                TreatmentPlanErrors.DeleteFailed);
        }
    }

    private static TreatmentPlanResponse MapToResponse(
        TreatmentPlan treatmentPlan)
    {
        var completedSessions = treatmentPlan.Sessions
            .Count(x => x.Status == SessionStatus.Completed);

        var progressPercentage =
            treatmentPlan.NumberOfSessions <= 0
                ? 0
                : Math.Round(
                    (decimal)completedSessions
                    / treatmentPlan.NumberOfSessions
                    * 100,
                    2);

        return new TreatmentPlanResponse
        {
            Id = treatmentPlan.Id,
            AssessmentId = treatmentPlan.AssessmentId,

            DoctorId = treatmentPlan.DoctorId,
            DoctorName = treatmentPlan.Doctor?.FullName ?? string.Empty,

            NumberOfSessions = treatmentPlan.NumberOfSessions,
            CompletedSessions = completedSessions,
            ProgressPercentage = progressPercentage,

            FrequencyPerWeek = treatmentPlan.FrequencyPerWeek,

            TreatmentType = treatmentPlan.TreatmentType,
            Exercises = treatmentPlan.Exercises,

            Status = treatmentPlan.Status
        };
    }
}