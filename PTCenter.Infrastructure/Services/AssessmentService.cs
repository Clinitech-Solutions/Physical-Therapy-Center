using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Assessments;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class AssessmentService(
    ApplicationDbContext context)
    : IAssessmentService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Result<AssessmentResponse>> AddAsync(
        CreateAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.DiagnosisRequired);
        }

        if (string.IsNullOrWhiteSpace(request.ConditionType))
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.ConditionTypeRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Priority))
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.PriorityRequired);
        }

        var patientExists = await _context.Patients
            .AnyAsync(x => x.Id == request.PatientId);

        if (!patientExists)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.PatientNotFound);
        }

        var seniorExists = await _context.Users
            .AnyAsync(x => x.Id == request.SeniorId);

        if (!seniorExists)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.SeniorNotFound);
        }

        var assessment = new Assessment
        {
            PatientId = request.PatientId,
            SeniorId = request.SeniorId,
            Diagnosis = request.Diagnosis,
            ConditionType = request.ConditionType,
            Priority = request.Priority,
            Notes = request.Notes,
            AssessmentDate = request.AssessmentDate,
            Status = request.Status
        };

        await _context.Assessments.AddAsync(assessment);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(assessment.Id);
    }

    public async Task<Result<List<AssessmentResponse>>> GetAllAsync(
        int? patientId = null)
    {
        var query = _context.Assessments
            .AsNoTracking()
            .Include(x => x.Patient)
                .ThenInclude(x => x.ApplicationUser)
            .Include(x => x.Senior)
            .Include(x => x.TreatmentPlan)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(x =>
                x.PatientId == patientId.Value);
        }

        var assessments = await query
            .OrderByDescending(x => x.AssessmentDate)
            .ToListAsync();

        var response = assessments
            .Select(MapToResponse)
            .ToList();

        return Result.Success(response);
    }

    public async Task<Result<AssessmentResponse>> GetByIdAsync(
        int id)
    {
        var assessment = await _context.Assessments
            .AsNoTracking()
            .Include(x => x.Patient)
                .ThenInclude(x => x.ApplicationUser)
            .Include(x => x.Senior)
            .Include(x => x.TreatmentPlan)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (assessment is null)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.AssessmentNotFound);
        }

        return Result.Success(
            MapToResponse(assessment));
    }

    public async Task<Result<AssessmentResponse>> UpdateAsync(
        int id,
        UpdateAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Diagnosis))
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.DiagnosisRequired);
        }

        if (string.IsNullOrWhiteSpace(request.ConditionType))
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.ConditionTypeRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Priority))
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.PriorityRequired);
        }

        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (assessment is null)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.AssessmentNotFound);
        }

        var seniorExists = await _context.Users
            .AnyAsync(x => x.Id == request.SeniorId);

        if (!seniorExists)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.SeniorNotFound);
        }

        assessment.SeniorId = request.SeniorId;
        assessment.Diagnosis = request.Diagnosis;
        assessment.ConditionType = request.ConditionType;
        assessment.Priority = request.Priority;
        assessment.Notes = request.Notes;
        assessment.AssessmentDate = request.AssessmentDate;
        assessment.Status = request.Status;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (assessment is null)
        {
            return Result.Failure(
                AssessmentErrors.AssessmentNotFound);
        }

        try
        {
            _context.Assessments.Remove(assessment);

            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch
        {
            return Result.Failure(
                AssessmentErrors.DeleteFailed);
        }
    }

    private static AssessmentResponse MapToResponse(
        Assessment assessment)
    {
        return new AssessmentResponse
        {
            Id = assessment.Id,
            CompletedAt = assessment.CompletedAt,

            PatientId = assessment.PatientId,

            PatientName =
                assessment.Patient?.ApplicationUser?.FullName
                ?? string.Empty,

            SeniorId = assessment.SeniorId,

            SeniorName =
                assessment.Senior?.FullName
                ?? string.Empty,

            Diagnosis = assessment.Diagnosis,

            ConditionType = assessment.ConditionType,

            Priority = assessment.Priority,

            Notes = assessment.Notes,

            AssessmentDate = assessment.AssessmentDate,

            Status = assessment.Status,

            HasTreatmentPlan =
                assessment.TreatmentPlan is not null,

            TreatmentPlanId =
                assessment.TreatmentPlan?.Id
        };
    }
}
