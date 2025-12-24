// Author:      Gary Wu
// Project:     DotNetToolkit.Database
// Date:        2025-12-01
// Description: Convenience extension methods for IDbCommandWrapper to infer DbType
//              and simplify adding parameters from callers. Preserves explicit
//              AddParameter(name, value, DbType, ...) API and delegates to it.

using System.Data;
using DotNetToolkit.Database.Abstractions;

namespace DotNetToolkit.Database.Extensions
{
    /// <summary>
    /// Extension methods that provide convenient overloads for adding parameters to
    /// an <see cref="IDbCommandWrapper"/>. These helpers infer a <see cref="DbType"/>
    /// from the supplied value when callers omit the explicit type.
    /// </summary>
    public static class DbCommandWrapperExtensions
    {
        /// <summary>
        /// Adds a parameter to the command with inferred <see cref="DbType"/> based on
        /// the provided <paramref name="value"/>. If <paramref name="value"/> is
        /// <see langword="null"/>, <see cref="DBNull.Value"/> is used.
        /// </summary>
        /// <param name="wrapper">The command wrapper.</param>
        /// <param name="name">Parameter name (including @ where appropriate).</param>
        /// <param name="value">Value to assign to the parameter.</param>
        /// <param name="direction">Parameter direction. Defaults to <see cref="ParameterDirection.Input"/>.</param>
        /// <returns>The same <see cref="IDbCommandWrapper"/> instance for fluent usage.</returns>
        /// <example>
        /// <code><![CDATA[
        /// command.AddParameter("@UserId", 123);
        /// command.AddParameter("@Name", "Alice");
        /// ]]></code>
        /// </example>
        public static IDbCommandWrapper AddParameter(this IDbCommandWrapper wrapper, string name, object? value, ParameterDirection direction = ParameterDirection.Input)
        {
            var dbType = InferDbType(value);
            wrapper.AddParameter(name, value ?? DBNull.Value, dbType, direction);
            return wrapper;
        }

        /// <summary>
        /// Adds an output parameter and returns an <see cref="OutputParameterAccessor"/>
        /// that can be used to retrieve the parameter's value after command execution.
        /// </summary>
        /// <param name="wrapper">The command wrapper.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="dbType">The <see cref="DbType"/> for the output parameter.</param>
        /// <returns>An <see cref="OutputParameterAccessor"/> for reading the value.</returns>
        /// <example>
        /// <code><![CDATA[
        /// var idAccessor = command.AddOutputParameter("@NewId", DbType.Int32);
        /// await dbContext.ExecuteNonQueryAsync(command);
        /// var id = idAccessor.GetValue<int>();
        /// ]]></code>
        /// </example>
        public static OutputParameterAccessor AddOutputParameter(this IDbCommandWrapper wrapper, string name, DbType dbType)
        {
            wrapper.AddParameter(name, DBNull.Value, dbType, ParameterDirection.Output);
            return new OutputParameterAccessor(wrapper, name);
        }

        /// <summary>
        /// Adds an input/output parameter and returns an <see cref="OutputParameterAccessor"/>
        /// that can be used to read the parameter's final value after execution.
        /// </summary>
        /// <param name="wrapper">The command wrapper.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Initial value for the parameter.</param>
        /// <param name="dbType">The <see cref="DbType"/> for the parameter.</param>
        /// <returns>An <see cref="OutputParameterAccessor"/> for reading the value.</returns>
        public static OutputParameterAccessor AddInOutParameter(this IDbCommandWrapper wrapper, string name, object? value, DbType dbType)
        {
            wrapper.AddParameter(name, value ?? DBNull.Value, dbType, ParameterDirection.InputOutput);
            return new OutputParameterAccessor(wrapper, name);
        }

        /// <summary>
        /// Infers a <see cref="DbType"/> for the provided value. This mapping covers
        /// common CLR types used in the project; unknown types default to <see cref="DbType.Object"/>.
        /// </summary>
        /// <param name="value">Value to inspect.</param>
        /// <returns>Inferred <see cref="DbType"/>.</returns>
        private static DbType InferDbType(object? value)
        {
            if (value == null || value == DBNull.Value)
            {
                return DbType.Object;
            }

            var type = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(bool)) return DbType.Boolean;
            if (type == typeof(string)) return DbType.String;
            if (type == typeof(DateTime)) return DbType.DateTime2;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(Guid)) return DbType.Guid;
            if (type == typeof(byte[])) return DbType.Binary;

            return DbType.Object;
        }
    }
}
