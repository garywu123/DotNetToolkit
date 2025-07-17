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
using Microsoft.Data.SqlClient;

namespace DotNetToolkit.Database.Services;

/// <summary>
/// Provides a factory for creating database connections using the specified provider and settings.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    /// <summary>
    /// The database settings used for connection creation.
    /// </summary>
    private readonly DatabaseSettings _settings;

    /// <summary>
    /// The provider factory used to create database connections.
    /// </summary>
    private readonly DbProviderFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConnectionFactory"/> class.
    /// </summary>
    /// <param name="options">The options containing the database settings.</param>
    public DbConnectionFactory(IOptions<DatabaseSettings> options)
    {
        _settings = options.Value;
        _factory = InitializeProviderFactory();
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
    /// Initializes the <see cref="DbProviderFactory"/> based on the configured provider name.
    /// </summary>
    /// <returns>The <see cref="DbProviderFactory"/> instance for the specified provider.</returns>
    /// <exception cref="NotSupportedException">Thrown if the provider is not supported.</exception>
    private DbProviderFactory InitializeProviderFactory()
    {
        return _settings.ProviderName switch
        {
            "Microsoft.Data.SqlClient" => SqlClientFactory.Instance,
            _ => throw new NotSupportedException(
                $"Provider '{_settings.ProviderName}' is not supported by this factory."
            )
        };
    }

    /// <summary>
    /// Gets the <see cref="DbProviderFactory"/> used by this factory.
    /// </summary>
    /// <returns>The <see cref="DbProviderFactory"/> instance.</returns>
    public DbProviderFactory GetProviderFactory() => _factory;
}
