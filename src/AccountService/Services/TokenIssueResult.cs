namespace AccountService.Services;

public sealed record TokenIssueResult(string Token, DateTimeOffset ExpiresAt);
