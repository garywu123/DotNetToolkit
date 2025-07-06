#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using System.Data;
using System.Data.Common;
using DotNetToolkit.Database.Abstractions;
using DotNetToolkit.Database.Configuration;
using Microsoft.Extensions.Options;

namespace DotNetToolkit.Database.Services;

/// <summary>
/// Provides a factory for creating database connections using the specified provider and settings.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseSettings _settings;
    private readonly DbProviderFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">The options containing the database settings.</param>
    public DbConnectionFactory(IOptions<DatabaseSettings> options)
    {
        _settings = options.Value;
        _factory = DbProviderFactories.GetFactory(_settings.ProviderName);
    }

    /// <summary>
    /// Creates and returns a new <see cref="IDbConnection"/> using the configured provider and connection string.
    /// </summary>
    /// <returns>A new <see cref="IDbConnection"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection could not be created for the specified provider.</exception>
    public IDbConnection CreateConnection()
    {
        var connection = _factory.CreateConnection();
        if (connection == null)
            throw new InvalidOperationException($"Could not create a connection for provider '{_settings.ProviderName}'.");
        connection.ConnectionString = _settings.ConnectionString;
        return connection;
    }

    /// <summary>
    /// Gets the <see cref="DbProviderFactory"/> used by this factory.
    /// </summary>
    /// <returns>The <see cref="DbProviderFactory"/> instance.</returns>
    public DbProviderFactory GetProviderFactory() => _factory;
}
