using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Services;

namespace ASMILE.BO.App.Services;

public sealed class SecurityPromptService : ISecurityPromptService
{
    private readonly IDialogService _dialogService;

    public SecurityPromptService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public Task<bool> ConfirmDangerousActionAsync(string title, string message)
    {
        return Task.FromResult(_dialogService.Confirm(message, title));
    }
}
