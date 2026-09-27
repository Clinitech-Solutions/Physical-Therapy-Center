using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.Patients;

namespace PTCenter.Application.Interfaces;

public interface IPatientService
{
    Task<Result<PatientResponse>> AddAsync(
        CreatePatientRequest request);

    Task<Result<PatientResponse>> UpdateAsync(
        int id,
        UpdatePatientRequest request);

    Task<Result> DeleteAsync(int id);

    Task<Result<PatientResponse>> GetByIdAsync(int id);

    Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(
        PatientQueryRequest request);
}