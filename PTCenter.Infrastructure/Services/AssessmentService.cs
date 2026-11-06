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
        CreateAssessmentRequest request, string actor, bool supervisor)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(_context);
        if (!LegacyClinicalRules.Owns(request.SeniorId, actor, supervisor))
            return Result.Failure<AssessmentResponse>(LegacyClinicalRules.Denied);
        var appointment = await _context.Appointments.Where(a=>a.PatientId==request.PatientId && a.SeniorId==request.SeniorId && a.AppointmentType=="Assessment" &&
            a.Status!=PTCenter.Domain.Entities.Enums.AppointmentStatus.Cancelled && a.Status!=PTCenter.Domain.Entities.Enums.AppointmentStatus.NoShow &&
            !_context.Assessments.Any(x=>x.AppointmentId==a.Id)).OrderByDescending(a=>a.StartTime).FirstOrDefaultAsync();
        if (!supervisor && appointment == null) return Result.Failure<AssessmentResponse>(LegacyClinicalRules.Denied);
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

        var seniorExists = await new LegacyClinicalRules(_context).EligibleAsync(request.SeniorId, request.PatientId, "Senior");

        if (!seniorExists)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.SeniorNotFound);
        }

        var assessment = new Assessment
        {
            AppointmentId = appointment?.Id,
            CreatedBy = actor,
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

        await transaction.CommitAsync();
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
        UpdateAssessmentRequest request, string actor, bool supervisor)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(_context);
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

        var denied = await new LegacyClinicalRules(_context).AssessmentAsync(assessment, actor, supervisor, true);
        if (denied != null) return Result.Failure<AssessmentResponse>(denied);
        if (!supervisor && request.SeniorId != assessment.SeniorId) return Result.Failure<AssessmentResponse>(LegacyClinicalRules.Denied);
        var seniorExists = await new LegacyClinicalRules(_context).EligibleAsync(request.SeniorId, assessment.PatientId, "Senior");

        if (!seniorExists)
        {
            return Result.Failure<AssessmentResponse>(
                AssessmentErrors.SeniorNotFound);
        }

        assessment.UpdatedBy = actor;
        assessment.SeniorId = request.SeniorId;
        assessment.Diagnosis = request.Diagnosis;
        assessment.ConditionType = request.ConditionType;
        assessment.Priority = request.Priority;
        assessment.Notes = request.Notes;
        assessment.AssessmentDate = request.AssessmentDate;
        assessment.Status = request.Status;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id, string actor, bool supervisor)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(_context);
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (assessment is null)
        {
            return Result.Failure(
                AssessmentErrors.AssessmentNotFound);
        }

        var denied = await new LegacyClinicalRules(_context).AssessmentAsync(assessment, actor, supervisor, true);
        if (denied != null) return Result.Failure(denied);
        if (await _context.TreatmentPlans.AnyAsync(p=>p.AssessmentId==id)) return Result.Failure(LegacyClinicalRules.Signed);
        assessment.DeletedBy = actor;
        try
        {
            _context.Assessments.Remove(assessment);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
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
