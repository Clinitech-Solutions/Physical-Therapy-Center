
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Users;

public class UserResponse
{
    public string Id { get; init; } = null!;

    public string UserName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public Gender Gender { get; set; }

    public DateTime BirthDate { get; set; }

    public IEnumerable<string> Roles { get; set; } = [];
    public List<StaffWorkingHours> WorkingHours { get; set; } = [];
}

