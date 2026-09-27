using FluentMigrator;

namespace AccountService.Database.Migrations;

[Migration(202607010001, "Initial migration")]
public sealed class Initial : Migration
{
    public override void Up()
    {
        Create.Table(DbConstants.Accounts)
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("username").AsString(100).NotNullable()
            .WithColumn("avatar").AsBinary(int.MaxValue).Nullable()
            .WithColumn("gender").AsInt16().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable()
            .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
            .WithColumn("purge_after").AsDateTimeOffset().Nullable();

        Create.Table(DbConstants.ExternalAuthLinks)
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("account_id").AsGuid().NotNullable().ForeignKey(DbConstants.Accounts, "id")
            .WithColumn("provider").AsInt16().NotNullable()
            .WithColumn("provider_user_id").AsString(128).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.Table(DbConstants.AccountSessions)
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("account_id").AsGuid().NotNullable().ForeignKey(DbConstants.Accounts, "id")
            .WithColumn("jwt_id").AsString(128).NotNullable()
            .WithColumn("expires_at").AsDateTimeOffset().NotNullable()
            .WithColumn("revoked_at").AsDateTimeOffset().Nullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable();

        Create.Index("ix_external_auth_links_provider_user")
            .OnTable(DbConstants.ExternalAuthLinks)
            .OnColumn("provider").Ascending()
            .OnColumn("provider_user_id").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_external_auth_links_account")
            .OnTable(DbConstants.ExternalAuthLinks)
            .OnColumn("account_id").Ascending();

        Create.Index("ix_account_sessions_jwt")
            .OnTable(DbConstants.AccountSessions)
            .OnColumn("jwt_id").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_account_sessions_account")
            .OnTable(DbConstants.AccountSessions)
            .OnColumn("account_id").Ascending();

        Create.Index("ix_accounts_purge_after")
            .OnTable(DbConstants.Accounts)
            .OnColumn("purge_after").Ascending();

        Create.Index("ix_accounts_username")
            .OnTable(DbConstants.Accounts)
            .OnColumn("username").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table(DbConstants.AccountSessions);
        Delete.Table(DbConstants.ExternalAuthLinks);
        Delete.Table(DbConstants.Accounts);
    }
}
