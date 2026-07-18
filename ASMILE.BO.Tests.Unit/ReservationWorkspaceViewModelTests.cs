using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReservationWorkspaceViewModelTests
{
    [Fact]
    public async Task UI_RES_U_001_loads_list_and_maps_dtos()
    {
        var service = new FakeReservationUiService();
        service.SearchResult = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([service.Current], 1, 1, 50));
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        await viewModel.LoadAsync();

        viewModel.Reservations.Should().ContainSingle().Which.ReservationNumber.Should().Be("RSV-1001");
        viewModel.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task UI_RES_U_008_successful_mutation_replaces_rowversion()
    {
        var service = new FakeReservationUiService();
        var updated = service.Current with { RowVersion = [9, 9, 9, 9, 9, 9, 9, 9], GuestName = "Guest updated" };
        service.GetResult = Result<ReservationDto>.Success(service.Current);
        service.UpdateResult = Result<ReservationDto>.Success(updated);
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        viewModel.SelectedReservation = service.Current;
        await WaitUntilAsync(() => viewModel.HasSelection);
        viewModel.GuestName = "Guest updated";
        await viewModel.SaveAsync();

        service.LastUpdateToken.Should().Equal([1, 2, 3, 4, 5, 6, 7, 8]);
        viewModel.SelectedReservation!.RowVersion.Should().Equal([9, 9, 9, 9, 9, 9, 9, 9]);
    }

    [Fact]
    public async Task UI_RES_U_009_concurrency_conflict_does_not_retry_and_offers_reload_state()
    {
        var service = new FakeReservationUiService();
        service.GetResult = Result<ReservationDto>.Success(service.Current);
        service.UpdateResult = Result<ReservationDto>.Failure(Error.Concurrency("RESERVATION_CONCURRENCY_CONFLICT", "stale"));
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        viewModel.SelectedReservation = service.Current;
        await WaitUntilAsync(() => viewModel.HasSelection);
        await viewModel.SaveAsync();

        service.UpdateCalls.Should().Be(1);
        viewModel.ErrorMessage.Should().Contain("vua duoc cap nhat");
        viewModel.InfoMessage.Should().Contain("tai lai");
    }

    [Fact]
    public async Task UI_RES_U_013_permission_disables_create_action()
    {
        var service = new FakeReservationUiService { CanCreateValue = false };
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        await viewModel.NewAsync();

        viewModel.IsCreateMode.Should().BeFalse();
        viewModel.ErrorMessage.Should().Contain("khong co quyen");
    }

    [Fact]
    public void UI_RES_U_016_maps_safe_error_messages()
    {
        ReservationErrorMessageMapper.ToMessage(Error.Business("RESERVATION_ROOM_CONFLICT", "database details"))
            .Should().Be("Phong khong con phu hop trong khoang ngay da chon.");
    }

    [Fact]
    public void UI_RES_U_019_resolves_workspace_through_validated_di_scope()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=(localdb)\\MSSQLLocalDB;Database=AsmileBoUiDiValidation;Trusted_Connection=True;TrustServerCertificate=True"
            }))
            .UseDefaultServiceProvider(options => { options.ValidateOnBuild = true; options.ValidateScopes = true; })
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                ASMILE.BO.Infrastructure.DependencyInjection.AddInfrastructure(services, context.Configuration);
                ASMILE.BO.App.DependencyInjection.AddPresentation(services);
            })
            .Build();
        using var scope = host.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<ReservationWorkspaceViewModel>().Should().NotBeNull();
    }

    [Fact]
    public async Task CTX_U_012_and_014_workspace_clears_old_property_data_and_cancels_requests()
    {
        var service = new FakeReservationUiService
        {
            SearchResult = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([new(Guid.NewGuid(), "RSV-A", Guid.NewGuid(), "Guest A", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21), 1, 0, null, null, ReservationStatus.Draft, "Direct", DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8])], 1, 1, 50))
        };
        var propertyHost = new FakePropertyContextHost();
        using var viewModel = new ReservationWorkspaceViewModel(service, propertyHost);
        await viewModel.LoadAsync();

        propertyHost.Raise(null, "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");
        await WaitUntilAsync(() => service.ResetCalls == 1 && !viewModel.HasData);

        viewModel.SelectedReservation.Should().BeNull();
        viewModel.HasPropertyContext.Should().BeFalse();
    }

    [Fact]
    public async Task CTX_U_013_workspace_reloads_using_new_property_context()
    {
        var propertyA = new FrontOfficePropertyOption(Guid.NewGuid(), "A", "Branch A", "A - Branch A", true);
        var propertyB = new FrontOfficePropertyOption(Guid.NewGuid(), "B", "Branch B", "B - Branch B", true);
        var service = new FakeReservationUiService
        {
            SearchResult = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([new(Guid.NewGuid(), "RSV-B", propertyB.PropertyId, "Guest B", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21), 1, 0, null, null, ReservationStatus.Draft, "Direct", DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8])], 1, 1, 50))
        };
        var propertyHost = new FakePropertyContextHost(propertyA);
        using var viewModel = new ReservationWorkspaceViewModel(service, propertyHost);

        propertyHost.Raise(propertyB);
        await WaitUntilAsync(() => viewModel.Reservations.SingleOrDefault()?.ReservationNumber == "RSV-B");

        service.ResetCalls.Should().Be(1);
        viewModel.Reservations.Single().PropertyId.Should().Be(propertyB.PropertyId);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        for (var attempt = 0; attempt < 20 && !predicate(); attempt++) await Task.Delay(10);
        predicate().Should().BeTrue();
    }

    private sealed class FakeReservationUiService : IReservationUiService
    {
        public ReservationDto Current { get; } = new(Guid.NewGuid(), "RSV-1001", Guid.NewGuid(), "Guest", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), 2, 0, null, null, ReservationStatus.Draft, "Direct", DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8]);
        public bool HasContext => true; public bool CanView => true; public bool CanCreate => CanCreateValue; public bool CanUpdate => true; public bool CanAssignRoom => true; public bool CanChangeStatus => true; public bool CanCancel => true;
        public bool CanCreateValue { get; set; } = true;
        public int UpdateCalls { get; private set; }
        public int ResetCalls { get; private set; }
        public byte[]? LastUpdateToken { get; private set; }
        public Result<PageResult<ReservationDto>> SearchResult { get; set; } = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([], 0, 1, 50));
        public Result<ReservationDto> GetResult { get; set; } = Result<ReservationDto>.Success(new(Guid.NewGuid(), "RSV-1001", Guid.NewGuid(), "Guest", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), 2, 0, null, null, ReservationStatus.Draft, "Direct", DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8]));
        public Result<ReservationDto> UpdateResult { get; set; } = Result<ReservationDto>.Success(new(Guid.NewGuid(), "RSV-1001", Guid.NewGuid(), "Guest", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), 2, 0, null, null, ReservationStatus.Draft, "Direct", DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8]));
        public Task<Result<PageResult<ReservationDto>>> SearchAsync(string? keyword, ReservationStatus? status, DateOnly? arrivalFrom, DateOnly? arrivalTo, int page, int pageSize, CancellationToken ct) => Task.FromResult(SearchResult);
        public Task<Result<ReservationDto>> GetAsync(Guid reservationId, CancellationToken ct) => Task.FromResult(GetResult);
        public Task<Result<PageResult<HotelRoomTypeDto>>> GetActiveRoomTypesAsync(CancellationToken ct) => Task.FromResult(Result<PageResult<HotelRoomTypeDto>>.Success(new([], 0, 1, 200)));
        public Task<Result<PageResult<HotelRoomDto>>> GetAssignableRoomsAsync(Guid roomTypeId, CancellationToken ct) => Task.FromResult(Result<PageResult<HotelRoomDto>>.Success(new([], 0, 1, 200)));
        public Task<Result<RoomConflictResult>> CheckRoomConflictAsync(ReservationDto current, Guid roomId, CancellationToken ct) => Task.FromResult(Result<RoomConflictResult>.Success(new(false, null)));
        public Task<Result<ReservationDto>> CreateAsync(string guestName, Guid? guestId, DateOnly arrival, DateOnly departure, int adults, int children, Guid? roomTypeId, string source, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> UpdateAsync(ReservationDto current, string guestName, Guid? roomTypeId, string? notes, CancellationToken ct) { UpdateCalls++; LastUpdateToken = current.RowVersion?.ToArray(); return Task.FromResult(UpdateResult); }
        public Task<Result<ReservationDto>> ChangeStayAsync(ReservationDto current, DateOnly arrival, DateOnly departure, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> ChangeGuestsAsync(ReservationDto current, int adults, int children, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> AssignAsync(ReservationDto current, Guid roomId, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> UnassignAsync(ReservationDto current, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> TentativeAsync(ReservationDto current, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> ConfirmAsync(ReservationDto current, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> CancelAsync(ReservationDto current, string reason, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> NoShowAsync(ReservationDto current, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public void ResetForContextChange() => ResetCalls++;
        public void Dispose() { }
    }

    private sealed class FakePropertyContextHost : IFrontOfficePropertyContextHost
    {
        private readonly FrontOfficePropertyOption _property;
        public FrontOfficePropertyOption? CurrentProperty { get; private set; }
        public IReadOnlyList<FrontOfficePropertyOption> AvailableProperties => [_property];
        public bool HasCurrentProperty => CurrentProperty is not null;
        public int Version { get; private set; } = 1;
        public string? ErrorCode { get; private set; }
        public event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged;
        public FakePropertyContextHost(FrontOfficePropertyOption? property = null)
        {
            _property = property ?? new FrontOfficePropertyOption(Guid.NewGuid(), "MAIN", "Main Branch", "MAIN - Main Branch", true);
            CurrentProperty = _property;
        }
        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Raise(FrontOfficePropertyOption? current, string? errorCode = null)
        {
            var previous = CurrentProperty;
            CurrentProperty = current;
            ErrorCode = errorCode;
            Version++;
            PropertyChanged?.Invoke(this, new FrontOfficePropertyContextChangedEventArgs(previous, current, Version, errorCode));
        }
        public void Dispose() { }
    }
}
