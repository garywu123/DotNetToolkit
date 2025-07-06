#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using DotNetToolkit.Database.Abstractions;
using System.Data;
using System.Data.Common;

namespace DotNetToolkit.Database.Internal;

/// <summary>
/// Provides an implementation of <see cref="IDbCommandWrapper"/> that encapsulates command text, command type, and parameters using a specific <see cref="DbProviderFactory"/>.
/// </summary>
public class DbCommandWrapper : IDbCommandWrapper
{
    private readonly List<IDbDataParameter>        _parameters = [];
    private readonly DbProviderFactory             _factory;
    public           string                        CommandText { get; }
    public           CommandType                   CommandType { get; }
    public           IEnumerable<IDbDataParameter> Parameters  => _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandWrapper"/> class with the specified command text, command type, and provider factory.
    /// </summary>
    /// <param name="commandText">The command text (e.g., SQL query or stored procedure name).</param>
    /// <param name="commandType">The type of the command (e.g., Text, StoredProcedure).</param>
    /// <param name="factory">The provider factory to create parameters.</param>
    public DbCommandWrapper(string commandText, CommandType commandType, DbProviderFactory factory)
    {
        CommandText = commandText;
        CommandType = commandType;
        _factory = factory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandWrapper"/> class with the specified command text and provider factory. Defaults to StoredProcedure.
    /// </summary>
    /// <param name="commandText">The command text (e.g., stored procedure name).</param>
    /// <param name="factory">The provider factory to create parameters.</param>
    public DbCommandWrapper(string commandText, DbProviderFactory factory)
        : this(commandText, CommandType.StoredProcedure, factory) { }

    /// <inheritdoc/>
    public void AddParameter(string name,
        object value,
        DbType type,
        ParameterDirection direction = ParameterDirection.Input)
    {
        var param = _factory.CreateParameter();
        if (param == null)
            throw new InvalidOperationException(
                "Could not create a parameter from the provider factory."
            );

        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        param.DbType = type;
        param.Direction = direction;
        _parameters.Add(param);
    }

    /// <inheritdoc/>
    public T GetParameterValue<T>(string name)
    {
        var param = _parameters.Find(p => p.ParameterName == name);
        if (param == null)
            throw new ArgumentException($"Parameter '{name}' not found.");

        if (param.Value is null or DBNull)
            return default!;

        try
        {
            return (T)Convert.ChangeType(param.Value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidCastException(
                $"Cannot convert parameter '{name}' value to type {typeof(T).Name}.", ex);
        }
    }
}
