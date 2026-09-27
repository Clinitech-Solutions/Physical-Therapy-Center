using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Assessments;

namespace PTCenter.Application.Interfaces;

public interface IAssessmentService
{
    Task<Result<AssessmentResponse>> AddAsync(
        CreateAssessmentRequest request);

    Task<Result<AssessmentResponse>> UpdateAsync(
        int id,
        UpdateAssessmentRequest request);

    Task<Result> DeleteAsync(int id);

    Task<Result<AssessmentResponse>> GetByIdAsync(int id);

    Task<Result<List<AssessmentResponse>>> GetAllAsync(
        int? patientId = null);
}