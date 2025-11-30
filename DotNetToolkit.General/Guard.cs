using System;

namespace DotNetToolkit.General
{
    /// <summary>
    /// Lightweight guard helpers used across toolkit libraries.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Ensures the provided value is not null.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="paramName">Name of the parameter to use in the exception.</param>
        public static void NotNull(object? value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Ensures the provided string is not null or empty. Throws <see cref="ArgumentNullException"/>
        /// when null and <see cref="ArgumentException"/> when empty.
        /// </summary>
        /// <param name="value">String to validate.</param>
        /// <param name="paramName">Name of the parameter to use in the exception.</param>
        public static void NotNullOrEmpty(string? value, string paramName)
        {
            if (value is null)
                throw new ArgumentNullException(paramName);

            if (value.Length == 0)
                throw new ArgumentException($"'{paramName}' cannot be empty.", paramName);
        }
    }
}
