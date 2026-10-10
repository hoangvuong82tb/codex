namespace ASMILE.BO.App.Services;

public static class SettingsConfirmationPolicy
{
    public static async Task<bool> ConfirmIfRequiredAsync(
        bool requireConfirmation,
        Func<Task<bool>> confirmAsync)
    {
        ArgumentNullException.ThrowIfNull(confirmAsync);
        return !requireConfirmation || await confirmAsync().ConfigureAwait(true);
    }
}
