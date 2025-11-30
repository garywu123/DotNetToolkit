#region License

// Author:      Gary Wu
// Project:     DotNetToolkit Integration Tests
// Date:        December 1, 2025
// Description: Provides helper utilities for initializing and managing
//              SQL Server LocalDB databases for integration testing.

#endregion

using System.Data;
using Microsoft.Data.SqlClient;

namespace DotNetToolkit.Tests.Database.Helpers;

/// <summary>
/// Provides utility methods for creating, initializing, and managing SQL Server LocalDB databases for integration testing.
/// </summary>
/// <remarks>
/// This helper class automatically manages LocalDB instances and database creation, hiding file placement details.
/// <code><![CDATA[
/// // Example usage in a test:
/// var helper = new TestDatabaseHelper("DotNetToolkitTest");
/// await helper.EnsureDatabaseExistsAsync();
/// await helper.InitializeDatabaseAsync("Scripts/InitializeTestDatabase.sql");
/// 
/// // ... run tests ...
/// 
/// await helper.CleanupDatabaseAsync();
/// ]]></code>
/// </remarks>
public class TestDatabaseHelper
{
    private readonly string _databaseName;
    private readonly string _masterConnectionString;
    private readonly string _databaseConnectionString;

    /// <summary>
    /// Gets the name of the test database.
    /// </summary>
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
    /// <remarks>
    /// <code><![CDATA[
    /// var helper = new TestDatabaseHelper("MyTestDb");
    /// // or specify custom instance
    /// var helper2 = new TestDatabaseHelper("MyTestDb", "MyLocalDBInstance");
    /// ]]></code>
    /// </remarks>
    public TestDatabaseHelper(string databaseName, string instanceName = "MSSQLLocalDB")
    {
        _databaseName = databaseName;
        _masterConnectionString = $"Server=(localdb)\\{instanceName};Database=master;Integrated Security=true;TrustServerCertificate=true;";
        _databaseConnectionString = $"Server=(localdb)\\{instanceName};Database={databaseName};Integrated Security=true;TrustServerCertificate=true;";
    }

    /// <summary>
    /// Ensures that the test database exists, creating it if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is idempotent - it can be called multiple times safely.
    /// <code><![CDATA[
    /// await helper.EnsureDatabaseExistsAsync();
    /// ]]></code>
    /// </remarks>
    public async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        var checkDbCommand = connection.CreateCommand();
        checkDbCommand.CommandText = $"SELECT database_id FROM sys.databases WHERE name = @DatabaseName";
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
    /// <param name="scriptPath">The relative or absolute path to the SQL script file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The script path is resolved relative to the test assembly location if not absolute.
    /// <code><![CDATA[
    /// await helper.InitializeDatabaseAsync("Scripts/InitializeTestDatabase.sql");
    /// ]]></code>
    /// </remarks>
    /// <exception cref="FileNotFoundException">Thrown if the script file does not exist.</exception>
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
    /// <param name="script">The SQL script text to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Splits the script by GO statements and executes each batch separately.
    /// <code><![CDATA[
    /// await helper.ExecuteScriptAsync("DELETE FROM Users; DELETE FROM Orders;");
    /// ]]></code>
    /// </remarks>
    public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        var batches = SplitSqlBatches(script);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120; // 2 minutes for complex scripts
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Resets the database to its initial state by re-running the initialization script.
    /// </summary>
    /// <param name="scriptPath">The path to the initialization script.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <code><![CDATA[
    /// await helper.ResetDatabaseAsync("Scripts/InitializeTestDatabase.sql");
    /// ]]></code>
    /// </remarks>
    public async Task ResetDatabaseAsync(string scriptPath, CancellationToken cancellationToken = default)
    {
        await InitializeDatabaseAsync(scriptPath, cancellationToken);
    }

    /// <summary>
    /// Clears all data from the test database tables while preserving the schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method disables foreign key constraints, truncates all tables, then re-enables constraints.
    /// <code><![CDATA[
    /// await helper.ClearAllDataAsync();
    /// ]]></code>
    /// </remarks>
    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Disable all constraints
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Delete all data
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC sp_MSforeachtable 'DELETE FROM ?'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Re-enable all constraints
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Drops the test database if it exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Use this method for cleanup after all tests have completed.
    /// <code><![CDATA[
    /// await helper.CleanupDatabaseAsync();
    /// ]]></code>
    /// </remarks>
    public async Task CleanupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Set database to single user mode to forcibly close connections
        var setSingleUserCommand = connection.CreateCommand();
        setSingleUserCommand.CommandText = $@"
            IF EXISTS (SELECT database_id FROM sys.databases WHERE name = @DatabaseName)
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            END";
        setSingleUserCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);

        try
        {
            await setSingleUserCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Ignore errors if database doesn't exist
        }

        // Drop database
        var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"IF EXISTS (SELECT database_id FROM sys.databases WHERE name = @DatabaseName) DROP DATABASE [{_databaseName}]";
        dropCommand.Parameters.AddWithValue("@DatabaseName", _databaseName);

        await dropCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new database connection to the test database.
    /// </summary>
    /// <returns>A new <see cref="IDbConnection"/> instance.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the connection.
    /// <code><![CDATA[
    /// using var connection = helper.CreateConnection();
    /// connection.Open();
    /// // ... use connection ...
    /// ]]></code>
    /// </remarks>
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_databaseConnectionString);
    }

    /// <summary>
    /// Creates and opens a new database connection to the test database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns a new opened <see cref="IDbConnection"/> instance.</returns>
    /// <remarks>
    /// The caller is responsible for disposing the connection.
    /// <code><![CDATA[
    /// await using var connection = await helper.CreateConnectionAsync();
    /// // connection is already open
    /// ]]></code>
    /// </remarks>
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Splits a SQL script into individual batches separated by GO statements.
    /// </summary>
    /// <param name="script">The SQL script text.</param>
    /// <returns>An array of SQL batch strings.</returns>
    private static string[] SplitSqlBatches(string script)
    {
        // Split on GO (case-insensitive, as a whole word)
        return System.Text.RegularExpressions.Regex.Split(
            script,
            @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }
}
