using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class CheckOutModalAcceptanceTests
{
    private static HotelStayDto Stay(string id, string rsv, string guest, string room) => new(Guid.NewGuid(), $"STY-{id}", Guid.NewGuid(), Guid.NewGuid(), rsv, Guid.NewGuid(), guest, Guid.NewGuid(), room, Guid.NewGuid(), "STD", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(2)), null, null, 2, 0, HotelStayStatus.InHouse, "Test", 0, DateTime.UtcNow, DateTime.UtcNow, null);

    [Fact]
    public async Task CHECKOUT_MODAL_U_001_loads_inhouse_stays_on_initialize()
    {
        var inHouse = new[] { Stay("00000000-0000-0000-0000-000000000001", "RSV-001", "Guest One", "101"), Stay("00000000-0000-0000-0000-000000000002", "RSV-002", "Guest Two", "102") };
        var service = new FakeCheckOutReceptionService { InHouse = inHouse };
        var modal = new CheckOutStayModalViewModel(service);
        await modal.InitializeAsync(() => { }, CancellationToken.None);
        modal.Stays.Should().HaveCount(2);
        modal.SelectedStay.Should().NotBeNull();
        modal.HasSelection.Should().BeTrue();
        modal.CanConfirm.Should().BeTrue();
    }

    [Fact]
    public async Task CHECKOUT_MODAL_U_002_successful_checkout_shows_info_message()
    {
        var stay = Stay("00000000-0000-0000-0000-000000000001", "RSV-001", "Guest One", "101");
        var service = new FakeCheckOutReceptionService { InHouse = new[] { stay }, CheckOutResult = Result<HotelStayDto>.Success(stay with { Status = HotelStayStatus.CheckedOut, ActualCheckOutAtUtc = DateTime.UtcNow }) };
        var modal = new CheckOutStayModalViewModel(service);
        await modal.InitializeAsync(() => { }, CancellationToken.None);
        await modal.ConfirmCommand.ExecuteAsync(null);
        modal.InfoMessage.Should().Contain("tra phong thanh cong");
    }

    [Fact]
    public async Task CHECKOUT_MODAL_U_003_prevents_double_submit()
    {
        var tcs = new TaskCompletionSource<Result<HotelStayDto>>();
        var stay = Stay("00000000-0000-0000-0000-000000000001", "RSV-001", "Guest One", "101");
        var service = new FakeCheckOutReceptionService { InHouse = new[] { stay }, BlockingCheckOutResult = tcs.Task };
        var modal = new CheckOutStayModalViewModel(service);
        await modal.InitializeAsync(() => { }, CancellationToken.None);
        var confirm = modal.ConfirmCommand.ExecuteAsync(null);
        modal.IsSaving.Should().BeTrue();
        modal.CanConfirm.Should().BeFalse();
        tcs.SetResult(Result<HotelStayDto>.Success(stay with { Status = HotelStayStatus.CheckedOut, ActualCheckOutAtUtc = DateTime.UtcNow }));
        await confirm;
        modal.IsSaving.Should().BeFalse();
    }

    [Fact]
    public async Task CHECKOUT_MODAL_U_004_maps_concurrency_error()
    {
        var stay = Stay("00000000-0000-0000-0000-000000000001", "RSV-001", "Guest One", "101");
        var service = new FakeCheckOutReceptionService { InHouse = new[] { stay }, CheckOutResult = Result<HotelStayDto>.Failure(Error.Concurrency("STAY_CONCURRENCY_CONFLICT", "stale")) };
        var modal = new CheckOutStayModalViewModel(service);
        await modal.InitializeAsync(() => { }, CancellationToken.None);
        await modal.ConfirmCommand.ExecuteAsync(null);
        modal.ErrorMessage.Should().Contain("cap nhat");
    }

    [Fact]
    public async Task CHECKOUT_MODAL_U_005_maps_not_inhouse_error()
    {
        var stay = Stay("00000000-0000-0000-0000-000000000001", "RSV-001", "Guest One", "101");
        var service = new FakeCheckOutReceptionService { InHouse = new[] { stay }, CheckOutResult = Result<HotelStayDto>.Failure(Error.Business("STAY_NOT_IN_HOUSE", "not in-house")) };
        var modal = new CheckOutStayModalViewModel(service);
        await modal.InitializeAsync(() => { }, CancellationToken.None);
        await modal.ConfirmCommand.ExecuteAsync(null);
        modal.ErrorMessage.Should().Contain("dang luu tru");
    }

    [Fact]
    public async Task CHECKOUT_MODAL_U_006_close_invokes_callback()
    {
        var stay = Stay("00000000-0000-0000-0000-000000000001", "RSV-001", "Guest One", "101");
        var service = new FakeCheckOutReceptionService { InHouse = new[] { stay } };
        var modal = new CheckOutStayModalViewModel(service);
        var closed = false;
        await modal.InitializeAsync(() => closed = true, CancellationToken.None);
        modal.CloseCommand.Execute(null);
        closed.Should().BeTrue();
    }

    [Fact]
    public async Task CHECKOUT_MODAL_U_007_disabled_confirm_when_no_selection()
    {
        var service = new FakeCheckOutReceptionService { InHouse = Array.Empty<HotelStayDto>() };
        var modal = new CheckOutStayModalViewModel(service);
        await modal.InitializeAsync(() => { }, CancellationToken.None);
        modal.HasSelection.Should().BeFalse();
        modal.CanConfirm.Should().BeFalse();
    }

    private sealed class FakeCheckOutReceptionService : IReceptionUiService, IDisposable
    {
        public IReadOnlyList<HotelStayDto> InHouse { get; set; } = [];
        public Result<HotelStayDto>? CheckOutResult { get; set; }
        public Task<Result<HotelStayDto>>? BlockingCheckOutResult { get; set; }
        public bool ThrowOnLoad { get; set; }
        public bool HasContext => true;
        public bool CanView => true;
        public void ResetForContextChange() { }
        public void Dispose() { }
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetLockedRoomsAsync(CancellationToken ct) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetArrivalsTodayAsync(CancellationToken ct) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetDeparturesTodayAsync(CancellationToken ct) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetStayProfilesAsync(CancellationToken ct) => Rows();
        public Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> GetCurrentInHouseAsync(CancellationToken ct) => Rows();
        public Task<Result<IReadOnlyList<HotelStayDto>>> GetPendingArrivalsAsync(string? keyword, CancellationToken ct) => Task.FromResult(Result<IReadOnlyList<HotelStayDto>>.Success(InHouse));
        public Task<Result<HotelStayDto>> CheckInAsync(Guid stayId, byte[] rowVersion, CancellationToken ct) => Task.FromResult(Result<HotelStayDto>.Success(InHouse[0]));
        public Task<Result<IReadOnlyList<HotelStayDto>>> GetInHouseForCheckOutAsync(string? keyword, CancellationToken ct) { if (ThrowOnLoad) throw new InvalidOperationException("load failed"); return Task.FromResult(Result<IReadOnlyList<HotelStayDto>>.Success(InHouse)); }
        public async Task<Result<HotelStayDto>> CheckOutAsync(Guid stayId, byte[] rowVersion, CancellationToken ct) { if (BlockingCheckOutResult is not null) return await BlockingCheckOutResult; return CheckOutResult ?? Result<HotelStayDto>.Success(InHouse.Single(x => x.HotelStayId == stayId) with { Status = HotelStayStatus.CheckedOut, ActualCheckOutAtUtc = DateTime.UtcNow }); }
        public Task<Result<IReadOnlyList<HotelStayDto>>> GetCurrentInHouseStaysAsync(int limit, CancellationToken ct) => Task.FromResult(Result<IReadOnlyList<HotelStayDto>>.Success(InHouse));
        public Task<Result<HotelStayDto>> ToggleDndAsync(Guid stayId, bool enabled, byte[]? rowVersion, CancellationToken ct) => Task.FromResult(Result<HotelStayDto>.Success(InHouse.First()));
        public Task<Result<HotelStayDto>> RequestWakeUpCallAsync(Guid stayId, DateTime requestedAt, string? notes, byte[]? rowVersion, CancellationToken ct) => Task.FromResult(Result<HotelStayDto>.Success(InHouse.First()));
        private static Task<Result<IReadOnlyList<ReceptionReadOnlyRow>>> Rows() => Task.FromResult(Result<IReadOnlyList<ReceptionReadOnlyRow>>.Success([]));
    }
}
