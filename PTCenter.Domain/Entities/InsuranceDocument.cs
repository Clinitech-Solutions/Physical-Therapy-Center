namespace PTCenter.Domain.Entities;

public class InsuranceDocument : BaseEntity
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
}
