using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.TreatmentPlans;


namespace PTCenter.Application.Interfaces;

public interface ITreatmentPlanService
{
    Task<Result<TreatmentPlanResponse>> AddAsync(
        CreateTreatmentPlanRequest request, string actor, bool supervisor);

    Task<Result<TreatmentPlanResponse>> GetByIdAsync(
        int id);

    Task<Result<List<TreatmentPlanResponse>>> GetAllAsync(int? patientId = null);

    Task<Result<TreatmentPlanResponse>> UpdateAsync(
        int id,
        UpdateTreatmentPlanRequest request, string actor, bool supervisor);

    Task<Result> DeleteAsync(
        int id, string actor, bool supervisor);
}
