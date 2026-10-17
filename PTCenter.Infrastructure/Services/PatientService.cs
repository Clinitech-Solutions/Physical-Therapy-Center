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
        if ((await _userManager.FindByNameAsync(request.UserName) ??
             await _userManager.FindByEmailAsync(request.Email)) is not null)
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
                Email = request.Email,
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

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Mediqova - Account Password",
                    $"""
                    <!DOCTYPE html>
                    <html lang="en">
                    <head>
                        <meta charset="UTF-8">
                        <title>Mediqova</title>
                    </head>
                    <body style="margin:0;padding:0;background:#f4f7fb;font-family:'Segoe UI',Arial,sans-serif;">
                        <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f7fb;padding:40px 0;">
                            <tr>
                                <td align="center">
                                    <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:18px;overflow:hidden;box-shadow:0 10px 30px rgba(0,0,0,.08);">
                                        <tr>
                                            <td style="background:linear-gradient(135deg,#0F4C81,#1E88E5);padding:40px;text-align:center;">
                                                <div style="width:72px;height:72px;margin:auto;background:#fff;border-radius:50%;line-height:72px;font-size:34px;">
                                                    🏥
                                                </div>
                                                <h1 style="margin:20px 0 8px;color:white;">
                                                    Mediqova
                                                </h1>
                                                <p style="margin:0;color:#dbeafe;">
                                                    Powered by Clinitech Solutions
                                                </p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="padding:45px;">
                                                <h2>Welcome to Mediqova 👋</h2>
                                                <p style="font-size:16px;color:#4b5563;">
                                                    Your account has been created successfully.
                                                </p>
                                                <p style="font-size:16px;color:#4b5563;">
                                                    Your temporary password is:
                                                </p>
                                                <div style="margin:30px auto;width:280px;background:#eff6ff;border:2px dashed #1E88E5;border-radius:12px;padding:20px;text-align:center;">
                                                    <div style="font-size:34px;font-weight:bold;letter-spacing:5px;color:#0F4C81;">
                                                        {password}
                                                    </div>
                                                </div>
                                                <p style="color:#dc2626;font-weight:bold;text-align:center;">
                                                    Please change your password after your first login.
                                                </p>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style="background:#f9fafb;padding:25px;text-align:center;">
                                                <strong>Clinitech Solutions</strong>
                                                <br />
                                                <small>© 2026 Mediqova. All rights reserved.</small>
                                            </td>
                                        </tr>
                                    </table>
                                </td>
                            </tr>
                        </table>
                    </body>
                    </html>
                    """);
            }
            catch
            {
                // Log email error if needed
            }

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
        if (request.PageNumber < 1)
            request.PageNumber = 1;

        if (request.PageSize < 1)
            request.PageSize = 10;

        if (request.PageSize > 100)
            request.PageSize = 100;

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
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var response = new PagedResponse<PatientResponse>
        {
            Items = patients,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Result.Success(response);
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