namespace PTCenter.Infrastructure.Services;

public interface IPatientUsernameGenerator { string Next(); }

public sealed class PatientUsernameGenerator : IPatientUsernameGenerator
{
    // No name, phone or email-derived identifier; Identity's unique username index is authoritative.
    public string Next() => "patient." + Guid.NewGuid().ToString("N");
}
