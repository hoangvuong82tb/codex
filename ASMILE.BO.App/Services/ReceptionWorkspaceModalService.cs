using ASMILE.BO.App.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Services;

public sealed partial class ReceptionWorkspaceModalService : ObservableObject, IReceptionWorkspaceModalService
{
    private readonly IServiceScopeFactory scopeFactory;
    private IServiceScope? modalScope;
    private bool isOpening;
    private bool isClosing;
    private bool isDisposed;
    private string? activeSourceCardId;

    [ObservableProperty] private ReceptionReadOnlyWorkspaceViewModel? activeModal;
    [ObservableProperty] private string? focusRestoreCardId;

    public ReceptionWorkspaceModalService(IServiceScopeFactory scopeFactory) => this.scopeFactory = scopeFactory;
    public bool IsOpen => ActiveModal is not null;

    public async Task<bool> OpenAsync(ReceptionWorkspaceKind kind, string sourceCardId, CancellationToken cancellationToken = default)
    {
        if (isDisposed || isOpening || isClosing || ActiveModal is not null) return false;

        isOpening = true;
        activeSourceCardId = sourceCardId;
        modalScope = scopeFactory.CreateScope();
        try
        {
            var modal = modalScope.ServiceProvider.GetRequiredService<ReceptionReadOnlyWorkspaceViewModel>();
            ActiveModal = modal;
            OnPropertyChanged(nameof(IsOpen));
            await modal.InitializeAsync(kind, Close, cancellationToken);
            if (!ReferenceEquals(ActiveModal, modal)) return false;
            FocusRestoreCardId = null;
            return true;
        }
        catch
        {
            DisposeActiveModal();
            throw;
        }
        finally
        {
            isOpening = false;
        }
    }

    public void Close()
    {
        if (isDisposed || isClosing || ActiveModal is null) return;
        isClosing = true;
        try
        {
            FocusRestoreCardId = activeSourceCardId;
            DisposeActiveModal();
        }
        finally
        {
            isClosing = false;
        }
    }

    private void DisposeActiveModal()
    {
        ActiveModal = null;
        var scope = modalScope;
        modalScope = null;
        OnPropertyChanged(nameof(IsOpen));
        scope?.Dispose();
    }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        DisposeActiveModal();
        GC.SuppressFinalize(this);
    }
}
