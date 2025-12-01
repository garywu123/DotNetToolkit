// -----------------------------------------------------------------------
// <copyright file="LoggingServiceCollectionExtensions.cs" company="DotNetToolkit">
//     Author: Gary Wu
//     Project: DotNetToolkit.Logging
//     Date: December 1, 2025
//     Description: Extension methods for registering logging services
//                  in the dependency injection container.
// </copyright>
// -----------------------------------------------------------------------

using DotNetToolkit.Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DotNetToolkit.Logging.Extensions;

/// <summary>
/// Provides extension methods for registering logging-related services in the dependency injection container.
/// </summary>
/// <remarks>
/// This class enables clean separation between the logging abstraction (<see cref="ILogService"/>) and
/// the concrete Serilog implementation. Application hosts can configure Serilog as needed and register
/// it with the DI container, while core application code depends only on <see cref="ILogService"/>.
/// </remarks>
/// <example>
/// <code><![CDATA[
/// // In your application startup (Program.cs or Startup.cs)
/// var serilogLogger = new LoggerConfiguration()
///     .MinimumLevel.Debug()
///     .WriteTo.Console()
///     .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
///     .CreateLogger();
/// 
/// services.AddSerilogLogService(serilogLogger);
/// 
/// // Now ILogService can be injected anywhere in your application
/// public class MyService
/// {
///     private readonly ILogService _logService;
///     
///     public MyService(ILogService logService)
///     {
///         _logService = logService;
///     }
/// }
/// ]]></code>
/// </example>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Serilog-backed <see cref="ILogService"/> implementation to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="serilogLogger">The configured Serilog <see cref="ILogger"/> instance to use for logging.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="serilogLogger"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <see cref="SerilogLogService"/> is registered as a singleton since the underlying Serilog logger
    /// is designed to be shared across the application. The caller is responsible for configuring the
    /// Serilog logger with appropriate sinks, minimum levels, and enrichers before calling this method.
    /// </para>
    /// <para>
    /// This method does not configure any Serilog sinks, file paths, or environment-specific settings.
    /// All configuration decisions are left to the application host.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddSerilogLogService(this IServiceCollection services, ILogger serilogLogger)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serilogLogger);

        services.AddSingleton(serilogLogger);
        services.AddSingleton<ILogService, SerilogLogService>();

        return services;
    }

    /// <summary>
    /// Adds the Serilog-backed <see cref="ILogService"/> implementation to the specified <see cref="IServiceCollection"/>
    /// using a factory function to create the Serilog logger.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="loggerFactory">A factory function that creates the Serilog <see cref="ILogger"/> instance.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="loggerFactory"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// This overload is useful when the Serilog logger configuration depends on services from the DI container.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// services.AddSerilogLogService(sp =>
    /// {
    ///     var config = sp.GetRequiredService<IConfiguration>();
    ///     return new LoggerConfiguration()
    ///         .ReadFrom.Configuration(config)
    ///         .CreateLogger();
    /// });
    /// ]]></code>
    /// </example>
    public static IServiceCollection AddSerilogLogService(
        this IServiceCollection services,
        Func<IServiceProvider, ILogger> loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        services.AddSingleton(loggerFactory);
        services.AddSingleton<ILogService, SerilogLogService>();

        return services;
    }
}
