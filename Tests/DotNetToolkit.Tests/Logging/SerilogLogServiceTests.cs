// -----------------------------------------------------------------------
// <copyright file="SerilogLogServiceTests.cs" company="DotNetToolkit">
//     Author: Gary Wu
//     Project: DotNetToolkit.Tests
//     Date: December 1, 2025
//     Description: Unit tests for SerilogLogService verifying that each
//                  ILogService method correctly invokes the corresponding
//                  Serilog logging level.
// </copyright>
// -----------------------------------------------------------------------

using DotNetToolkit.Logging;
using DotNetToolkit.Logging.Extensions;
using DotNetToolkit.Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DotNetToolkit.Tests.Logging;

/// <summary>
/// Unit tests for <see cref="SerilogLogService"/> that verify each <see cref="ILogService"/> method
/// correctly maps to the corresponding Serilog log level.
/// </summary>
public class SerilogLogServiceTests
{
    #region Test Infrastructure

    /// <summary>
    /// A test sink that captures log events for verification.
    /// </summary>
    private sealed class TestLogEventSink : ILogEventSink
    {
        private readonly List<LogEvent> _events = [];

        /// <summary>
        /// Gets all captured log events.
        /// </summary>
        public IReadOnlyList<LogEvent> Events => _events;

        /// <summary>
        /// Gets the last captured log event, or null if no events have been captured.
        /// </summary>
        public LogEvent? LastEvent => _events.Count > 0 ? _events[^1] : null;

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }

        /// <summary>
        /// Clears all captured events.
        /// </summary>
        public void Clear() => _events.Clear();
    }

    private readonly TestLogEventSink _testSink;
    private readonly ILogger _serilogLogger;
    private readonly SerilogLogService _logService;

    public SerilogLogServiceTests()
    {
        _testSink = new TestLogEventSink();
        _serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_testSink)
            .CreateLogger();
        _logService = new SerilogLogService(_serilogLogger);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SerilogLogService(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange & Act
        var service = new SerilogLogService(_serilogLogger);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region Sync Method Tests

    [Fact]
    public void LogVerbose_LogsAtVerboseLevel()
    {
        // Arrange
        const string message = "Verbose test message";

        // Act
        _logService.LogVerbose(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Verbose, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public void LogDebug_LogsAtDebugLevel()
    {
        // Arrange
        const string message = "Debug test message";

        // Act
        _logService.LogDebug(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Debug, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public void LogInformation_LogsAtInformationLevel()
    {
        // Arrange
        const string message = "Information test message";

        // Act
        _logService.LogInformation(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Information, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public void LogWarning_LogsAtWarningLevel()
    {
        // Arrange
        const string message = "Warning test message";

        // Act
        _logService.LogWarning(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Warning, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public void LogError_WithoutException_LogsAtErrorLevel()
    {
        // Arrange
        const string message = "Error test message";

        // Act
        _logService.LogError(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Error, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Null(_testSink.LastEvent.Exception);
    }

    [Fact]
    public void LogError_WithException_LogsAtErrorLevelWithException()
    {
        // Arrange
        const string message = "Error test message with exception";
        var exception = new InvalidOperationException("Test exception");

        // Act
        _logService.LogError(message, exception);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Error, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Same(exception, _testSink.LastEvent.Exception);
    }

    [Fact]
    public void LogCritical_WithoutException_LogsAtFatalLevel()
    {
        // Arrange
        const string message = "Critical test message";

        // Act
        _logService.LogCritical(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Fatal, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Null(_testSink.LastEvent.Exception);
    }

    [Fact]
    public void LogCritical_WithException_LogsAtFatalLevelWithException()
    {
        // Arrange
        const string message = "Critical test message with exception";
        var exception = new InvalidOperationException("Critical exception");

        // Act
        _logService.LogCritical(message, exception);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Fatal, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Same(exception, _testSink.LastEvent.Exception);
    }

    #endregion

    #region Async Method Tests

    [Fact]
    public async Task LogVerboseAsync_LogsAtVerboseLevel()
    {
        // Arrange
        const string message = "Verbose async test message";

        // Act
        await _logService.LogVerboseAsync(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Verbose, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public async Task LogDebugAsync_LogsAtDebugLevel()
    {
        // Arrange
        const string message = "Debug async test message";

        // Act
        await _logService.LogDebugAsync(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Debug, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public async Task LogInformationAsync_LogsAtInformationLevel()
    {
        // Arrange
        const string message = "Information async test message";

        // Act
        await _logService.LogInformationAsync(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Information, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public async Task LogWarningAsync_LogsAtWarningLevel()
    {
        // Arrange
        const string message = "Warning async test message";

        // Act
        await _logService.LogWarningAsync(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Warning, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
    }

    [Fact]
    public async Task LogErrorAsync_WithoutException_LogsAtErrorLevel()
    {
        // Arrange
        const string message = "Error async test message";

        // Act
        await _logService.LogErrorAsync(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Error, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Null(_testSink.LastEvent.Exception);
    }

    [Fact]
    public async Task LogErrorAsync_WithException_LogsAtErrorLevelWithException()
    {
        // Arrange
        const string message = "Error async test message with exception";
        var exception = new InvalidOperationException("Async test exception");

        // Act
        await _logService.LogErrorAsync(message, exception);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Error, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Same(exception, _testSink.LastEvent.Exception);
    }

    [Fact]
    public async Task LogCriticalAsync_WithoutException_LogsAtFatalLevel()
    {
        // Arrange
        const string message = "Critical async test message";

        // Act
        await _logService.LogCriticalAsync(message);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Fatal, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Null(_testSink.LastEvent.Exception);
    }

    [Fact]
    public async Task LogCriticalAsync_WithException_LogsAtFatalLevelWithException()
    {
        // Arrange
        const string message = "Critical async test message with exception";
        var exception = new InvalidOperationException("Critical async exception");

        // Act
        await _logService.LogCriticalAsync(message, exception);

        // Assert
        Assert.NotNull(_testSink.LastEvent);
        Assert.Equal(LogEventLevel.Fatal, _testSink.LastEvent.Level);
        Assert.Contains(message, _testSink.LastEvent.RenderMessage());
        Assert.Same(exception, _testSink.LastEvent.Exception);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task LogVerboseAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _logService.LogVerboseAsync("message", cts.Token));
    }

    [Fact]
    public async Task LogDebugAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _logService.LogDebugAsync("message", cts.Token));
    }

    [Fact]
    public async Task LogInformationAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _logService.LogInformationAsync("message", cts.Token));
    }

    [Fact]
    public async Task LogWarningAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _logService.LogWarningAsync("message", cts.Token));
    }

    [Fact]
    public async Task LogErrorAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _logService.LogErrorAsync("message", null, cts.Token));
    }

    [Fact]
    public async Task LogCriticalAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _logService.LogCriticalAsync("message", null, cts.Token));
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddSerilogLogService_WithLogger_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSerilogLogService(_serilogLogger);
        using var provider = services.BuildServiceProvider();

        // Assert
        var logService = provider.GetService<ILogService>();
        Assert.NotNull(logService);
        Assert.IsType<SerilogLogService>(logService);
    }

    [Fact]
    public void AddSerilogLogService_WithFactory_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSerilogLogService(_ => _serilogLogger);
        using var provider = services.BuildServiceProvider();

        // Assert
        var logService = provider.GetService<ILogService>();
        Assert.NotNull(logService);
        Assert.IsType<SerilogLogService>(logService);
    }

    [Fact]
    public void AddSerilogLogService_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddSerilogLogService(_serilogLogger));
    }

    [Fact]
    public void AddSerilogLogService_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddSerilogLogService((ILogger)null!));
    }

    [Fact]
    public void AddSerilogLogService_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddSerilogLogService((Func<IServiceProvider, ILogger>)null!));
    }

    [Fact]
    public void ILogService_CanBeResolvedWithoutKnowingSerilog()
    {
        // This test confirms that consuming code only needs to depend on ILogService,
        // not on Serilog types directly.

        // Arrange
        var services = new ServiceCollection();
        services.AddSerilogLogService(_serilogLogger);
        using var provider = services.BuildServiceProvider();

        // Act - resolve only ILogService, not SerilogLogService
        var logService = provider.GetRequiredService<ILogService>();

        // Assert
        logService.LogInformation("Test message - consumer only sees ILogService");
        Assert.NotNull(_testSink.LastEvent);
    }

    #endregion
}
