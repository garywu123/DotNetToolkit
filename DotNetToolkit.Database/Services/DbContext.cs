#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using System.Data;
using System.Data.Common;
using DotNetToolkit.Database.Abstractions;
using DotNetToolkit.Database.Configuration;
using DotNetToolkit.Database.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace DotNetToolkit.Database.Services;

/// <summary>
/// Represents a database context that provides methods for executing commands and queries against a database using a connection factory and data mappers.
/// </summary>
public class DbContext : IDbContext
{
    #region Fields

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DbContext>   _logger;
    private readonly IServiceProvider     _serviceProvider;
    private readonly DatabaseSettings     _settings;
    private          IDbConnection?       _connection;
    private          bool                 _disposed;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContext"/> class with the specified dependencies.
    /// </summary>
    /// <param name="connectionFactory">The connection factory to create database connections.</param>
    /// <param name="options">The database settings options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for resolving data mappers.</param>
    public DbContext(IDbConnectionFactory connectionFactory,
        IOptions<DatabaseSettings> options,
        ILogger<DbContext> logger,
        IServiceProvider serviceProvider)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = options.Value;
    }

    #endregion

    #region Interface: IDbContext

    #region Methods

    /// <summary>
    /// Creates a command wrapper for the specified command text and command type.
    /// </summary>
    /// <param name="commandText">The command text (e.g., SQL query or stored procedure name).</param>
    /// <param name="commandType">The type of the command (e.g., Text, StoredProcedure).</param>
    /// <returns>An <see cref="IDbCommandWrapper"/> instance.</returns>
    public IDbCommandWrapper CreateCommand(string commandText, CommandType commandType)
    {
        return new DbCommandWrapper(commandText, commandType, _connectionFactory.GetProviderFactory());
    }

    /// <inheritdoc/>
    public IDbCommandWrapper CreateCommand(string storedProcedureName)
    {
        return new DbCommandWrapper(storedProcedureName, CommandType.StoredProcedure, _connectionFactory.GetProviderFactory());
    }

    /// <inheritdoc/>
    public async Task<int> ExecuteNonQueryAsync(IDbCommandWrapper command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Executing non-query: {command.CommandText}");
        using var connection = _connectionFactory.CreateConnection();
        await OpenConnectionAsync(connection, cancellationToken);
        using var dbCommand = connection.CreateCommand();
        dbCommand.CommandText = command.CommandText;
        dbCommand.CommandType = command.CommandType;
        dbCommand.CommandTimeout = _settings.CommandTimeoutSeconds;
        foreach (var param in command.Parameters) dbCommand.Parameters.Add(param);
        return await ExecuteNonQueryAsync(dbCommand, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<T>> ExecuteQueryAsync<T>(IDbCommandWrapper command,
        CancellationToken cancellationToken = default) where T : new()
    {
        _logger.LogInformation($"Executing query: {command.CommandText}");
        var result = new List<T>();

        using var connection = _connectionFactory.CreateConnection();
        await OpenConnectionAsync(connection, cancellationToken);
        using var dbCommand = connection.CreateCommand();
        dbCommand.CommandText = command.CommandText;
        dbCommand.CommandType = command.CommandType;
        dbCommand.CommandTimeout = _settings.CommandTimeoutSeconds;
        foreach (var param in command.Parameters) dbCommand.Parameters.Add(param);
        using var reader = await ExecuteReaderAsync(dbCommand, cancellationToken);

        var service = _serviceProvider.GetService(typeof(IDataMapper<T>));
        var mapper  = service as IDataMapper<T> ?? new ReflectionDataMapper<T>();
        while (await ReadAsync(reader, cancellationToken)) result.Add(mapper.Map(reader));

        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _disposed = true;
    }

    #endregion

    #endregion

    #region Static Methods

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command,
        CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCmd) return await dbCmd.ExecuteNonQueryAsync(cancellationToken);
        return command.ExecuteNonQuery();
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command,
        CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCmd) return await dbCmd.ExecuteReaderAsync(cancellationToken);
        return command.ExecuteReader();
    }

    private static async Task OpenConnectionAsync(IDbConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection is DbConnection dbConn)
            await dbConn.OpenAsync(cancellationToken);
        else
            connection.Open();
    }

    private static async Task<bool> ReadAsync(IDataReader reader,
        CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbReader) return await dbReader.ReadAsync(cancellationToken);
        return reader.Read();
    }

    #endregion
}
