using AccountService.Contract.Models;
using LinqToDB.Mapping;

namespace AccountService.Database.Models;

[Table(Schema = DbConstants.Schema, Name = DbConstants.Accounts)]
public sealed class AccountRecord
{
    [PrimaryKey]
    [Column(Name = "id")]
    public Guid Id { get; set; }

    [NotNull]
    [Column(Name = "username")]
    public string Username { get; set; } = string.Empty;

    [Nullable]
    [Column(Name = "avatar")]
    public byte[]? Avatar { get; set; }

    [Column(Name = "gender")]
    public Gender Gender { get; set; }

    [NotNull]
    [Column(Name = "created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [NotNull]
    [Column(Name = "updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Nullable]
    [Column(Name = "deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }

    [Nullable]
    [Column(Name = "purge_after")]
    public DateTimeOffset? PurgeAfter { get; set; }
}
