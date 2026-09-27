using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Domain.Errors
{
    public static class PatientErrors
    {
        public static readonly Error DeleteFailed =
    new("Patient.DeleteFailed", "Failed to delete patient.", StatusCodes.Status400BadRequest);
        public static readonly Error PatientNotFound =
            new("Patient.NotFound", "Patient was not found.", StatusCodes.Status404NotFound);

        public static readonly Error UserNotFound =
            new("Patient.UserNotFound", "Application user was not found.", StatusCodes.Status404NotFound);

        public static readonly Error PatientAlreadyExists =
            new("Patient.AlreadyExists", "This user is already registered as a patient.", StatusCodes.Status409Conflict);

        public static readonly Error InsuranceCompanyNotFound =
            new("Patient.InsuranceCompanyNotFound", "Insurance company was not found.", StatusCodes.Status404NotFound);
        public static readonly Error FailedToAssignRole =
            new("Patient.FailedToAssignRole", "Failed to assign role to the user.", StatusCodes.Status500InternalServerError);
        public static readonly Error RegistrationFailed = new("Patient.RegistrationFailed", "Patient registration failed", StatusCodes.Status400BadRequest);
    }
}
