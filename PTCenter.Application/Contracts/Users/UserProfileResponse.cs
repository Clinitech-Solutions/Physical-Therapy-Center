namespace PTCenter.Application.Contracts.Users;

public class UserProfileResponse
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    
    public IList<string> Roles { get; set; } = [];
}