#region License

// author:         garyw
// created:        21:07
// description:

#endregion

namespace DotNetToolkit.Database.Configuration;

/// <summary>
/// Represents the configuration settings required for database connectivity, including connection string, provider name, and command timeout.
/// </summary>
public class DatabaseSettings
{
    public string ConnectionString      { get; set; } = string.Empty;
    public string ProviderName          { get; set; } = string.Empty;
    public int    CommandTimeoutSeconds { get; set; } = 30;
}
