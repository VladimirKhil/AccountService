using AccountService.Database.Models;
using LinqToDB;
using LinqToDB.Data;

namespace AccountService.Database;

public sealed class AccountDbConnection : DataConnection
{
    public AccountDbConnection(DataOptions options)
        : base(options)
    {
    }

    public ITable<AccountRecord> Accounts => this.GetTable<AccountRecord>();

    public ITable<ExternalAuthLinkRecord> ExternalAuthLinks => this.GetTable<ExternalAuthLinkRecord>();

    public ITable<AccountSessionRecord> AccountSessions => this.GetTable<AccountSessionRecord>();
}
