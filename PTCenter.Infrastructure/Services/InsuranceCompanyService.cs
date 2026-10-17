using Mapster;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.InsuranceCompany;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class InsuranceCompanyService(ApplicationDbContext context) : IInsuranceCompanyService
{
    private readonly ApplicationDbContext _context = context;


    public async Task<Result<InsuranceCompanyResponse>> AddAsync(CreateInsuranceCompanyRequest request)
    {
        var companyExists = await _context.InsuranceCompanies
            .AnyAsync(x => x.Name == request.Name);

        if (companyExists)
            return Result.Failure<InsuranceCompanyResponse>(
                InsuranceCompanyErrors.InsuranceCompanyAlreadyExists);


        var company = request.Adapt<InsuranceCompany>();

        await _context.InsuranceCompanies.AddAsync(company);

        await _context.SaveChangesAsync();


        var response = company.Adapt<InsuranceCompanyResponse>();

        return Result.Success(response);
    }


    public async Task<Result<List<InsuranceCompanyResponse>>> GetAllAsync()
    {
        var companies = await _context.InsuranceCompanies
            .Include(x => x.Patients)
            .AsNoTracking()
            .ToListAsync();


        var response = companies.Adapt<List<InsuranceCompanyResponse>>();

        return Result.Success(response);
    }


    public async Task<Result<InsuranceCompanyResponse>> GetByIdAsync(int id)
    {
        var company = await _context.InsuranceCompanies
            .Include(x => x.Patients)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);


        if (company is null)
            return Result.Failure<InsuranceCompanyResponse>(
                InsuranceCompanyErrors.InsuranceCompanyNotFound);


        var response = company.Adapt<InsuranceCompanyResponse>();

        return Result.Success(response);
    }


    public async Task<Result<InsuranceCompanyResponse>> UpdateAsync(
        int id,
        UpdateInsuranceCompanyRequest request)
    {
        var company = await _context.InsuranceCompanies
            .FirstOrDefaultAsync(x => x.Id == id);


        if (company is null)
            return Result.Failure<InsuranceCompanyResponse>(
                InsuranceCompanyErrors.InsuranceCompanyNotFound);


        var nameExists = await _context.InsuranceCompanies
            .AnyAsync(x => x.Name == request.Name && x.Id != id);


        if (nameExists)
            return Result.Failure<InsuranceCompanyResponse>(
                InsuranceCompanyErrors.InsuranceCompanyAlreadyExists);



        company.Name = request.Name;
        company.PortalUrl = request.PortalUrl;
        company.IsActive = request.IsActive;
        company.Hotline = request.Hotline;
        await _context.SaveChangesAsync();


        return await GetByIdAsync(id);
    }


    public async Task<Result> DeleteAsync(int id)
    {
        var company = await _context.InsuranceCompanies
            .FirstOrDefaultAsync(x => x.Id == id);


        if (company is null)
            return Result.Failure(
                InsuranceCompanyErrors.InsuranceCompanyNotFound);


        _context.InsuranceCompanies.Remove(company);


        await _context.SaveChangesAsync();


        return Result.Success();
    }
}