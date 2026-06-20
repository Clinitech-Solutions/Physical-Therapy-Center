using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Authentication;
using PTCenter.Domain.Entities.Enums;
using PTCenter.Infrastructure.Persistence;
using System.Security.Cryptography;
using static System.Net.WebRequestMethods;

namespace PTCenter.Infrastructure.Services;

public class AuthService(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager,IJwtProvider jwtProvider,ApplicationDbContext context,IEmailSender emailSender,IConfiguration configuration) : IAuthService 
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager  = signInManager;
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    private readonly ApplicationDbContext _context =context;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly IConfiguration _configuration=configuration;

    public async Task<Result<AuthResponse>> GetTokenAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName)
        ?? await _userManager.FindByEmailAsync(request.UserName);
        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        // منع تسجيل الدخول إذا لم يتم تأكيد البريد الإلكتروني
        if (!user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.EmailNotConfirmed);

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, true);

        if (result.Succeeded)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var jwtResult = _jwtProvider.GenerateToken(user, userRoles);

            var response = new AuthResponse(
                user.Id,
                user.UserName!,
                jwtResult.token,
                "",
                jwtResult.expiresIn,
                "User logged in successfully."
            );

            return Result.Success(response);
        }

        var error = result.IsLockedOut
            ? UserErrors.LockedUser
            : UserErrors.InvalidCredentials;

        return Result.Failure<AuthResponse>(error);
    }
    public async Task<Result> RegisterAsync(RegisterRequest request)
    {
        if ((await _userManager.FindByNameAsync(request.UserName) ??
            await _userManager.FindByEmailAsync(request.Email)) is not null)
        {
            return Result.Failure(UserErrors.UserAlreadyExists);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                FullName = request.FullName,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
                return Result.Failure(UserErrors.RegistrationFailed);

            var roleResult = await _userManager.AddToRoleAsync(user, "Patient");

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return Result.Failure(UserErrors.FailedToAssignRole);
            }

            var patient = new Patient
            {
                ApplicationUserId = user.Id
            };

            _context.Patients.Add(patient);

            var otp = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();

            _context.OTPVerifies.Add(new OTPVerify
            {
                ApplicationUserId = user.Id,
                OtpCode = otp,
                CreatedAt = DateTime.UtcNow,
                IsUsed = false
            });

            await _context.SaveChangesAsync();

            var emailSent = await _emailSender.SendEmailAsync(
                user.Email!,
                "OTP Verification",
                $"""
            <h2>Welcome to PT Center</h2>

            <p>Your verification code is:</p>

            <h1>{otp}</h1>

            <p>This code will expire in 5 minutes.</p>
            """
            );

            if (!emailSent)
            {
                await transaction.RollbackAsync();
                await _userManager.DeleteAsync(user);

                return Result.Failure(UserErrors.EmailSendingFailed);
            }

            await transaction.CommitAsync();

            return Result.Success("User registered successfully. OTP has been sent to your email.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<Result<AuthResponse>> VerifyRegisterAsync(VerifyRegisterRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.EmailORUserName)
               ?? await _userManager.FindByEmailAsync(request.EmailORUserName);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        var otpRecord = await _context.OTPVerifies
            .Where(x => x.ApplicationUserId == user.Id && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpRecord is null)
            return Result.Failure<AuthResponse>(UserErrors.NoOtpFound);

        if ((DateTime.UtcNow - otpRecord.CreatedAt).TotalMinutes > 5)
            return Result.Failure<AuthResponse>(UserErrors.OtpExpired);

        if (otpRecord.OtpCode != request.OTP)
            return Result.Failure<AuthResponse>(UserErrors.OtpNotVerified);

        otpRecord.IsUsed = true;
        user.EmailConfirmed = true;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
            return Result.Failure<AuthResponse>(UserErrors.FailedToUpdateUser);

        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);

        var jwtResult = _jwtProvider.GenerateToken(user, roles);

        var response = new AuthResponse(
            user.Id,
            user.UserName!,
            jwtResult.token,
            string.Empty,
            jwtResult.expiresIn,
            "Email verified successfully."
        );

        return Result.Success(response);
    }
    public async Task<Result> ResendOtpAsync(ResendOtpRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.EmailORUserName)
               ?? await _userManager.FindByEmailAsync(request.EmailORUserName);

        if (user is null)
            return Result.Failure(UserErrors.InvalidCredentials);

        // لو الإيميل متفعل بالفعل
        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.EmailAlreadyConfirmed);

        // إلغاء صلاحية أي OTP قديم
        var oldOtps = await _context.OTPVerifies
            .Where(x => x.ApplicationUserId == user.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var otp in oldOtps)
            otp.IsUsed = true;

        // إنشاء OTP جديد
        var newOtp = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();

        _context.OTPVerifies.Add(new OTPVerify
        {
            ApplicationUserId = user.Id,
            OtpCode = newOtp,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        });

        await _context.SaveChangesAsync();

        var emailSent = await _emailSender.SendEmailAsync(
            user.Email!,
            "OTP Verification",
            $"""
        <h2>PT Center</h2>

        <p>Your new verification code is:</p>

        <h1>{newOtp}</h1>

        <p>This code will expire in 5 minutes.</p>
        """
        );

        if (!emailSent)
            return Result.Failure(UserErrors.EmailSendingFailed);

        return Result.Success("A new OTP has been sent to your email.");
    }
    public async Task<Result<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return Result.Failure<AuthResponse>(UserErrors.InvalidGoogleToken);

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[]
                    {
                    _configuration["GoogleAuthSettings:ClientId"]
                    }
                });
        }
        catch
        {
            return Result.Failure<AuthResponse>(UserErrors.InvalidGoogleToken);
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                FullName = payload.Name,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
                return Result.Failure<AuthResponse>(UserErrors.RegistrationFailed);

            var roleResult = await _userManager.AddToRoleAsync(user, "Patient");

            if (!roleResult.Succeeded)
                return Result.Failure<AuthResponse>(UserErrors.FailedToAssignRole);

            _context.Patients.Add(new Patient
            {
                ApplicationUserId = user.Id
            });

            await _context.SaveChangesAsync();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var jwt = _jwtProvider.GenerateToken(user, roles);

        var response = new AuthResponse(
            user.Id,
            user.UserName!,
            jwt.token,
            "",
            jwt.expiresIn,
            "Google login successful."
        );

        return Result.Success(response);
    }
}