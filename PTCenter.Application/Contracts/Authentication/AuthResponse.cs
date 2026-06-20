namespace PTCenter.Application.Contracts.Authentication;

public record AuthResponse(
    string Id,
    string UserName,
    string Token,
    int ExpiresIn
);