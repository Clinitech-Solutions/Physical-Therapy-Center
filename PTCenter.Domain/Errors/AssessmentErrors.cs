using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Domain.Errors
{
   

    public static class AssessmentErrors
    {
        public static readonly Error AssessmentNotFound =
            new(
                "Assessment.NotFound",
                "Assessment was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error PatientNotFound =
            new(
                "Assessment.PatientNotFound",
                "Patient was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error SeniorNotFound =
            new(
                "Assessment.SeniorNotFound",
                "Senior was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error DeleteFailed =
            new(
                "Assessment.DeleteFailed",
                "Failed to delete assessment.",
                StatusCodes.Status400BadRequest);

        public static readonly Error DiagnosisRequired =
            new(
                "Assessment.DiagnosisRequired",
                "Diagnosis is required.",
                StatusCodes.Status400BadRequest);

        public static readonly Error ConditionTypeRequired =
            new(
                "Assessment.ConditionTypeRequired",
                "Condition type is required.",
                StatusCodes.Status400BadRequest);

        public static readonly Error PriorityRequired =
            new(
                "Assessment.PriorityRequired",
                "Priority is required.",
                StatusCodes.Status400BadRequest);
    }
}
