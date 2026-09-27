using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.TreatmentPlans;
using PTCenter.Application.Contracts.TreatmentPlanSession;

namespace PTCenter.Application.Interfaces;

public interface ITreatmentPlanSessionService
{
    Task<Result<TreatmentPlanSessionResponse>> AddAsync(
        CreateTreatmentPlanSessionRequest request);

    Task<Result<TreatmentPlanSessionResponse>> UpdateAsync(
        int id,
        UpdateTreatmentPlanSessionRequest request);

    Task<Result> DeleteAsync(int id);

    Task<Result<TreatmentPlanSessionResponse>> GetByIdAsync(int id);

    Task<Result<List<TreatmentPlanSessionResponse>>> GetAllAsync(
        int treatmentPlanId);
}