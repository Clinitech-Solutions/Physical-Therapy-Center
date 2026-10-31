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

namespace PTCenter.Infrastructure.Services;

public class InsuranceDocumentService(
    ApplicationDbContext context,
    IWebHostEnvironment environment)
    : IInsuranceDocumentService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<Result<List<InsuranceDocumentResponse>>> AddRangeAsync(CreateInsuranceDocumentRequest request)
    {
        var patientExists = await _context.Patients
            .AnyAsync(x => x.Id == request.PatientId);

        if (!patientExists)
            return Result.Failure<List<InsuranceDocumentResponse>>(InsuranceDocumentErrors.PatientNotFound);

        if (request.Files == null || request.Files.Count == 0)
            return Result.Failure<List<InsuranceDocumentResponse>>(InsuranceDocumentErrors.DocumentNotFound);

        var savedDocuments = new List<InsuranceDocument>();

        foreach (var file in request.Files)
        {
            var filePath = await SaveFileAsync(file);

            var document = new InsuranceDocument
            {
                PatientId = request.PatientId,
                FileName = Path.GetFileName(file.FileName.Replace('\\', '/')),
                FilePath = filePath,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            savedDocuments.Add(document);
        }

        await _context.InsuranceDocuments.AddRangeAsync(savedDocuments);
        await _context.SaveChangesAsync();

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
            DeleteFile(document.FilePath);

            var filePath = await SaveFileAsync(request.File);

            document.FileName = Path.GetFileName(request.File.FileName.Replace('\\', '/'));
            document.FilePath = filePath;
            document.ContentType = string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType;
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

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

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
