using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.InsuranceCompany;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Application.Interfaces
{
    public interface IInsuranceCompanyService
    {
        Task<Result<InsuranceCompanyResponse>> AddAsync(CreateInsuranceCompanyRequest request);

        Task<Result<InsuranceCompanyResponse>> UpdateAsync(int id, UpdateInsuranceCompanyRequest request);

        Task<Result> DeleteAsync(int id);

        Task<Result<InsuranceCompanyResponse>> GetByIdAsync(int id);

        Task<Result<List<InsuranceCompanyResponse>>> GetAllAsync();
    }
}
