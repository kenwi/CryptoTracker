using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class KeyPressHandlerService : IKeyPressHandlerService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<KeyPressHandlerService> _logger;

    public KeyPressHandlerService(
        IHostApplicationLifetime applicationLifetime,
        ILogger<KeyPressHandlerService> logger)
    {
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    public Task StartListening(Action onSpacebar, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting key press handler");
        
        return Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Spacebar)
                {
                    _logger.LogDebug("Spacebar pressed - triggering update");
                    onSpacebar();
                }
                if (key.Key == ConsoleKey.Enter)
                {
                    _logger.LogInformation("Enter pressed - stopping application");
                    _applicationLifetime.StopApplication();
                    return;
                }
            }
        }, cancellationToken);
    }
} 