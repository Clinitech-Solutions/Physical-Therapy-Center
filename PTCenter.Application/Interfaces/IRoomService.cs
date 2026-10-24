using PTCenter.Application.Abstractions;
using PTCenter.Application.Contracts.Rooms;

namespace PTCenter.Application.Interfaces;

public interface IRoomService
{
    Task<Result<List<RoomResponse>>> GetAllAsync();
    Task<Result<RoomResponse>> GetByIdAsync(int id);
    Task<Result<RoomResponse>> CreateAsync(CreateRoomRequest request);
    Task<Result<RoomResponse>> UpdateAsync(int id, UpdateRoomRequest request);
    Task<Result> UpdateMaintenanceAsync(int id, bool maintenance);
    Task<Result> DeleteAsync(int id);
}