using Microsoft.AspNetCore.Http;

namespace PTCenter.Domain.Errors;

public static class AppointmentErrors
{
    public static readonly Error AppointmentNotFound =
        new("Appointment.NotFound", "Appointment was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error RoomConflict =
        new("Appointment.RoomConflict",
            "The selected room is already booked for this time.",
            StatusCodes.Status409Conflict);

    public static readonly Error DoctorConflict =
        new("Appointment.DoctorConflict",
            "The selected doctor already has an appointment at this time.",
            StatusCodes.Status409Conflict);

    public static readonly Error PatientNotFound =
        new("Appointment.PatientNotFound", "Patient was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error DoctorNotFound =
        new("Appointment.DoctorNotFound", "Doctor was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error RoomNotFound =
        new("Appointment.RoomNotFound", "Room was not found.",
            StatusCodes.Status404NotFound);

    public static readonly Error RoomInactive =
        new("Appointment.RoomInactive",
            "The selected room is inactive or under maintenance.",
            StatusCodes.Status409Conflict);

    public static readonly Error InvalidTime =
        new("Appointment.InvalidTime",
            "End time must be later than start time.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidAppointmentType =
        new("Appointment.InvalidType",
            "Appointment type is required.",
            StatusCodes.Status400BadRequest);

    public static readonly Error InvalidStatus =
        new("Appointment.InvalidStatus",
            "Appointment status is invalid.",
            StatusCodes.Status400BadRequest);

    public static readonly Error DeleteFailed =
        new("Appointment.DeleteFailed", "Failed to delete appointment.",
            StatusCodes.Status400BadRequest);
}