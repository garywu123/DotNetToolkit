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

            if (prop == null || reader.IsDBNull(i)) continue;

            var rawValue   = reader.GetValue(i);
            var propType   = prop.PropertyType;
            var targetType = Nullable.GetUnderlyingType(propType) ?? propType;

            object? converted;

            if (targetType.IsEnum)
            {
                // Support both numeric and string-backed enums
                converted = rawValue is string s
                    ? Enum.Parse(targetType, s)
                    : Enum.ToObject(targetType, rawValue);
            }
            else if (targetType == typeof(Guid))
            {
                converted = rawValue is Guid g ? g : Guid.Parse(rawValue.ToString()!);
            }
            else
            {
                // Convert to the underlying (non-nullable) type, e.g. Int32 for Nullable<int>
                converted = Convert.ChangeType(rawValue, targetType);
            }

            prop.SetValue(obj, converted);
        }

        return obj;
    }
}
