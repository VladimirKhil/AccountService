using System.Security.Claims;

namespace AccountService.Services;

public sealed record TokenValidationResult(bool IsValid, ClaimsPrincipal? Principal = null, string? JwtId = null);
