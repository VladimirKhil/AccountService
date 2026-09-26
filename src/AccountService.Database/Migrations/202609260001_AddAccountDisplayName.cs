using FluentMigrator;

namespace AccountService.Database.Migrations;

[Migration(202609260001, "Add account display name")]
public sealed class AddAccountDisplayName : Migration
{
    public override void Up()
    {
        Alter.Table(DbConstants.Accounts)
            .AddColumn("display_name").AsString(128).Nullable();
    }

    public override void Down()
    {
        Delete.Column("display_name").FromTable(DbConstants.Accounts);
    }
}