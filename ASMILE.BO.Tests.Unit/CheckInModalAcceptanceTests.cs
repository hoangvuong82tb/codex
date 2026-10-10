using System.ComponentModel;
using System.Runtime.CompilerServices;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class CheckInModalAcceptanceTests
{
    [Fact]
    public async Task CHECKIN_UI_U_003_to_012_modal_searches_pending_arrivals_and_prevents_double_submit()
    {
        var service = new FakeReceptionUiService { Pending = [Stay("STY-001", "RSV-001", "Guest One", "101"), Stay("STY-002", "RSV-002", "Guest Two", "102")] };
        var viewModel = new CheckInStayModalViewModel(service);
        await viewModel.InitializeAsync(() => { }, default);

        viewModel.Stays.Should().HaveCount(2);
        viewModel.SelectedStay.Should().NotBeNull();
        viewModel.SelectedStay!.RowVersion.Should().HaveCount(8);
        viewModel.SearchText = "RSV-002";
        await viewModel.SearchCommand.ExecuteAsync(null);
        service.LastKeyword.Should().Be("RSV-002");

        viewModel.IsSaving = true;
        viewModel.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task CHECKIN_UI_U_014_to_018_concurrency_is_safe_and_the_view_exposes_reload()
    {
        var service = new FakeReceptionUiService { Pending = [Stay("STY-001", "RSV-001", "Guest One", "101")], CheckInResult = Result<HotelStayDto>.Failure(Error.Concurrency("STAY_CONCURRENCY_CONFLICT", "stale")) };
        var viewModel = new CheckInStayModalViewModel(service);
        await viewModel.InitializeAsync(() => { }, default);

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        service.CheckInCalls.Should().Be(1);
        viewModel.ErrorMessage.Should().Be("Ho so vua duoc cap nhat boi nguoi dung khac. Vui long tai lai.");
        File.ReadAllText(Path.Combine(Root(), "ASMILE.BO.App", "Views", "CheckInStayModalView.xaml")).Should().Contain("ReloadCommand");
    }

    [Fact]
    public async Task CHECKIN_UI_U_020_failed_modal_initialization_disposes_scope_once()
    {
        var services = new ServiceCollection();
        var created = new List<FakeReceptionUiService>();
        services.AddScoped<IReceptionUiService>(_ => { var service = new FakeReceptionUiService { ThrowOnLoad = true }; created.Add(service); return service; });
        services.AddTransient<CheckInStayModalViewModel>();
        using var root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var modal = new CheckInStayModalService(root.GetRequiredService<IServiceScopeFactory>());

        await FluentActions.Invoking(() => modal.OpenAsync("check-in")).Should().ThrowAsync<InvalidOperationException>();

        modal.IsOpen.Should().BeFalse();
        created.Should().ContainSingle();
        created.Single().DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task CHECKIN_UI_U_001_to_002_and_021_checkin_card_opens_and_back_closes_checkin_modal()
    {
        var checkIn = new FakeCheckInModalService();
        using var hub = new ReceptionHubViewModel(new FakePermissions(), new FakePropertyContextHost(), new FakeWorkspaceModalService(), null!, null!, null, checkIn);

        var card = hub.Cards.Single(x => x.Id == "check-in");
        card.IsComingSoon.Should().BeFalse();
        card.IsEnabled.Should().BeTrue();
        await ((IAsyncRelayCommand)card.Command).ExecuteAsync(null);
        checkIn.OpenCalls.Should().Be(1);

        await hub.BackCommand.ExecuteAsync(null);
        checkIn.CloseCalls.Should().Be(1);
    }

    private static HotelStayDto Stay(string number, string reservation, string guest, string room) => new(Guid.NewGuid(), number, Guid.NewGuid(), Guid.NewGuid(), reservation, Guid.NewGuid(), guest, Guid.NewGuid(), room, Guid.NewGuid(), "STD", new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 20), null, null, 1, 0, HotelStayStatus.PendingArrival, "Test", 0, DateTime.UtcNow, DateTime.UtcNow, new byte[8]);

    private static string Root([CallerFilePath] string sourceFile = "")
    {
        foreach (var start in new[] { Path.GetDirectoryName(sourceFile) ?? string.Empty, Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(start)) continue;

            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ASMILE.BO.sln"))) return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate ASMILE.BO.sln from the test output or working directory.");
    }

    private sealed class FakeReceptionUiService : IReceptionUiService, IDisposable
    {
        public IReadOnlyList<HotelStayDto> Pending { get; set; } = [];
        public Result<HotelStayDto>? CheckInResult { get; set; }
        public string? LastKeyword { get; private set; }
        public int CheckInCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public bool ThrowOnLoad { get; set; }
        public bool HasContext => true;
        public bool CanView => true;
        public void ResetForContextChange() { }
        public void Dispose() => DisposeCalls++;
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetLockedRoomsAsync(CancellationToken cancellationToken) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetArrivalsTodayAsync(CancellationToken cancellationToken) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetDeparturesTodayAsync(CancellationToken cancellationToken) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetStayProfilesAsync(CancellationToken cancellationToken) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetCurrentInHouseAsync(CancellationToken cancellationToken) => Rows();
        public Task<Result<IReadOnlyList<HotelStayDto>>> GetPendingArrivalsAsync(string? keyword, CancellationToken cancellationToken)
        {
            if (ThrowOnLoad) throw new InvalidOperationException("load failed");
            LastKeyword = keyword;
            return Task.FromResult(Result<IReadOnlyList<HotelStayDto>>.Success(Pending));
        }
        public Task<Result<HotelStayDto>> CheckInAsync(Guid stayId, byte[] rowVersion, CancellationToken cancellationToken)
        {
            CheckInCalls++;
            return Task.FromResult(CheckInResult ?? Result<HotelStayDto>.Success(Pending.Single(x => x.HotelStayId == stayId) with { Status = HotelStayStatus.InHouse, ActualCheckInAtUtc = DateTime.UtcNow }));
        }
        public Task<Result<IReadOnlyList<HotelStayDto>>> GetInHouseForCheckOutAsync(string? keyword, CancellationToken cancellationToken)
        {
            if (ThrowOnLoad) throw new InvalidOperationException("load failed");
            LastKeyword = keyword;
            return Task.FromResult(Result<IReadOnlyList<HotelStayDto>>.Success(Pending));
        }
        public Task<Result<HotelStayDto>> CheckOutAsync(Guid stayId, byte[] rowVersion, CancellationToken cancellationToken)
        {
            return Task.FromResult(CheckInResult ?? Result<HotelStayDto>.Success(Pending.Single(x => x.HotelStayId == stayId) with { Status = HotelStayStatus.CheckedOut, ActualCheckOutAtUtc = DateTime.UtcNow }));
        }
        public Task<Result<IReadOnlyList<HotelStayDto>>> GetCurrentInHouseStaysAsync(int limit, CancellationToken ct) => Task.FromResult(Result<IReadOnlyList<HotelStayDto>>.Success(Pending));
        public Task<Result<HotelStayDto>> ToggleDndAsync(Guid stayId, bool enabled, byte[]? rowVersion, CancellationToken ct) => Task.FromResult(Result<HotelStayDto>.Success(Pending.First()));
        public Task<Result<HotelStayDto>> RequestWakeUpCallAsync(Guid stayId, DateTime requestedAt, string? notes, byte[]? rowVersion, CancellationToken ct) => Task.FromResult(Result<HotelStayDto>.Success(Pending.First()));
        private static Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> Rows() => Task.FromResult(Result<IReadOnlyList<ReceptionReadOnlyRow>>.Success([]));
    }

    private sealed class FakeCheckInModalService : ICheckInStayModalService
    {
        public CheckInStayModalViewModel? ActiveModal => null;
        public bool IsOpen { get; private set; }
        public int OpenCalls { get; private set; }
        public int CloseCalls { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public Task<bool> OpenAsync(string sourceCardId, CancellationToken cancellationToken = default) { OpenCalls++; IsOpen = true; PropertyChanged?.Invoke(this, new(nameof(IsOpen))); return Task.FromResult(true); }
        public void Close() { CloseCalls++; IsOpen = false; PropertyChanged?.Invoke(this, new(nameof(IsOpen))); }
        public void Dispose() => Close();
    }

    private sealed class FakePermissions : IReceptionHubPermissionReader
    {
        public bool HasContext => true;
        public bool CanView => true;
        public void ResetForContextChange() { }
        public void Dispose() { }
    }

    private sealed class FakeWorkspaceModalService : IReceptionWorkspaceModalService
    {
        public ReceptionReadOnlyWorkspaceViewModel? ActiveModal => null;
        public bool IsOpen => false;
        public string? FocusRestoreCardId => null;
        public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public Task<bool> OpenAsync(ReceptionWorkspaceKind kind, string sourceCardId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void Close() { }
        public void Dispose() { }
    }

    private sealed class FakePropertyContextHost : IFrontOfficePropertyContextHost
    {
        public FrontOfficePropertyOption? CurrentProperty { get; } = new(Guid.NewGuid(), "MAIN", "Main", "MAIN - Main", true);
        public IReadOnlyList<FrontOfficePropertyOption> AvailableProperties => [CurrentProperty!];
        public bool HasCurrentProperty => true;
        public int Version => 1;
        public string? ErrorCode => null;
        public event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged { add { } remove { } }
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }
}
