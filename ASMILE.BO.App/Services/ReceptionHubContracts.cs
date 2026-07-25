using System.ComponentModel;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.App.Services;

public enum ReceptionWorkspaceKind
{
    LockedRooms,
    ArrivalsToday,
    DeparturesToday,
    StayProfiles,
    CurrentInHouse
}

/// <summary>Read-only permission seam for the Reception Hub.</summary>
public interface IReceptionHubPermissionReader : IDisposable
{
    void ResetForContextChange();
    bool HasContext { get; }
    bool CanView { get; }
}

/// <summary>Application adapter used only by Reception read-only workspaces.</summary>
public interface IReceptionUiService : IReceptionHubPermissionReader
{
    Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetLockedRoomsAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetArrivalsTodayAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetDeparturesTodayAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetStayProfilesAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetCurrentInHouseAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>>> GetPendingArrivalsAsync(string? keyword, CancellationToken cancellationToken);
    Task<Result<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>> CheckInAsync(Guid stayId, byte[] rowVersion, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>>> GetInHouseForCheckOutAsync(string? keyword, CancellationToken cancellationToken);
    Task<Result<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>> CheckOutAsync(Guid stayId, byte[] rowVersion, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>>> GetCurrentInHouseStaysAsync(int limit, CancellationToken cancellationToken);
    Task<Result<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>> ToggleDndAsync(Guid stayId, bool enabled, byte[]? rowVersion, CancellationToken cancellationToken);
    Task<Result<ASMILE.BO.Application.FrontOffice.Stay.HotelStayDto>> RequestWakeUpCallAsync(Guid stayId, DateTime requestedAt, string? notes, byte[]? rowVersion, CancellationToken cancellationToken);
}
public interface ICheckInStayModalService : IDisposable, INotifyPropertyChanged { ASMILE.BO.App.ViewModels.CheckInStayModalViewModel? ActiveModal { get; } bool IsOpen { get; } Task<bool> OpenAsync(string sourceCardId, CancellationToken cancellationToken = default); void Close(); }
public interface ICheckOutStayModalService : IDisposable, INotifyPropertyChanged { ASMILE.BO.App.ViewModels.CheckOutStayModalViewModel? ActiveModal { get; } bool IsOpen { get; } Task<bool> OpenAsync(string sourceCardId, CancellationToken cancellationToken = default); void Close(); }

/// <summary>Owns at most one Reception modal scope and disposes it when the modal closes.</summary>
public interface IReceptionWorkspaceModalService : IDisposable, INotifyPropertyChanged
{
    ReceptionReadOnlyWorkspaceViewModel? ActiveModal { get; }
    bool IsOpen { get; }
    string? FocusRestoreCardId { get; }
    Task<bool> OpenAsync(ReceptionWorkspaceKind kind, string sourceCardId, CancellationToken cancellationToken = default);
    void Close();
}
