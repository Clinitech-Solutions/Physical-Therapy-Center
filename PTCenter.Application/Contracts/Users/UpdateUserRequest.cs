
using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Users;

public class UpdateUserRequest
{
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public Gender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public List<string> Roles { get; set; } = [];
}

