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

    void LogError(string message);

    void LogCritical(string message);

}
