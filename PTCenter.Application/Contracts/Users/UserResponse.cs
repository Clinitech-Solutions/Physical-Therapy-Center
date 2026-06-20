namespace PTCenter.Application.Contracts.Users;

public class UserResponse
{
    public string Id { get; init; } = null!;

    public string UserName { get; set; } = null!;

    public IEnumerable<string> Roles { get; set; } = [];
}