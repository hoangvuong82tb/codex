using ASMILE.BO.App.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace ASMILE.BO.App.Services;

public enum ReservationModalKind
{
    New,
    Search,
    Edit,
    AssignRoom,
    Confirm,
    Cancel,
    Status
}

/// <summary>Small read-only permission seam used by the Hub; it contains no reservation operations.</summary>
public interface IReservationHubPermissionReader : IDisposable
{
    bool HasContext { get; }
    bool CanView { get; }
    bool CanCreate { get; }
    bool CanUpdate { get; }
    bool CanAssignRoom { get; }
    bool CanChangeStatus { get; }
    bool CanCancel { get; }
}

/// <summary>
/// Owns one modal scope at a time. The modal content is resolved in that scope and is disposed when closed.
/// </summary>
public interface IReservationWorkspaceModalService : IDisposable
{
    ReservationModalWorkspaceViewModel? ActiveModal { get; }
    bool IsOpen { get; }
    string? FocusRestoreCardId { get; }
    event PropertyChangedEventHandler? PropertyChanged;
    Task<bool> OpenAsync(ReservationModalKind kind, string sourceCardId, CancellationToken cancellationToken = default);
    void Close();
}
