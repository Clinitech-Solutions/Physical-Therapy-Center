using Microsoft.AspNetCore.Http;

namespace PTCenter.Domain.Errors;

public static class RoomErrors
{
    public static readonly Error RoomNotFound =
        new("Room.NotFound", "Room was not found.", StatusCodes.Status404NotFound);

    public static readonly Error InvalidName =
        new("Room.InvalidName", "Room name is required.", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidCapacity =
        new("Room.InvalidCapacity", "Room capacity must be greater than zero.", StatusCodes.Status400BadRequest);

    public static readonly Error RoomHasAppointments =
        new("Room.HasAppointments", "Room cannot be deleted while it has appointments.", StatusCodes.Status409Conflict);

    public static readonly Error RoomHasFutureAppointments =
        new("Room.HasFutureAppointments", "Maintenance cannot be enabled while the room has future appointments.", StatusCodes.Status409Conflict);
}