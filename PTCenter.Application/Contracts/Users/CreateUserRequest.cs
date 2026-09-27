using PTCenter.Domain.Entities.Enums;

namespace PTCenter.Application.Contracts.Users;

public class CreateUserRequest
{
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public Gender Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public List<string> Roles { get; set; } = [];
}