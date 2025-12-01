#region License

// Author:      Gary Wu
// Project:     DotNetToolkit Integration Tests
// Date:        December 1, 2025
// Description: Integration tests for DbConnectionFactory to verify connection creation
//              and provider factory functionality against SQL Server LocalDB.

#endregion

using System.Data;
using DotNetToolkit.Database.Abstractions;
using DotNetToolkit.Database.Configuration;
using DotNetToolkit.Database.Services;
using DotNetToolkit.Tests.Database.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DotNetToolkit.Tests.Database.Integration;

/// <summary>
/// Integration tests for <see cref="DbConnectionFactory"/> using SQL Server LocalDB.
/// </summary>
/// <remarks>
/// These tests verify that the connection factory can create and manage database connections
/// against a real SQL Server LocalDB instance.
/// <code><![CDATA[
/// // Example test execution:
/// dotnet test --filter "FullyQualifiedName~DbConnectionFactoryIntegrationTests"
/// ]]></code>
/// </remarks>
[Collection("Database")]
public class DbConnectionFactoryIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private IDbConnectionFactory _connectionFactory = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConnectionFactoryIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared database fixture.</param>
    public DbConnectionFactoryIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Sets up the test by resetting the database and creating a connection factory instance.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization.</returns>
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        var settings = new DatabaseSettings
        {
            ProviderName = "Microsoft.Data.SqlClient",
            ConnectionString = _fixture.ConnectionString
        };

        var options = Options.Create(settings);
        _connectionFactory = new DbConnectionFactory(options);
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup.</returns>
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that CreateConnection returns a valid connection instance.
    /// </summary>
    [Fact]
    public void CreateConnection_ShouldReturnValidConnection()
    {
        // Act
        var connection = _connectionFactory.CreateConnection();

        // Assert
        Assert.NotNull(connection);
        Assert.IsAssignableFrom<IDbConnection>(connection);
        Assert.IsType<SqlConnection>(connection);
        Assert.Equal(_fixture.ConnectionString, connection.ConnectionString);
    }

    /// <summary>
    /// Verifies that the created connection can be opened successfully.
    /// </summary>
    [Fact]
    public void CreateConnection_ShouldOpenSuccessfully()
    {
        // Arrange
        using var connection = _connectionFactory.CreateConnection();

        // Act
        connection.Open();

        // Assert
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    /// <summary>
    /// Verifies that the created connection can execute a simple query.
    /// </summary>
    [Fact]
    public void CreateConnection_ShouldExecuteQuery()
    {
        // Arrange
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 AS TestValue";

        // Act
        var result = command.ExecuteScalar();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result);
    }

    /// <summary>
    /// Verifies that CreateConnectionAsync returns a valid opened connection.
    /// </summary>
    [Fact]
    public async Task CreateConnectionAsync_ShouldReturnOpenedConnection()
    {
        // Act
        using var connection = await _connectionFactory.CreateConnectionAsync();

        // Assert
        Assert.NotNull(connection);
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    /// <summary>
    /// Verifies that async connection can execute queries.
    /// </summary>
    [Fact]
    public async Task CreateConnectionAsync_ShouldExecuteQueryAsync()
    {
        // Arrange
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sqlConnection = connection as SqlConnection;
        Assert.NotNull(sqlConnection);

        using var command = sqlConnection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users";

        // Act
        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True((int)result >= 0);
    }

    /// <summary>
    /// Verifies that GetProviderFactory returns the correct factory instance.
    /// </summary>
    [Fact]
    public void GetProviderFactory_ShouldReturnSqlClientFactory()
    {
        // Act
        var factory = _connectionFactory.GetProviderFactory();

        // Assert
        Assert.NotNull(factory);
        Assert.Equal(SqlClientFactory.Instance, factory);
    }

    /// <summary>
    /// Verifies that multiple connections can be created from the same factory.
    /// </summary>
    [Fact]
    public void CreateConnection_ShouldSupportMultipleConnections()
    {
        // Act
        using var connection1 = _connectionFactory.CreateConnection();
        using var connection2 = _connectionFactory.CreateConnection();

        connection1.Open();
        connection2.Open();

        // Assert
        Assert.NotSame(connection1, connection2);
        Assert.Equal(ConnectionState.Open, connection1.State);
        Assert.Equal(ConnectionState.Open, connection2.State);
    }

    /// <summary>
    /// Verifies that connection can access the test database tables.
    /// </summary>
    [Fact]
    public async Task CreateConnectionAsync_ShouldAccessTestDatabaseTables()
    {
        // Arrange
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sqlConnection = connection as SqlConnection;
        Assert.NotNull(sqlConnection);

        await using var command = sqlConnection.CreateCommand();
        command.CommandText = @"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' 
            ORDER BY TABLE_NAME";

        // Act
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        // Assert
        Assert.Contains("Users", tables);
        Assert.Contains("Orders", tables);
        Assert.Contains("OrderItems", tables);
        Assert.Contains("Products", tables);
        Assert.Contains("Categories", tables);
    }

    /// <summary>
    /// Verifies that connection factory throws appropriate exception for invalid provider.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowForUnsupportedProvider()
    {
        // Arrange
        var settings = new DatabaseSettings
        {
            ProviderName = "InvalidProvider",
            ConnectionString = "dummy"
        };
        var options = Options.Create(settings);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => new DbConnectionFactory(options));
        Assert.Contains("InvalidProvider", exception.Message);
        Assert.Contains("not supported", exception.Message);
    }

    /// <summary>
    /// Verifies that connection can execute stored procedures from test database.
    /// </summary>
    [Fact]
    public async Task CreateConnectionAsync_ShouldExecuteStoredProcedures()
    {
        // Arrange
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var sqlConnection = connection as SqlConnection;
        Assert.NotNull(sqlConnection);

        await using var command = sqlConnection.CreateCommand();
        command.CommandText = "usp_GetAllUsers";
        command.CommandType = CommandType.StoredProcedure;

        // Act
        await using var reader = await command.ExecuteReaderAsync();
        var userCount = 0;
        while (await reader.ReadAsync())
        {
            userCount++;
        }

        // Assert
        Assert.True(userCount >= 3, "Expected at least 3 seeded users");
    }
}
