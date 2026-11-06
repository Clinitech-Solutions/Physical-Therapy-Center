using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Assessments;

namespace PTCenter.Application.Interfaces;

public interface IAssessmentService
{
    Task<Result<AssessmentResponse>> AddAsync(
        CreateAssessmentRequest request, string actor, bool supervisor);

    Task<Result<AssessmentResponse>> UpdateAsync(
        int id,
        UpdateAssessmentRequest request, string actor, bool supervisor);

    Task<Result> DeleteAsync(int id, string actor, bool supervisor);

    Task<Result<AssessmentResponse>> GetByIdAsync(int id);

    Task<Result<List<AssessmentResponse>>> GetAllAsync(
        int? patientId = null);
}