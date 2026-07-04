using LinqToDB.Mapping;

namespace AccountService.Database.Models;

[Table(Schema = DbConstants.Schema, Name = DbConstants.AccountSessions)]
public sealed class AccountSessionRecord
{
    [PrimaryKey]
    [Column(Name = "id")]
    public Guid Id { get; set; }

    [NotNull]
    [Column(Name = "account_id")]
    public Guid AccountId { get; set; }

    [NotNull]
    [Column(Name = "jwt_id")]
    public string JwtId { get; set; } = string.Empty;

    [NotNull]
    [Column(Name = "expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Nullable]
    [Column(Name = "revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [NotNull]
    [Column(Name = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
