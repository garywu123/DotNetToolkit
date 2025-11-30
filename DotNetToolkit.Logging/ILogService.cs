#region License

// author:         garyw
// created:        15:07
// description:

#endregion

namespace DotNetToolkit.Logging;

public interface ILogService
{
    void LogVerbose(string message);

    void LogDebug(string message);

    void LogInformation(string message);

    void LogWarning(string message);

    void LogError(string message, Exception? ex = null);

    void LogCritical(string message, Exception? ex = null);

    // Async-friendly variants that support CancellationToken for adapters that implement async sinks.
    Task LogVerboseAsync(string message, CancellationToken cancellationToken = default);

    Task LogDebugAsync(string message, CancellationToken cancellationToken = default);

    Task LogInformationAsync(string message, CancellationToken cancellationToken = default);

    Task LogWarningAsync(string message, CancellationToken cancellationToken = default);

    Task LogErrorAsync(string message, Exception? ex = null, CancellationToken cancellationToken = default);

    Task LogCriticalAsync(string message, Exception? ex = null, CancellationToken cancellationToken = default);

}
