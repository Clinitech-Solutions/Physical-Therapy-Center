using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.TreatmentPlans;


namespace PTCenter.Application.Interfaces;

public interface ITreatmentPlanService
{
    Task<Result<TreatmentPlanResponse>> AddAsync(
        CreateTreatmentPlanRequest request);

    Task<Result<TreatmentPlanResponse>> GetByIdAsync(
        int id);

    Task<Result<List<TreatmentPlanResponse>>> GetAllAsync();

    Task<Result<TreatmentPlanResponse>> UpdateAsync(
        int id,
        UpdateTreatmentPlanRequest request);

    Task<Result> DeleteAsync(
        int id);
}