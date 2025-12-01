// -----------------------------------------------------------------------
// <copyright file="SerilogLogService.cs" company="DotNetToolkit">
//     Author: Gary Wu
//     Project: DotNetToolkit.Logging
//     Date: December 1, 2025
//     Description: Serilog-backed implementation of ILogService providing
//                  a clean logging abstraction for any application.
// </copyright>
// -----------------------------------------------------------------------

using Serilog;
using Serilog.Events;

namespace DotNetToolkit.Logging.Services;

/// <summary>
/// A Serilog-backed implementation of <see cref="ILogService"/> that wraps a Serilog <see cref="ILogger"/> instance.
/// This class provides a clean abstraction over Serilog, allowing applications to depend on <see cref="ILogService"/>
/// without knowing the underlying logging implementation.
/// </summary>
/// <remarks>
/// <para>
/// The async methods in this implementation delegate to the synchronous Serilog methods and return
/// <see cref="Task.CompletedTask"/> since Serilog's core logging is synchronous by design.
/// For true async behavior with async sinks, the sink handles the async operations internally.
/// </para>
/// </remarks>
/// <example>
/// <code><![CDATA[
/// // Create a Serilog logger
/// var serilogLogger = new LoggerConfiguration()
///     .MinimumLevel.Debug()
///     .WriteTo.Console()
///     .CreateLogger();
/// 
/// // Create the log service
/// ILogService logService = new SerilogLogService(serilogLogger);
/// 
/// // Use the logging abstraction
/// logService.LogInformation("Application started");
/// logService.LogError("An error occurred", exception);
/// ]]></code>
/// </example>
public sealed class SerilogLogService : ILogService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogLogService"/> class.
    /// </summary>
    /// <param name="logger">The Serilog <see cref="ILogger"/> instance to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <c>null</c>.</exception>
    public SerilogLogService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void LogVerbose(string message)
    {
        _logger.Verbose(message);
    }

    /// <inheritdoc />
    public void LogDebug(string message)
    {
        _logger.Debug(message);
    }

    /// <inheritdoc />
    public void LogInformation(string message)
    {
        _logger.Information(message);
    }

    /// <inheritdoc />
    public void LogWarning(string message)
    {
        _logger.Warning(message);
    }

    /// <inheritdoc />
    public void LogError(string message, Exception? ex = null)
    {
        if (ex is not null)
        {
            _logger.Error(ex, message);
        }
        else
        {
            _logger.Error(message);
        }
    }

    /// <inheritdoc />
    public void LogCritical(string message, Exception? ex = null)
    {
        if (ex is not null)
        {
            _logger.Fatal(ex, message);
        }
        else
        {
            _logger.Fatal(message);
        }
    }

    /// <inheritdoc />
    public Task LogVerboseAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogVerbose(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogDebugAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogDebug(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogInformationAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogInformation(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogWarningAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogWarning(message);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogErrorAsync(string message, Exception? ex = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogError(message, ex);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogCriticalAsync(string message, Exception? ex = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogCritical(message, ex);
        return Task.CompletedTask;
    }
}
