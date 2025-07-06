#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using System.Data;

namespace DotNetToolkit.Database.Abstractions;

/// <summary>
/// Defines a contract for a command wrapper that encapsulates command text, command type, and parameters for database operations.
/// </summary>
public interface IDbCommandWrapper
{
    /// <summary>
    /// Adds a parameter to the command.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <param name="type">The database type of the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    void AddParameter(string name, object value, DbType type, ParameterDirection direction = ParameterDirection.Input);

    /// <summary>
    /// Gets the value of a parameter by name.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter value.</typeparam>
    /// <param name="name">The parameter name.</param>
    /// <returns>The value of the parameter cast to <typeparamref name="T"/>.</returns>
    T GetParameterValue<T>(string name);

    /// <summary>
    /// Gets the command text (e.g., SQL query or stored procedure name).
    /// </summary>
    string CommandText { get; }

    /// <summary>
    /// Gets the command type (e.g., Text, StoredProcedure, TableDirect).
    /// </summary>
    CommandType CommandType { get; }

    /// <summary>
    /// Gets the collection of parameters for the command.
    /// </summary>
    IEnumerable<IDbDataParameter> Parameters { get; }
}
