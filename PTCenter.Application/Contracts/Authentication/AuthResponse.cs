namespace PTCenter.Application.Contracts.Authentication;

public record AuthResponse(
    string Id,
    string UserName,
    string Token,
    string RefreshToken,
    int ExpiresIn,
    string? Message
);