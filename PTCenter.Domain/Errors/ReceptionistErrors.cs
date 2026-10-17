using Microsoft.AspNetCore.Http;

namespace PTCenter.Domain.Errors;

public static class ReceptionistErrors
{
    public static readonly Error AppointmentNotFound =
        new(
            "Appointment.NotFound",
            "Appointment was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error RoomConflict =
        new(
            "Appointment.RoomConflict",
            "The selected room is already booked for this time.",
            StatusCodes.Status409Conflict);

    public static readonly Error DoctorConflict =
        new(
            "Appointment.DoctorConflict",
            "The selected doctor already has an appointment at this time.",
            StatusCodes.Status409Conflict);

    public static readonly Error PatientNotFound =
        new(
            "Appointment.PatientNotFound",
            "Patient was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error InvalidStatusTransition =
        new(
            "Appointment.InvalidStatusTransition",
            "Invalid status transition for this appointment.",
            StatusCodes.Status400BadRequest);
}