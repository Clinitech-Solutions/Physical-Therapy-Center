using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.Patients;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;

namespace PTCenter.Infrastructure.Services;

public class PatientService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender)
    : IPatientService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IEmailSender _emailSender = emailSender;

    public async Task<Result<PatientResponse>> AddAsync(CreatePatientRequest request)
    {
        if ((await _userManager.FindByNameAsync(request.UserName)) is not null)
        {
            return Result.Failure<PatientResponse>(PatientErrors.PatientAlreadyExists);
        }

        if (request.InsuranceCompanyId.HasValue)
        {
            var companyExists = await _context.InsuranceCompanies
                .AnyAsync(x => x.Id == request.InsuranceCompanyId);

            if (!companyExists)
                return Result.Failure<PatientResponse>(PatientErrors.InsuranceCompanyNotFound);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        ApplicationUser? user = null;

        try
        {
            user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.UserName+"@example.com",
                PhoneNumber = request.PhoneNumber,
                FullName = request.FullName,
                Gender = request.Gender,
                BirthDate = request.BirthDate,
                EmailConfirmed = true
            };

            var password = GeneratePassword();
            var createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return Result.Failure<PatientResponse>(PatientErrors.RegistrationFailed);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Patient");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                await transaction.RollbackAsync();
                return Result.Failure<PatientResponse>(PatientErrors.FailedToAssignRole);
            }

            var patient = request.Adapt<Patient>();
            patient.ApplicationUserId = user.Id;

            // Mapping InsuranceDetails explicitly if needed or via Mapster
            if (request.InsuranceDetails is not null)
            {
                patient.InsuranceDetails = request.InsuranceDetails.Adapt<InsuranceDetails>();
            }

            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = patient.Adapt<PatientResponse>();
            response.FullName = user.FullName;
            response.Email = user.Email!;
            response.PhoneNumber = user.PhoneNumber!;
            response.BirthDate = user.BirthDate;

            if (patient.InsuranceCompanyId.HasValue)
            {
                response.InsuranceCompanyName = await _context.InsuranceCompanies
                    .Where(x => x.Id == patient.InsuranceCompanyId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();
            }

            // Send Email code goes here (kept as is in your project)...

            return Result.Success(response);
        }
        catch
        {
            await transaction.RollbackAsync();
            if (user != null)
                await _userManager.DeleteAsync(user);
            throw;
        }
    }

    public async Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(PatientQueryRequest request)
    {
        if (request.PageNumber < 1) request.PageNumber = 1;
        if (request.PageSize < 1) request.PageSize = 10;
        if (request.PageSize > 100) request.PageSize = 100;

        var query = _context.Patients
            .Include(x => x.ApplicationUser)
            .Include(x => x.InsuranceCompany)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.ApplicationUser.FullName.Contains(search) ||
                x.ApplicationUser.PhoneNumber!.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentType))
        {
            query = query.Where(x => x.PaymentType == request.PaymentType);
        }

        if (request.InsuranceCompanyId.HasValue)
        {
            query = query.Where(x => x.InsuranceCompanyId == request.InsuranceCompanyId.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var patients = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PatientResponse
            {
                Id = x.Id,
                ApplicationUserId = x.ApplicationUserId,
                FullName = x.ApplicationUser.FullName,
                Email = x.ApplicationUser.Email!,
                PhoneNumber = x.ApplicationUser.PhoneNumber!,
                Gender = x.ApplicationUser.Gender,
                BirthDate = x.ApplicationUser.BirthDate,
                PaymentType = x.PaymentType,
                EmergencyContact = x.EmergencyContact,
                InsuranceCompanyId = x.InsuranceCompanyId,
                InsuranceCompanyName = x.InsuranceCompany != null ? x.InsuranceCompany.Name : null,
                InsuranceDetails = x.InsuranceDetails.Adapt<InsuranceDetailsResponse>(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Result.Success(new PagedResponse<PatientResponse>
        {
            Items = patients,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        });
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(int id)
    {
        var patient = await _context.Patients
            .Include(x => x.ApplicationUser)
            .Include(x => x.InsuranceCompany)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient is null)
            return Result.Failure<PatientResponse>(PatientErrors.PatientNotFound);

        var response = new PatientResponse
        {
            Id = patient.Id,
            ApplicationUserId = patient.ApplicationUserId,
            FullName = patient.ApplicationUser.FullName,
            Email = patient.ApplicationUser.Email!,
            PhoneNumber = patient.ApplicationUser.PhoneNumber!,
            Gender = patient.ApplicationUser.Gender,
            BirthDate = patient.ApplicationUser.BirthDate,
            PaymentType = patient.PaymentType,
            EmergencyContact = patient.EmergencyContact,
            InsuranceCompanyId = patient.InsuranceCompanyId,
            InsuranceCompanyName = patient.InsuranceCompany?.Name,
            InsuranceDetails = patient.InsuranceDetails.Adapt<InsuranceDetailsResponse>(),
            CreatedAt = patient.CreatedAt
        };

        return Result.Success(response);
    }

    public async Task<Result<PatientResponse>> UpdateAsync(int id, UpdatePatientRequest request)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient is null)
            return Result.Failure<PatientResponse>(PatientErrors.PatientNotFound);

        if (request.InsuranceCompanyId.HasValue)
        {
            var companyExists = await _context.InsuranceCompanies
                .AnyAsync(x => x.Id == request.InsuranceCompanyId);

            if (!companyExists)
                return Result.Failure<PatientResponse>(PatientErrors.InsuranceCompanyNotFound);
        }

        var user = await _userManager.FindByIdAsync(patient.ApplicationUserId);
        if (user is null)
            return Result.Failure<PatientResponse>(PatientErrors.PatientNotFound);

        var userNameExists = await _userManager.FindByNameAsync(request.UserName);
        if (userNameExists is not null && userNameExists.Id != user.Id)
            return Result.Failure<PatientResponse>(PatientErrors.PatientAlreadyExists);

        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists is not null && emailExists.Id != user.Id)
            return Result.Failure<PatientResponse>(PatientErrors.PatientAlreadyExists);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            patient.PaymentType = request.PaymentType;
            patient.EmergencyContact = request.EmergencyContact;
            patient.InsuranceCompanyId = request.InsuranceCompanyId;

            // Update Insurance Details
            if (request.InsuranceDetails is not null)
            {
                patient.InsuranceDetails ??= new InsuranceDetails();
                patient.InsuranceDetails.Status = request.InsuranceDetails.Status;
                patient.InsuranceDetails.CopayPercentage = request.InsuranceDetails.CopayPercentage;
                patient.InsuranceDetails.ApprovedSessions = request.InsuranceDetails.ApprovedSessions;
                patient.InsuranceDetails.MemberId = request.InsuranceDetails.MemberId;
                patient.InsuranceDetails.Employer = request.InsuranceDetails.Employer;
            }
            else
            {
                patient.InsuranceDetails = null;
            }

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.FullName = request.FullName;
            user.BirthDate = request.BirthDate;
            user.Gender = request.Gender;

            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return Result.Failure<PatientResponse>(PatientErrors.RegistrationFailed);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient is null)
            return Result.Failure(PatientErrors.PatientNotFound);

        var user = await _userManager.FindByIdAsync(patient.ApplicationUserId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            if (user is not null)
            {
                var identityResult = await _userManager.DeleteAsync(user);
                if (!identityResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure(PatientErrors.DeleteFailed);
                }
            }

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private string GeneratePassword(int length = 10)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%";

        var random = new Random();
        var password = new List<char>
        {
            upper[random.Next(upper.Length)],
            lower[random.Next(lower.Length)],
            digits[random.Next(digits.Length)],
            special[random.Next(special.Length)]
        };

        string allChars = upper + lower + digits + special;
        for (int i = password.Count; i < length; i++)
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
}