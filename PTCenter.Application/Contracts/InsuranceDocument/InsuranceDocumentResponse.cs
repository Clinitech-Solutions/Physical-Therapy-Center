namespace PTCenter.Application.Contracts.InsuranceDocument;

public class InsuranceDocumentResponse
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public string PatientName { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public DateTime UploadedAt { get; set; }
}