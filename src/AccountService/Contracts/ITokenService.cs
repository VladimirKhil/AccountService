using AccountService.Contract.Models;
using AccountService.Services;
using Microsoft.IdentityModel.Tokens;
using CustomTokenValidationResult = AccountService.Services.TokenValidationResult;

namespace AccountService.Contracts;

public interface ITokenService
{
    TokenIssueResult Issue(Guid userId, string username, string displayName, AuthProvider authProvider, string jwtId);

    CustomTokenValidationResult Validate(string token);

    SecurityKey GetPublicSigningKey();
}
