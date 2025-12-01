#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using System.Data;
using System.Data.Common;
using DotNetToolkit.Database.Abstractions;
using DotNetToolkit.Database.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DotNetToolkit.Database.Services;

/// <summary>
///     Provides a factory for creating database connections using the specified
///     provider and settings.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    #region Fields

    /// <summary>
    ///     The provider factory used to create database connections.
    /// </summary>
    private readonly DbProviderFactory _factory;

    /// <summary>
    ///     The database settings used for connection creation.
    /// </summary>
    private readonly DatabaseSettings _settings;

    #endregion

    #region Constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="DbConnectionFactory" /> class.
    /// </summary>
    /// <param name="options">The options containing the database settings.</param>
    public DbConnectionFactory(IOptions<DatabaseSettings> options)
    {
        _settings = options.Value;
        _factory = InitializeProviderFactory();
    }

    #endregion

    #region Interface: IDbConnectionFactory

    #region Methods

    /// <summary>
    ///     Creates and returns a new <see cref="IDbConnection" /> using the configured
    ///     provider and connection string.
    /// </summary>
    /// <returns>A new <see cref="IDbConnection" /> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the connection could not
    ///     be created for the specified provider.
    /// </exception>
    public IDbConnection CreateConnection()
    {
        var connection = _factory.CreateConnection();
        if (connection == null)
            throw new InvalidOperationException(
                $"Could not create a connection for provider '{_settings.ProviderName}'."
            );

        connection.ConnectionString = _settings.ConnectionString;
        return connection;
    }

    /// <summary>
    ///     Asynchronously creates and returns a new <see cref="IDbConnection" /> and
    ///     opens it when supported.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns a new <see cref="IDbConnection" /> instance.</returns>
    public async Task<IDbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = _factory.CreateConnection();
        if (connection == null)
            throw new InvalidOperationException(
                $"Could not create a connection for provider '{_settings.ProviderName}'."
            );

        connection.ConnectionString = _settings.ConnectionString;

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return connection;
    }

    /// <summary>
    ///     Gets the <see cref="DbProviderFactory" /> used by this factory.
    /// </summary>
    /// <returns>The <see cref="DbProviderFactory" /> instance.</returns>
    public DbProviderFactory GetProviderFactory() { return _factory; }

    #endregion

    #endregion

    #region Methods

    /// <summary>
    ///     Initializes the <see cref="DbProviderFactory" /> based on the configured
    ///     provider name.
    /// </summary>
    /// <returns>
    ///     The <see cref="DbProviderFactory" /> instance for the specified
    ///     provider.
    /// </returns>
    /// <exception cref="NotSupportedException">
    ///     Thrown if the provider is not
    ///     supported.
    /// </exception>
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

    #endregion
}
