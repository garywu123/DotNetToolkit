#region License

// author:         garyw
// created:        21:07
// description:

#endregion

using System.Data;

namespace DotNetToolkit.Database.Abstractions;

/// <summary>
/// Defines a contract for mapping data from an <see cref="IDataReader"/> to an instance of type <typeparamref name="T"/>.
/// </summary>
public interface IDataMapper<T>
{
    /// <summary>
    /// Maps the current row of the specified <see cref="IDataReader"/> to an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="reader">The data reader positioned at the row to map.</param>
    /// <returns>An instance of <typeparamref name="T"/> mapped from the current row.</returns>
    T Map(IDataReader reader);
}
