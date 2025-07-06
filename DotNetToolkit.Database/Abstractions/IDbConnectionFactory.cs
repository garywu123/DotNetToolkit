#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using System.Data;
using System.Data.Common;

namespace DotNetToolkit.Database.Abstractions;

/// <summary>
/// Defines a contract for a factory that creates database connections and provides the associated provider factory.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and returns a new <see cref="IDbConnection"/>.
    /// </summary>
    /// <returns>A new <see cref="IDbConnection"/> instance.</returns>
    IDbConnection CreateConnection();

    /// <summary>
    /// Gets the <see cref="DbProviderFactory"/> used by this factory.
    /// </summary>
    /// <returns>The <see cref="DbProviderFactory"/> instance.</returns>
    DbProviderFactory GetProviderFactory();
}
