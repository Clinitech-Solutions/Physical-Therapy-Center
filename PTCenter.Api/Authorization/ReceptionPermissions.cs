namespace PTCenter.Api.Authorization;

/// <summary>Reception work grants no staff-account or clinical-authoring privileges.</summary>
public static class ReceptionPermissions
{
    public const string StaffRead = "Receptionist,Senior,Doctor,Ceo,Admin,Owner";
    public const string Operations = "Receptionist,Ceo,Admin,Owner";
    public const string Administration = "Ceo,Admin,Owner";
    public const string ClinicalWrite = "Senior,Doctor,Ceo,Admin,Owner";
}
