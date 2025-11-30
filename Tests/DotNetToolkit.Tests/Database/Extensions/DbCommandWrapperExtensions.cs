#region License

// Author:      Gary Wu
// Project:     DotNetToolkit Integration Tests
// Date:        December 1, 2025
// Description: Extension methods for IDbCommandWrapper to simplify parameter handling in tests.

#endregion

using System.Data;
using DotNetToolkit.Database.Abstractions;

namespace DotNetToolkit.Tests.Database.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IDbCommandWrapper"/> to simplify parameter operations in tests.
/// </summary>
/// <remarks>
/// These extensions infer DbType from the parameter value type, reducing boilerplate in test code.
/// <code><![CDATA[
/// var command = dbContext.CreateCommand("usp_GetUserById");
/// command.AddParameter("@UserId", 1);  // Infers DbType.Int32
/// command.AddParameter("@Name", "John");  // Infers DbType.String
/// 
/// var outputParam = command.AddOutputParameter("@Result", DbType.Int32);
/// ]]></code>
/// </remarks>
public static class DbCommandWrapperExtensions
{
    /// <summary>
    /// Adds an input parameter to the command with automatic type inference.
    /// </summary>
    /// <param name="wrapper">The command wrapper.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The command wrapper for fluent chaining.</returns>
    /// <remarks>
    /// <code><![CDATA[
    /// command.AddParameter("@UserId", 1)
    ///        .AddParameter("@Name", "John");
    /// ]]></code>
    /// </remarks>
    public static IDbCommandWrapper AddParameter(this IDbCommandWrapper wrapper, string name, object? value)
    {
        var dbType = InferDbType(value);
        wrapper.AddParameter(name, value ?? DBNull.Value, dbType, ParameterDirection.Input);
        return wrapper;
    }

    /// <summary>
    /// Adds an output parameter to the command and returns a parameter accessor.
    /// </summary>
    /// <param name="wrapper">The command wrapper.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="dbType">The database type of the parameter.</param>
    /// <returns>A parameter accessor that can retrieve the output value after command execution.</returns>
    /// <remarks>
    /// <code><![CDATA[
    /// var outputParam = command.AddOutputParameter("@OrderId", DbType.Int32);
    /// await dbContext.ExecuteNonQueryAsync(command);
    /// var orderId = outputParam.Value;
    /// ]]></code>
    /// </remarks>
    public static OutputParameterAccessor AddOutputParameter(this IDbCommandWrapper wrapper, string name, DbType dbType)
    {
        wrapper.AddParameter(name, DBNull.Value, dbType, ParameterDirection.Output);
        return new OutputParameterAccessor(wrapper, name);
    }

    /// <summary>
    /// Adds an input/output parameter to the command.
    /// </summary>
    /// <param name="wrapper">The command wrapper.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The initial parameter value.</param>
    /// <param name="dbType">The database type of the parameter.</param>
    /// <returns>A parameter accessor that can retrieve the value after command execution.</returns>
    /// <remarks>
    /// <code><![CDATA[
    /// var inOutParam = command.AddInOutParameter("@Counter", 5, DbType.Int32);
    /// await dbContext.ExecuteNonQueryAsync(command);
    /// var newValue = inOutParam.Value;
    /// ]]></code>
    /// </remarks>
    public static OutputParameterAccessor AddInOutParameter(this IDbCommandWrapper wrapper, string name, object value, DbType dbType)
    {
        wrapper.AddParameter(name, value ?? DBNull.Value, dbType, ParameterDirection.InputOutput);
        return new OutputParameterAccessor(wrapper, name);
    }

    /// <summary>
    /// Infers the <see cref="DbType"/> from a CLR type.
    /// </summary>
    /// <param name="value">The value to infer the type from.</param>
    /// <returns>The inferred <see cref="DbType"/>.</returns>
    private static DbType InferDbType(object? value)
    {
        if (value == null || value is DBNull)
            return DbType.Object;

        return value switch
        {
            int => DbType.Int32,
            long => DbType.Int64,
            short => DbType.Int16,
            byte => DbType.Byte,
            bool => DbType.Boolean,
            string => DbType.String,
            DateTime => DbType.DateTime2,
            decimal => DbType.Decimal,
            double => DbType.Double,
            float => DbType.Single,
            Guid => DbType.Guid,
            byte[] => DbType.Binary,
            _ => DbType.Object
        };
    }
}

/// <summary>
/// Provides access to output and input/output parameter values after command execution.
/// </summary>
/// <remarks>
/// This class enables retrieval of parameter values that are set by stored procedures.
/// <code><![CDATA[
/// var outputParam = command.AddOutputParameter("@UserId", DbType.Int32);
/// await dbContext.ExecuteNonQueryAsync(command);
/// var userId = outputParam.Value<int>();  // Strongly typed
/// var userIdObj = outputParam.Value;       // As object
/// ]]></code>
/// </remarks>
public class OutputParameterAccessor
{
    private readonly IDbCommandWrapper _wrapper;
    private readonly string _parameterName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParameterAccessor"/> class.
    /// </summary>
    /// <param name="wrapper">The command wrapper containing the parameter.</param>
    /// <param name="parameterName">The name of the parameter to access.</param>
    public OutputParameterAccessor(IDbCommandWrapper wrapper, string parameterName)
    {
        _wrapper = wrapper;
        _parameterName = parameterName;
    }

    /// <summary>
    /// Gets the parameter value as an object.
    /// </summary>
    public object? Value => _wrapper.GetParameterValue<object>(_parameterName);

    /// <summary>
    /// Gets the parameter value cast to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <returns>The parameter value as <typeparamref name="T"/>.</returns>
    /// <remarks>
    /// <code><![CDATA[
    /// var userId = outputParam.Value<int>();
    /// var name = outputParam.Value<string>();
    /// ]]></code>
    /// </remarks>
    public T GetValue<T>() => _wrapper.GetParameterValue<T>(_parameterName);
}
