// Author:      Gary Wu
// Project:     DotNetToolkit.Database
// Date:        2025-12-01
// Description: Provides a small accessor for reading output/inout parameter values
//              from an <see cref="IDbCommandWrapper"/> after command execution.

using DotNetToolkit.Database.Abstractions;

namespace DotNetToolkit.Database.Extensions
{
    /// <summary>
    /// Small helper that wraps an <see cref="IDbCommandWrapper"/> and a parameter
    /// name to provide typed access to an output or input/output parameter after
    /// a command has executed.
    /// </summary>
    public sealed class OutputParameterAccessor
    {
        private readonly IDbCommandWrapper _wrapper;
        private readonly string _parameterName;

        /// <summary>
        /// Initializes a new instance of <see cref="OutputParameterAccessor"/>.
        /// </summary>
        /// <param name="wrapper">The command wrapper.</param>
        /// <param name="parameterName">Name of the parameter to access.</param>
        public OutputParameterAccessor(IDbCommandWrapper wrapper, string parameterName)
        {
            _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
            _parameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        /// <summary>
        /// Gets the parameter value as <see cref="object"/>. May be <see langword="null"/>.
        /// </summary>
        public object? Value => _wrapper.GetParameterValue<object>(_parameterName);

        /// <summary>
        /// Gets the parameter value as the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type to convert the parameter value to.</typeparam>
        /// <returns>The parameter value converted to <typeparamref name="T"/>.</returns>
        public T GetValue<T>() => _wrapper.GetParameterValue<T>(_parameterName);
    }
}
