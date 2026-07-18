namespace ASMILE.BO.App.Services;

public interface INotificationService
{
    event Action<string>? MessageRaised;
    void Show(string message);
}

