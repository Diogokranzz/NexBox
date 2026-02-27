using Serilog;
using ILogger = Serilog.ILogger;
using ProductManagementSystem.Application.Services;

namespace ProductManagementSystem.Api.Services;

public class LoggingService : ILoggingService
{
    private readonly ILogger _logger;

    public LoggingService()
    {
        _logger = Log.ForContext<LoggingService>();
    }

    public void LogInformation(string message, params object?[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning(string message, params object?[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError(Exception exception, string message, params object?[] args)
    {
        _logger.Error(exception, message, args);
    }

    public void LogDebug(string message, params object?[] args)
    {
        _logger.Debug(message, args);
    }
}
