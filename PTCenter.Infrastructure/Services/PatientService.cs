
using System.Security.Cryptography;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    ILogger<PatientService> logger)
    : IPatientService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<PatientService> _logger = logger;

    // ========================= ADD PATIENT =========================

    public async Task<Result<PatientResponse>> AddAsync(
        CreatePatientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return Result.Failure<PatientResponse>(
                PatientErrors.RegistrationFailed);

        var userName = request.UserName.Trim();

        var existingUser = await _userManager.FindByNameAsync(userName);

        if (existingUser is not null)
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientAlreadyExists);

        if (request.InsuranceCompanyId.HasValue)
        {
            var companyExists = await _context.InsuranceCompanies
                .AnyAsync(x => x.Id == request.InsuranceCompanyId.Value);

            if (!companyExists)
                return Result.Failure<PatientResponse>(
                    PatientErrors.InsuranceCompanyNotFound);
        }

        // CreatePatientRequest does not contain an email in this version,
        // so the service generates a placeholder email.
        var generatedEmail = $"{userName}@example.com";

        var existingEmail = await _userManager.FindByEmailAsync(generatedEmail);

        if (existingEmail is not null)
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientAlreadyExists);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        ApplicationUser? user = null;

        try
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = generatedEmail,
                PhoneNumber = request.PhoneNumber?.Trim(),
                FullName = request.FullName.Trim(),
                Gender = request.Gender,
                BirthDate = request.BirthDate,

                EmailConfirmed = true
            };

            var password = GeneratePassword();
            var createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                LogIdentityErrors(
                    "Create patient user",
                    userName,
                    createResult.Errors);

                await transaction.RollbackAsync();

                return Result.Failure<PatientResponse>(
                    PatientErrors.RegistrationFailed);
            }

            // Assign the Patient role.
            // The Patient role must already exist in the database.
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                "Patient");

            if (!roleResult.Succeeded)
            {
                LogIdentityErrors(
                    "Assign Patient role",
                    userName,
                    roleResult.Errors);

                await transaction.RollbackAsync();

                return Result.Failure<PatientResponse>(
                    PatientErrors.FailedToAssignRole);
            }

            var patient = request.Adapt<Patient>();

            patient.ApplicationUserId = user.Id;

            // Uses InsuranceDetailsResponse as requested.
            if (request.InsuranceDetails is not null)
            {
                patient.InsuranceDetails =
                    request.InsuranceDetails.Adapt<InsuranceDetails>();
            }

            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = await GetPatientResponseAsync(patient.Id);

            if (response is null)
            {
                _logger.LogError(
                    "Patient {PatientId} was created but could not be loaded.",
                    patient.Id);

                return Result.Failure<PatientResponse>(
                    PatientErrors.RegistrationFailed);
            }

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Unexpected error while registering patient {UserName}.",
                userName);

            throw;
        }
    }

    // ========================= GET ALL PATIENTS =========================

    public async Task<Result<PagedResponse<PatientResponse>>> GetAllAsync(
        PatientQueryRequest request)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _context.Patients
            .Include(x => x.ApplicationUser)
            .Include(x => x.InsuranceCompany)
            .Include(x => x.InsuranceDetails)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.ApplicationUser.FullName.Contains(search) ||
                (x.ApplicationUser.PhoneNumber != null &&
                 x.ApplicationUser.PhoneNumber.Contains(search)) ||
                (x.ApplicationUser.UserName != null &&
                 x.ApplicationUser.UserName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentType))
        {
            query = query.Where(
                x => x.PaymentType == request.PaymentType);
        }

        if (request.InsuranceCompanyId.HasValue)
        {
            query = query.Where(
                x => x.InsuranceCompanyId ==
                     request.InsuranceCompanyId.Value);
        }

        var totalCount = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        var patients = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var items = patients
            .Select(patient => MapToResponse(patient))
            .ToList();

        var response = new PagedResponse<PatientResponse>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
        return Result.Success(response);
    }

    // ========================= GET PATIENT BY ID =========================

    public async Task<Result<PatientResponse>> GetByIdAsync(int id)
    {
        var patient = await _context.Patients
            .Include(x => x.ApplicationUser)
            .Include(x => x.InsuranceCompany)
            .Include(x => x.InsuranceDetails)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient is null)
        {
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientNotFound);
        }

        return Result.Success(MapToResponse(patient));
    }

    // ========================= UPDATE PATIENT =========================

    public async Task<Result<PatientResponse>> UpdateAsync(
        int id,
        UpdatePatientRequest request)
    {
        var patient = await _context.Patients
            .Include(x => x.InsuranceDetails)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient is null)
        {
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientNotFound);
        }

        if (request.InsuranceCompanyId.HasValue)
        {
            var companyExists = await _context.InsuranceCompanies
                .AnyAsync(
                    x => x.Id == request.InsuranceCompanyId.Value);

            if (!companyExists)
            {
                return Result.Failure<PatientResponse>(
                    PatientErrors.InsuranceCompanyNotFound);
            }
        }

        var user = await _userManager.FindByIdAsync(
            patient.ApplicationUserId);

        if (user is null)
        {
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientNotFound);
        }

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        var existingUserName =
            await _userManager.FindByNameAsync(userName);

        if (existingUserName is not null &&
            existingUserName.Id != user.Id)
        {
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientAlreadyExists);
        }

        var existingEmail =
            await _userManager.FindByEmailAsync(email);

        if (existingEmail is not null &&
            existingEmail.Id != user.Id)
        {
            return Result.Failure<PatientResponse>(
                PatientErrors.PatientAlreadyExists);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Update patient-specific fields.
            patient.PaymentType = request.PaymentType;
            patient.EmergencyContact = request.EmergencyContact;
            patient.InsuranceCompanyId = request.InsuranceCompanyId;

            // Uses InsuranceDetailsResponse as requested.
            if (request.InsuranceDetails is not null)
            {
                patient.InsuranceDetails ??= new InsuranceDetails();

                patient.InsuranceDetails.Status =
                    request.InsuranceDetails.Status;

                patient.InsuranceDetails.CopayPercentage =
                    request.InsuranceDetails.CopayPercentage;

                patient.InsuranceDetails.ApprovedSessions =
                    request.InsuranceDetails.ApprovedSessions;

                patient.InsuranceDetails.MemberId =
                    request.InsuranceDetails.MemberId;

                patient.InsuranceDetails.Employer =
                    request.InsuranceDetails.Employer;
            }
            else
            {
                patient.InsuranceDetails = null;
            }

            // Update the linked Identity user.
            user.UserName = userName;
            user.Email = email;
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.FullName = request.FullName.Trim();
            user.BirthDate = request.BirthDate;
            user.Gender = request.Gender;

            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                LogIdentityErrors(
                    "Update patient user",
                    userName,
                    identityResult.Errors);

                await transaction.RollbackAsync();

                return Result.Failure<PatientResponse>(
                    PatientErrors.RegistrationFailed);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updatedPatient = await GetByIdAsync(id);

            return updatedPatient;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Unexpected error while updating patient {PatientId}.",
                id);

            throw;
        }
    }

    // ========================= DELETE PATIENT =========================

    public async Task<Result> DeleteAsync(int id)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(x => x.Id == id);

        if (patient is null)
        {
            return Result.Failure(
                PatientErrors.PatientNotFound);
        }

        var user = await _userManager.FindByIdAsync(
            patient.ApplicationUserId);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            if (user is not null)
            {
                var identityResult =
                    await _userManager.DeleteAsync(user);

                if (!identityResult.Succeeded)
                {
                    LogIdentityErrors(
                        "Delete patient user",
                        user.UserName ?? user.Id,
                        identityResult.Errors);

                    await transaction.RollbackAsync();

                    return Result.Failure(
                        PatientErrors.DeleteFailed);
                }
            }

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Unexpected error while deleting patient {PatientId}.",
                id);

            throw;
        }
    }

    // ========================= MAPPING =========================

    private async Task<PatientResponse?> GetPatientResponseAsync(int id)
    {
        var patient = await _context.Patients
            .Include(x => x.ApplicationUser)
            .Include(x => x.InsuranceCompany)
            .Include(x => x.InsuranceDetails)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        return patient is null ? null : MapToResponse(patient);
    }

    private static PatientResponse MapToResponse(Patient patient)
    {
        var user = patient.ApplicationUser;

        return new PatientResponse
        {
            Id = patient.Id,
            ApplicationUserId = patient.ApplicationUserId,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Gender = user.Gender,
            BirthDate = user.BirthDate,
            PaymentType = patient.PaymentType,
            EmergencyContact = patient.EmergencyContact,
            InsuranceCompanyId = patient.InsuranceCompanyId,
            InsuranceCompanyName = patient.InsuranceCompany?.Name,

            // Map the entity to InsuranceDetailsResponse.
            InsuranceDetails = patient.InsuranceDetails is null
                ? null
                : patient.InsuranceDetails
                    .Adapt<InsuranceDetailsResponse>(),

            CreatedAt = patient.CreatedAt
        };
    }

    // ========================= IDENTITY LOGGING =========================

    private void LogIdentityErrors(
        string operation,
        string userName,
        IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
        {
            _logger.LogWarning(
                "Identity operation {Operation} failed for {UserName}. " +
                "Code: {Code}. Description: {Description}",
                operation,
                userName,
                error.Code,
                error.Description);
        }
    }

    // ========================= PASSWORD GENERATION =========================

    private static string GeneratePassword(int length = 12)
    {
        if (length < 4)
            throw new ArgumentOutOfRangeException(
                nameof(length),
                "Password length must be at least 4.");

        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%&*";

        var allCharacters = upper + lower + digits + special;

        var password = new List<char>
        {
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            special[RandomNumberGenerator.GetInt32(special.Length)]
        };

        while (password.Count < length)
        {
            password.Add(
                allCharacters[
                    RandomNumberGenerator.GetInt32(allCharacters.Length)]);
        }

        // Cryptographically secure shuffle.
        for (var i = password.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password.ToArray());
    }
}
