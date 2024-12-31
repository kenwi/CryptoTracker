public interface IKeyPressHandlerService
{
    Task StartListening(Action onSpacebar, CancellationToken cancellationToken);
}
