namespace PTCenter.Application.Contracts.Rooms;

public class UpdateRoomRequest
{
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
}