#region License

// author:         garyw
// created:        21:07
// description:    Reflection-based implementation of IDataMapper<T> that maps data from an IDataReader to an instance of T.
// updated:        2025-11-28 - updated file comments per Copilot instruction
// copyright:      (c) DotNetToolkit

#endregion

using DotNetToolkit.Database.Abstractions;
using System.Data;

namespace DotNetToolkit.Database.Internal;

/// <summary>
/// Provides a reflection-based implementation of <see cref="IDataMapper{T}"/> that maps data from an <see cref="IDataReader"/> to an instance of <typeparamref name="T"/>.
/// </summary>
public class ReflectionDataMapper<T> : IDataMapper<T> where T : new()
{
    /// <inheritdoc/>
    public T Map(IDataReader reader)
    {
        var obj   = new T();
        var props = typeof(T).GetProperties();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var prop = Array.Find(
                props, p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
            );

            if (prop != null && !reader.IsDBNull(i))
            {
                prop.SetValue(obj, Convert.ChangeType(reader.GetValue(i), prop.PropertyType));
            }
        }

        return obj;
    }
}
