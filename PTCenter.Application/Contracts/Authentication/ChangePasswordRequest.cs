namespace PTCenter.Application.Contracts.Authentication;

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = null!;
}