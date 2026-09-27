using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PTCenter.Application.Abstractions;
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


    public async Task<Result<InsuranceDocumentResponse>> AddAsync(
        CreateInsuranceDocumentRequest request)
    {
        var patientExists = await _context.Patients
            .AnyAsync(x => x.Id == request.PatientId);

        if (!patientExists)
            return Result.Failure<InsuranceDocumentResponse>(
                InsuranceDocumentErrors.PatientNotFound);


        var filePath = await SaveFileAsync(request.File);


        var document = new InsuranceDocument
        {
            PatientId = request.PatientId,
            CoveragePercent = request.CoveragePercent,
            FileName = request.File.FileName,
            FilePath = filePath,
            ContentType = request.File.ContentType,
            UploadedAt = DateTime.UtcNow
        };


        await _context.InsuranceDocuments.AddAsync(document);

        await _context.SaveChangesAsync();


        return await GetByIdAsync(document.Id);
    }



    public async Task<Result<List<InsuranceDocumentResponse>>> GetAllAsync()
    {
        var documents = await _context.InsuranceDocuments
            .Include(x => x.Patient)
            .ThenInclude(x => x.ApplicationUser)
            .AsNoTracking()
            .ToListAsync();


        var response = documents.Select(x => new InsuranceDocumentResponse
        {
            Id = x.Id,
            PatientId = x.PatientId,
            PatientName = x.Patient.ApplicationUser.FullName,
            CoveragePercent = x.CoveragePercent,
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
            return Result.Failure<InsuranceDocumentResponse>(
                InsuranceDocumentErrors.DocumentNotFound);



        var response = new InsuranceDocumentResponse
        {
            Id = document.Id,
            PatientId = document.PatientId,
            PatientName = document.Patient.ApplicationUser.FullName,
            CoveragePercent = document.CoveragePercent,
            FileName = document.FileName,
            FilePath = document.FilePath,
            ContentType = document.ContentType,
            UploadedAt = document.UploadedAt
        };


        return Result.Success(response);
    }



    public async Task<Result<InsuranceDocumentResponse>> UpdateAsync(
        int id,
        UpdateInsuranceDocumentRequest request)
    {
        var document = await _context.InsuranceDocuments
            .FirstOrDefaultAsync(x => x.Id == id);


        if (document is null)
            return Result.Failure<InsuranceDocumentResponse>(
                InsuranceDocumentErrors.DocumentNotFound);



        document.CoveragePercent = request.CoveragePercent;


        if (request.File is not null)
        {
            DeleteFile(document.FilePath);


            var filePath = await SaveFileAsync(request.File);


            document.FileName = request.File.FileName;
            document.FilePath = filePath;
            document.ContentType = request.File.ContentType;
        }


        await _context.SaveChangesAsync();


        return await GetByIdAsync(id);
    }



    public async Task<Result> DeleteAsync(int id)
    {
        var document = await _context.InsuranceDocuments
            .FirstOrDefaultAsync(x => x.Id == id);


        if (document is null)
            return Result.Failure(
                InsuranceDocumentErrors.DocumentNotFound);



        DeleteFile(document.FilePath);


        _context.InsuranceDocuments.Remove(document);


        await _context.SaveChangesAsync();


        return Result.Success();
    }




    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var folder =
            Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "insurance-documents");


        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);


        var fileName =
            $"{Guid.NewGuid()}_{file.FileName}";


        var fullPath =
            Path.Combine(folder, fileName);



        await using var stream =
            new FileStream(fullPath, FileMode.Create);


        await file.CopyToAsync(stream);



        return $"/uploads/insurance-documents/{fileName}";
    }



    private void DeleteFile(string filePath)
    {
        var fullPath =
            Path.Combine(
                _environment.WebRootPath,
                filePath.TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString()));


        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}