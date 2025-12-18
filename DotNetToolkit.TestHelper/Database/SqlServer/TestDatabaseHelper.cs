#region License

// Author:      Gary Wu
// Project:     DotNetToolkit Test Helpers
// Date:        December 1, 2025
// Description: Provides helper utilities for initializing and managing
//              SQL Server LocalDB databases for integration testing.

#endregion

using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DotNetToolkit.TestHelper.Database.SqlServer;

/// <summary>
/// Provides utility methods for creating, initializing, and managing SQL Server LocalDB databases for integration testing.
/// </summary>
public class TestDatabaseHelper
{
    private readonly string _databaseName;
    private readonly string _masterConnectionString;
    private readonly string _databaseConnectionString;

    /// <summary>
    /// Gets the name of the test database.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public string DatabaseName => _databaseName;

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _databaseConnectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDatabaseHelper"/> class.
    /// </summary>
    /// <param name="databaseName">The name of the test database to manage.</param>
    /// <param name="instanceName">The LocalDB instance name (default: "MSSQLLocalDB").</param>
    public TestDatabaseHelper(string databaseName, string instanceName = "MSSQLLocalDB")
    {
        _databaseName = databaseName;
        _masterConnectionString = $"Server=(localdb)\\{instanceName};Database=master;Integrated Security=true;TrustServerCertificate=true;";
        _databaseConnectionString = $"Server=(localdb)\\{instanceName};Database={databaseName};Integrated Security=true;TrustServerCertificate=true;";
    }

    /// <summary>
    /// Ensures that the test database exists, creating it if necessary.
    /// </summary>
    public async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        var checkDbCommand = connection.CreateCommand();
        checkDbCommand.CommandText = "SELECT database_id FROM sys.databases WHERE name = @DatabaseName";
        checkDbCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);

        var dbExists = await checkDbCommand.ExecuteScalarAsync(cancellationToken);

        if (dbExists == null)
        {
            var createDbCommand = connection.CreateCommand();
            createDbCommand.CommandText = $"CREATE DATABASE [{_databaseName}]";
            await createDbCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Initializes the database by executing a SQL script file.
    /// </summary>
    public async Task InitializeDatabaseAsync(string scriptPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.IsPathRooted(scriptPath)
            ? scriptPath
            : Path.Combine(AppContext.BaseDirectory, scriptPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Database initialization script not found at: {fullPath}", fullPath);
        }

        var script = await File.ReadAllTextAsync(fullPath, cancellationToken);
        await ExecuteScriptAsync(script, cancellationToken);
    }

    /// <summary>
    /// Executes a SQL script against the test database.
    /// </summary>
    public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        var batches = SplitSqlBatches(script);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120; // 2 minutes for complex scripts
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Resets the database to its initial state by re-running the initialization script.
    /// </summary>
    public async Task ResetDatabaseAsync(string scriptPath, CancellationToken cancellationToken = default)
    {
        await InitializeDatabaseAsync(scriptPath, cancellationToken);
    }

    /// <summary>
    /// Clears all data from the test database tables while preserving the schema.
    /// </summary>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC sp_MSforeachtable 'DELETE FROM ?'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Drops the test database if it exists.
    /// </summary>
    public async Task CleanupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        // Ensure no pooled connections keep the database locked.
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var setSingleUserCommand = connection.CreateCommand())
        {
            setSingleUserCommand.CommandText = @"
                IF EXISTS (SELECT database_id FROM sys.databases WHERE name = @DatabaseName)
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = @DatabaseName AND state_desc <> 'ONLINE')
                    BEGIN
                        DECLARE @onlineSql nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + N' SET ONLINE';
                        EXEC (@onlineSql);
                    END

                    DECLARE @singleUserSql nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE';
                    EXEC (@singleUserSql);
                END";
            setSingleUserCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);

            try
            {
                await setSingleUserCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            catch
            {
                // Ignore errors if database doesn't exist or is already offline.
            }
        }

        await using (var dropCommand = connection.CreateCommand())
        {
            dropCommand.CommandText = @"
                IF EXISTS (SELECT database_id FROM sys.databases WHERE name = @DatabaseName)
                BEGIN
                    DECLARE @dropSql nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@DatabaseName);
                    EXEC (@dropSql);
                END";
            dropCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);

            await dropCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Creates a new database connection to the test database.
    /// </summary>
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_databaseConnectionString);
    }

    /// <summary>
    /// Creates and opens a new database connection to the test database.
    /// </summary>
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string[] SplitSqlBatches(string script)
    {
        return Regex.Split(
            script,
            @"^\s*GO\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }
}