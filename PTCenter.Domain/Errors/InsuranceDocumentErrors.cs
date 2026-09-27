using Microsoft.AspNetCore.Http;

namespace PTCenter.Domain.Errors;

public static class InsuranceDocumentErrors
{
    public static readonly Error DocumentNotFound =
        new(
            "InsuranceDocument.NotFound",
            "Insurance document was not found.", StatusCodes.Status404NotFound);


    public static readonly Error PatientNotFound =
        new(
            "InsuranceDocument.PatientNotFound",
            "Patient was not found.", StatusCodes.Status404NotFound);


    public static readonly Error UploadFailed =
        new(
            "InsuranceDocument.UploadFailed",
            "Failed to upload insurance document.", StatusCodes.Status400BadRequest);


    public static readonly Error InvalidFile =
        new(
            "InsuranceDocument.InvalidFile",
            "Invalid file format.", StatusCodes.Status400BadRequest);
}