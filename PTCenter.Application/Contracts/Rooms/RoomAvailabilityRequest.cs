namespace PTCenter.Application.Contracts.Rooms;

public class RoomAvailabilityRequest
{
    public List<DateTimeOffset> StartsAt { get; set; } = [];
}
