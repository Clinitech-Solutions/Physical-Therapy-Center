using Microsoft.AspNetCore.Http;

namespace PTCenter.Domain.Errors;

public static class TreatmentPlanErrors
{
    public static readonly Error TreatmentPlanNotFound =
        new(
            "TreatmentPlan.NotFound",
            "Treatment plan was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error AssessmentNotFound =
        new(
            "TreatmentPlan.AssessmentNotFound",
            "Assessment was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error TreatmentPlanAlreadyExists =
        new(
            "TreatmentPlan.AlreadyExists",
            "This assessment already has a treatment plan.",
            StatusCodes.Status409Conflict);

    public static readonly Error DoctorNotFound =
        new(
            "TreatmentPlan.DoctorNotFound",
            "Doctor was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error DeleteFailed =
        new(
            "TreatmentPlan.DeleteFailed",
            "Failed to delete treatment plan.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidNumberOfSessions =
        new(
            "TreatmentPlan.InvalidNumberOfSessions",
            "Number of sessions must be greater than zero.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidFrequency =
        new(
            "TreatmentPlan.InvalidFrequency",
            "Frequency per week must be greater than zero.",
            StatusCodes.Status400BadRequest);

    public static readonly Error SessionsAlreadyCompleted =
        new(
            "TreatmentPlan.SessionsAlreadyCompleted",
            "Number of sessions cannot be less than completed sessions.",
            StatusCodes.Status400BadRequest);
}