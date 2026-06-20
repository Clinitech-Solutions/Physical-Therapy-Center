namespace PTCenter.Application.Contracts.Users;

public class CreateUserRequest
{
    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public IList<string> Roles { get; set; } = [];
}