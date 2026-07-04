using AccountService.Services;
using Microsoft.IdentityModel.Tokens;
using CustomTokenValidationResult = AccountService.Services.TokenValidationResult;

namespace AccountService.Contracts;

public interface ITokenService
{
    TokenIssueResult Issue(Guid userId, string username, string jwtId);

    CustomTokenValidationResult Validate(string token);

    SecurityKey GetPublicSigningKey();
}
