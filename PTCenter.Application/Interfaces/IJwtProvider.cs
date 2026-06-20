using PTCenter.Domain.Entities;

namespace PTCenter.Application.Interfaces;

public interface IJwtProvider
{
    public (string token, int expiresIn) GenerateToken(ApplicationUser user, IEnumerable<string> roles);
}