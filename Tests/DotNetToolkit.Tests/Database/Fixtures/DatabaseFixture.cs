#region License

// Author:      Gary Wu
// Project:     DotNetToolkit Integration Tests
// Date:        December 1, 2025
// Description: Provides base class and collection fixture for database integration tests
//              with automatic database initialization and cleanup.

#endregion

using DotNetToolkit.Tests.Database.Helpers;

namespace DotNetToolkit.Tests.Database.Fixtures;

/// <summary>
/// Provides a shared database fixture for integration tests, managing database lifecycle across test collections.
/// </summary>
/// <remarks>
/// This fixture creates and initializes the test database once per test collection, improving test performance.
/// <code><![CDATA[
/// [Collection("Database")]
/// public class MyIntegrationTests
/// {
///     private readonly DatabaseFixture _fixture;
///     
///     public MyIntegrationTests(DatabaseFixture fixture)
///     {
///         _fixture = fixture;
///     }
/// }
/// ]]></code>
/// </remarks>
// ReSharper disable once ClassNeverInstantiated.Global
public class DatabaseFixture : IAsyncLifetime
{
    private readonly TestDatabaseHelper _databaseHelper;

    /// <summary>
    /// Gets the test database helper instance.
    /// </summary>
    public TestDatabaseHelper DatabaseHelper => _databaseHelper;

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _databaseHelper.ConnectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseFixture"/> class.
    /// </summary>
    public DatabaseFixture()
    {
        _databaseHelper = new TestDatabaseHelper("DotNetToolkitTest");
    }

    /// <summary>
    /// Initializes the database fixture by creating and seeding the test database.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        await _databaseHelper.EnsureDatabaseExistsAsync();
        await _databaseHelper.InitializeDatabaseAsync("Database/Scripts/InitializeTestDatabase.sql");
    }

    /// <summary>
    /// Cleans up the database fixture by dropping the test database.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    public async Task DisposeAsync()
    {
        await _databaseHelper.CleanupDatabaseAsync();
    }

    /// <summary>
    /// Resets the database to its initial state for test isolation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    /// <remarks>
    /// Call this method in test setup to ensure a clean database state for each test.
    /// <code><![CDATA[
    /// public async Task InitializeAsync()
    /// {
    ///     await _fixture.ResetDatabaseAsync();
    /// }
    /// ]]></code>
    /// </remarks>
    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _databaseHelper.ResetDatabaseAsync("Database/Scripts/InitializeTestDatabase.sql", cancellationToken);
    }
}

/// <summary>
/// Defines the database test collection for grouping integration tests.
/// </summary>
/// <remarks>
/// Tests in this collection share the same <see cref="DatabaseFixture"/> instance,
/// ensuring efficient database management across multiple test classes.
/// <code><![CDATA[
/// [Collection("Database")]
/// public class MyTests : IAsyncLifetime
/// {
///     private readonly DatabaseFixture _fixture;
///     
///     public MyTests(DatabaseFixture fixture)
///     {
///         _fixture = fixture;
///     }
///     
///     public async Task InitializeAsync()
///     {
///         await _fixture.ResetDatabaseAsync();
///     }
///     
///     public Task DisposeAsync() => Task.CompletedTask;
/// }
/// ]]></code>
/// </remarks>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
