#region License

// author:         garyw
// created:        21:07
// description:

#endregion

namespace DotNetToolkit.Database.Abstractions;

/// <summary>
/// Defines a contract for a database context that provides methods for executing commands and queries against a database.
/// </summary>
public interface IDbContext : IDisposable
{
    #region Methods

    /// <summary>
    /// Creates a command wrapper for the specified stored procedure name.
    /// </summary>
    /// <param name="storedProcedureName">The name of the stored procedure.</param>
    /// <returns>An <see cref="IDbCommandWrapper"/> instance.</returns>
    IDbCommandWrapper CreateCommand(string storedProcedureName);

    /// <summary>
    /// Creates a command wrapper for the specified command text and command type.
    /// </summary>
    /// <param name="commandText">The command text (e.g., SQL query or stored procedure name).</param>
    /// <param name="commandType">The type of the command (e.g., Text, StoredProcedure).</param>
    /// <returns>An <see cref="IDbCommandWrapper"/> instance.</returns>
    IDbCommandWrapper CreateCommand(string commandText, System.Data.CommandType commandType);

    /// <summary>
    /// Executes a non-query command asynchronously.
    /// </summary>
    /// <param name="command">The command wrapper to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteNonQueryAsync(IDbCommandWrapper command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query asynchronously and maps the result to a list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to map the result to.</typeparam>
    /// <param name="command">The command wrapper to execute.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of mapped results.</returns>
    Task<List<T>> ExecuteQueryAsync<T>(IDbCommandWrapper command,
        CancellationToken cancellationToken = default) where T : new();

    #endregion
}
