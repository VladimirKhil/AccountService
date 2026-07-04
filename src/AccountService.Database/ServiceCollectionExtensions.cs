using AccountService.Database.Migrations;
using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider.PostgreSQL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace AccountService.Database;

public static class ServiceCollectionExtensions
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(NpgsqlProviderAdapter.NpgsqlConnection))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(NpgsqlDataReader))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Initial))]
    public static void AddAccountDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "AccountService")
    {
        var dbConnectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException("Database connection is undefined");

        services.AddLinqToDBContext<AccountDbConnection>((provider, options) =>
            options
                .UsePostgreSQL(dbConnectionString)
                .UseRetryPolicy(new TransientRetryPolicy())
                .UseDefaultLogging(provider));
    }
}
