using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Common;
using PTCenter.Application.Contracts.InsuranceDocument;
using PTCenter.Application.Interfaces;
using PTCenter.Domain.Entities;
using PTCenter.Domain.Errors;
using PTCenter.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text.Json;

namespace PTCenter.Infrastructure.Services;

public class InsuranceDocumentService(
    ApplicationDbContext context,
    IWebHostEnvironment environment)
    : IInsuranceDocumentService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;
    public const long MaximumFileBytes = 10 * 1024 * 1024;

    private static async Task<string?> ValidatedContentType(IFormFile file)
    {
        if (file.Length is <= 0 or > MaximumFileBytes) return null;
        var extension = Path.GetExtension(Path.GetFileName(file.FileName.Replace('\\', '/'))).ToLowerInvariant();
        await using var input = file.OpenReadStream();
        var signature = new byte[8];
        var count = 0;
        while (count < signature.Length)
        {
            var read = await input.ReadAsync(signature.AsMemory(count));
            if (read == 0) break;
            count += read;
        }
        if (extension == ".pdf" && count >= 5 && signature.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) return "application/pdf";
        if (extension is ".jpg" or ".jpeg" && count >= 3 && signature[0] == 0xff && signature[1] == 0xd8 && signature[2] == 0xff) return "image/jpeg";
        if (extension == ".png" && count == 8 && signature.AsSpan().SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return "image/png";
        return null;
    }

    private static readonly Error InvalidUpload = new("InsuranceDocument.InvalidFile",
        "Choose a non-empty PDF, JPEG or PNG file up to 10 MiB (10,485,760 bytes). File content must match its extension.", 400);

    public async Task<Result<List<InsuranceDocumentResponse>>> AddRangeAsync(CreateInsuranceDocumentRequest request)
    {
        if(request.IdempotencyKey!=null && (string.IsNullOrWhiteSpace(request.IdempotencyKey)||request.IdempotencyKey.Length>100))
            return Result.Failure<List<InsuranceDocumentResponse>>(new("InsuranceDocument.OperationKey","Invalid upload operation key.",400));
        var patientExists = await _context.Patients
            .AnyAsync(x => x.Id == request.PatientId);

        if (!patientExists)
            return Result.Failure<List<InsuranceDocumentResponse>>(InsuranceDocumentErrors.PatientNotFound);

        if (request.Files == null || request.Files.Count == 0)
            return Result.Failure<List<InsuranceDocumentResponse>>(InsuranceDocumentErrors.DocumentNotFound);

        var contentTypes = new List<string>();
        var fileHashes=new List<string>();
        foreach (var file in request.Files)
        {
            var contentType = await ValidatedContentType(file);
            if (contentType == null) return Result.Failure<List<InsuranceDocumentResponse>>(InvalidUpload);
            contentTypes.Add(contentType);
            if(request.IdempotencyKey!=null){await using var stream=file.OpenReadStream();fileHashes.Add(Convert.ToHexString(await SHA256.HashDataAsync(stream)));}
        }

        var fingerprint=request.IdempotencyKey==null?null:Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{request.PatientId,Files=request.Files.Select((file,index)=>new{Name=Path.GetFileName(file.FileName.Replace('\\','/')),file.Length,ContentType=contentTypes[index],Hash=fileHashes[index]})})));
        await using var transaction=await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await ClinicMutationLock.AcquireAsync(_context);
        if(request.IdempotencyKey!=null){
            var previous=await _context.InsuranceDocuments.IgnoreQueryFilters().Include(d=>d.Patient).ThenInclude(p=>p.ApplicationUser)
                .Where(d=>d.UploadOperationKey==request.IdempotencyKey).OrderBy(d=>d.UploadIndex).ToListAsync();
            if(previous.Count>0){
                if(previous.Count!=request.Files.Count || previous.Any(d=>d.IsDeleted || d.PatientId!=request.PatientId || d.UploadBatchHash!=fingerprint))
                    return Result.Failure<List<InsuranceDocumentResponse>>(new("InsuranceDocument.RetryConflict","This upload operation belongs to different or removed documents. Review the original upload.",409));
                await transaction.CommitAsync();
                return Result.Success(previous.Select(d=>new InsuranceDocumentResponse{Id=d.Id,PatientId=d.PatientId,PatientName=d.Patient.ApplicationUser.FullName,FileName=d.FileName,FilePath=d.FilePath,ContentType=d.ContentType,UploadedAt=d.UploadedAt}).ToList());
            }
        }

        var savedDocuments = new List<InsuranceDocument>();

        try
        {
        for (var index = 0; index < request.Files.Count; index++)
        {
            var file = request.Files[index];
            var filePath = await SaveFileAsync(file);

            var document = new InsuranceDocument
            {
                PatientId = request.PatientId,
                FileName = Path.GetFileName(file.FileName.Replace('\\', '/')),
                FilePath = filePath,
                ContentType = contentTypes[index],
                UploadedAt = DateTime.UtcNow
                ,UploadOperationKey=request.IdempotencyKey,UploadBatchHash=fingerprint,UploadIndex=request.IdempotencyKey==null?null:index
            };

            savedDocuments.Add(document);
        }

        await _context.InsuranceDocuments.AddRangeAsync(savedDocuments);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DbUpdateException)
        {
            await transaction.RollbackAsync();
            foreach (var document in savedDocuments) DeleteFile(document.FilePath);
            return Result.Failure<List<InsuranceDocumentResponse>>(InsuranceDocumentErrors.UploadFailed);
        }

        var documentIds = savedDocuments.Select(x => x.Id).ToList();

        var documents = await _context.InsuranceDocuments
            .Include(x => x.Patient)
            .ThenInclude(x => x.ApplicationUser)
            .Where(x => documentIds.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync();

        var response = documents.Select(x => new InsuranceDocumentResponse
        {
            Id = x.Id,
            PatientId = x.PatientId,
            PatientName = x.Patient.ApplicationUser.FullName,
            FileName = x.FileName,
            FilePath = x.FilePath,
            ContentType = x.ContentType,
            UploadedAt = x.UploadedAt
        }).ToList();

        return Result.Success(response);
    }

    public async Task<Result<List<InsuranceDocumentResponse>>> GetAllAsync(int? patientId = null)
    {
        var documents = await _context.InsuranceDocuments
            .Include(x => x.Patient)
            .ThenInclude(x => x.ApplicationUser)
            .Where(x => patientId == null || x.PatientId == patientId)
            .AsNoTracking()
            .ToListAsync();

        var response = documents.Select(x => new InsuranceDocumentResponse
        {
            Id = x.Id,
            PatientId = x.PatientId,
            PatientName = x.Patient.ApplicationUser.FullName,
            FileName = x.FileName,
            FilePath = x.FilePath,
            ContentType = x.ContentType,
            UploadedAt = x.UploadedAt
        }).ToList();

        return Result.Success(response);
    }

    public async Task<Result<InsuranceDocumentResponse>> GetByIdAsync(int id)
    {
        var document = await _context.InsuranceDocuments
            .Include(x => x.Patient)
            .ThenInclude(x => x.ApplicationUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (document is null)
            return Result.Failure<InsuranceDocumentResponse>(InsuranceDocumentErrors.DocumentNotFound);

        var response = new InsuranceDocumentResponse
        {
            Id = document.Id,
            PatientId = document.PatientId,
            PatientName = document.Patient.ApplicationUser.FullName,
            FileName = document.FileName,
            FilePath = document.FilePath,
            ContentType = document.ContentType,
            UploadedAt = document.UploadedAt
        };

        return Result.Success(response);
    }

    public async Task<Result<InsuranceDocumentResponse>> UpdateAsync(int id, UpdateInsuranceDocumentRequest request)
    {
        var document = await _context.InsuranceDocuments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (document is null)
            return Result.Failure<InsuranceDocumentResponse>(InsuranceDocumentErrors.DocumentNotFound);

        if (request.File is not null)
        {
            var contentType = await ValidatedContentType(request.File);
            if (contentType == null) return Result.Failure<InsuranceDocumentResponse>(InvalidUpload);
            var oldPath = document.FilePath;
            string? newPath = null;
            try
            {
                newPath = await SaveFileAsync(request.File);
                document.FileName = Path.GetFileName(request.File.FileName.Replace('\\', '/'));
                document.FilePath = newPath;
                document.ContentType = contentType;
                await _context.SaveChangesAsync();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or DbUpdateException)
            {
                if (newPath != null) DeleteFile(newPath);
                return Result.Failure<InsuranceDocumentResponse>(InsuranceDocumentErrors.UploadFailed);
            }
            DeleteFile(oldPath);
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var document = await _context.InsuranceDocuments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (document is null)
            return Result.Failure(InsuranceDocumentErrors.DocumentNotFound);

        DeleteFile(document.FilePath);

        _context.InsuranceDocuments.Remove(document);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var folder = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "insurance-documents");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(Path.GetFileName(file.FileName.Replace('\\', '/')))}";
        var fullPath = Path.Combine(folder, fileName);

        try
        {
            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await file.CopyToAsync(stream);
        }
        catch
        {
            // This path is a new server-generated file, never an existing patient document.
            if (File.Exists(fullPath)) File.Delete(fullPath);
            throw;
        }

        return $"/uploads/insurance-documents/{fileName}";
    }

    private void DeleteFile(string filePath)
    {
        var root = Path.GetFullPath(Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "insurance-documents")) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            filePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())));

        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
