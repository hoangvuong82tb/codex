namespace ASMILE.BO.App.Services;

public sealed class NotificationService : INotificationService
{
    public event Action<string>? MessageRaised;

    public void Show(string message)
    {
        MessageRaised?.Invoke(message);
    }
}

