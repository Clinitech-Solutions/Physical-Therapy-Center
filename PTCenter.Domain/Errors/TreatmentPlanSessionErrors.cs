using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Domain.Errors
{
   
    public static class TreatmentPlanSessionErrors
    {
        public static readonly Error SessionNotFound =
            new(
                "TreatmentPlanSession.NotFound",
                "Treatment plan session was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error TreatmentPlanNotFound =
            new(
                "TreatmentPlanSession.TreatmentPlanNotFound",
                "Treatment plan was not found.",
                StatusCodes.Status404NotFound);

        public static readonly Error SessionAlreadyExists =
            new(
                "TreatmentPlanSession.AlreadyExists",
                "This session number already exists for this treatment plan.",
                StatusCodes.Status409Conflict);

        public static readonly Error InvalidSessionNumber =
            new(
                "TreatmentPlanSession.InvalidSessionNumber",
                "Session number must be greater than zero.",
                StatusCodes.Status400BadRequest);

        public static readonly Error SessionNumberOutOfRange =
            new(
                "TreatmentPlanSession.SessionNumberOutOfRange",
                "Session number cannot be greater than the treatment plan number of sessions.",
                StatusCodes.Status400BadRequest);

        public static readonly Error InvalidCheckInOut =
            new(
                "TreatmentPlanSession.InvalidCheckInOut",
                "Check-out cannot be earlier than check-in.",
                StatusCodes.Status400BadRequest);

        public static readonly Error DeleteFailed =
            new(
                "TreatmentPlanSession.DeleteFailed",
                "Failed to delete treatment plan session.",
                StatusCodes.Status400BadRequest);
    }
}
