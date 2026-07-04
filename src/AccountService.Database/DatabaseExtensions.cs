using Npgsql;

namespace AccountService.Database;

public static class DatabaseExtensions
{
    public static void EnsureExists(string connectionString, string? schema = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database;

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("Database name is not specified in connection string");
        }

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{database.Replace("'", "''")}'";
            var exists = command.ExecuteScalar() != null;

            if (!exists)
            {
                command.CommandText = $"CREATE DATABASE \"{database.Replace("\"", "\"\"")}\"";
                command.ExecuteNonQuery();
            }
        }

        if (!string.IsNullOrWhiteSpace(schema))
        {
            using var schemaConnection = new NpgsqlConnection(connectionString);
            schemaConnection.Open();
            using var schemaCommand = schemaConnection.CreateCommand();
            schemaCommand.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schema.Replace("\"", "\"\"")}\"";
            schemaCommand.ExecuteNonQuery();
        }
    }
}
