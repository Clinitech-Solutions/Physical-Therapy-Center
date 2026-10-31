using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.InsuranceDocument;

namespace PTCenter.Application.Interfaces;

public interface IInsuranceDocumentService
{
    Task<Result<List<InsuranceDocumentResponse>>> AddRangeAsync(CreateInsuranceDocumentRequest request);
    Task<Result<List<InsuranceDocumentResponse>>> GetAllAsync(int? patientId = null);
    Task<Result<InsuranceDocumentResponse>> GetByIdAsync(int id);
    Task<Result<InsuranceDocumentResponse>> UpdateAsync(int id, UpdateInsuranceDocumentRequest request);
    Task<Result> DeleteAsync(int id);
}
