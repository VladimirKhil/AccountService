using AccountService.Contract.Models;
using LinqToDB.Mapping;

namespace AccountService.Database.Models;

[Table(Schema = DbConstants.Schema, Name = DbConstants.ExternalAuthLinks)]
public sealed class ExternalAuthLinkRecord
{
    [PrimaryKey]
    [Column(Name = "id")]
    public Guid Id { get; set; }

    [NotNull]
    [Column(Name = "account_id")]
    public Guid AccountId { get; set; }

    [Column(Name = "provider")]
    public AuthProvider Provider { get; set; }

    [NotNull]
    [Column(Name = "provider_user_id")]
    public string ProviderUserId { get; set; } = string.Empty;

    [NotNull]
    [Column(Name = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
