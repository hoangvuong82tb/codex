using ASMILE.BO.App.Services;

namespace ASMILE.BO.App.Infrastructure;

public static class SafeFireAndForget
{
    public static void Run(Func<Task> taskFactory, INotificationService? notificationService = null, string? failureMessage = null)
    {
        _ = RunInternalAsync(taskFactory, notificationService, failureMessage);
    }

    private static async Task RunInternalAsync(Func<Task> taskFactory, INotificationService? notificationService, string? failureMessage)
    {
        try
        {
            await taskFactory().ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(failureMessage))
            {
                notificationService?.Show(failureMessage);
            }
        }
    }
}
