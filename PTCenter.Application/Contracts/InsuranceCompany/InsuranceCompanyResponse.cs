using PTCenter.Application.Contracts.Patients;


namespace PTCenter.Application.Contracts.InsuranceCompany
{
    public class InsuranceCompanyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public string? PortalUrl { get; set; }
        public string? Hotline { get; set; } // أضف هذا الحقل
        public bool IsActive { get; set; } = true;

        public ICollection<PatientResponse> Patients { get; set; } = new List<PatientResponse>();
    }
}
