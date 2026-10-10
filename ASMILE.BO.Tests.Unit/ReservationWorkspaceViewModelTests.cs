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
    public async Task UI_RES_U_020_serializes_list_and_selected_detail_reads()
    {
        var service = new FakeReservationUiService { BlockSearch = true };
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        var listLoad = viewModel.LoadAsync();
        await service.SearchEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Selecting a row while the list query is still in flight used to start
        // a second operation on the same scoped EF DbContext.
        viewModel.SelectedReservation = service.Current;
        service.ReleaseSearch.TrySetResult(true);

        await listLoad;
        await WaitUntilAsync(() => service.GetCalls == 1);

        service.MaxConcurrentReads.Should().Be(1);
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
    public async Task UI_RES_U_021_edit_loads_and_saves_all_supported_reservation_details()
    {
        var roomTypeId = Guid.NewGuid();
        var detail = MakeDto(guestName: "Guest before") with
        {
            GuestPhone = "0900000000",
            GuestEmail = "before@example.com",
            RoomTypeId = roomTypeId,
            SpecialRequests = "Quiet room",
            InternalNotes = "Arrives late",
            Source = "Phone",
            ArrivalDate = new DateOnly(2026, 7, 21),
            DepartureDate = new DateOnly(2026, 7, 24),
            AdultCount = 2,
            ChildCount = 1
        };
        var service = new FakeReservationUiService
        {
            GetResult = Result<ReservationDto>.Success(detail),
            UpdateResult = Result<ReservationDto>.Success(detail with { GuestName = "Guest after" })
        };
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        (await viewModel.OpenForEditAsync(detail.ReservationId)).Should().BeTrue();
        viewModel.GuestName.Should().Be("Guest before");
        viewModel.GuestPhone.Should().Be("0900000000");
        viewModel.GuestEmail.Should().Be("before@example.com");
        viewModel.ArrivalDate.Should().Be(new DateTime(2026, 7, 21));
        viewModel.DepartureDate.Should().Be(new DateTime(2026, 7, 24));
        viewModel.Adults.Should().Be(2);
        viewModel.Children.Should().Be(1);
        viewModel.Source.Should().Be("Phone");
        viewModel.SpecialRequests.Should().Be("Quiet room");
        viewModel.InternalNotes.Should().Be("Arrives late");

        viewModel.GuestName = "Guest after";
        viewModel.GuestPhone = "0911111111";
        viewModel.GuestEmail = "after@example.com";
        viewModel.ArrivalDate = new DateTime(2026, 7, 22);
        viewModel.DepartureDate = new DateTime(2026, 7, 25);
        viewModel.Adults = 3;
        viewModel.Children = 0;
        viewModel.Source = "Direct";
        viewModel.SpecialRequests = "High floor";
        viewModel.InternalNotes = "Updated arrival note";

        await viewModel.SaveAsync();

        service.LastUpdateRequest.Should().NotBeNull();
        service.LastUpdateRequest!.GuestName.Should().Be("Guest after");
        service.LastUpdateRequest.Phone.Should().Be("0911111111");
        service.LastUpdateRequest.Email.Should().Be("after@example.com");
        service.LastUpdateRequest.RoomTypeId.Should().Be(roomTypeId);
        service.LastUpdateRequest.ArrivalDate.Should().Be(new DateOnly(2026, 7, 22));
        service.LastUpdateRequest.DepartureDate.Should().Be(new DateOnly(2026, 7, 25));
        service.LastUpdateRequest.Adults.Should().Be(3);
        service.LastUpdateRequest.Children.Should().Be(0);
        service.LastUpdateRequest.Source.Should().Be("Direct");
        service.LastUpdateRequest.SpecialRequests.Should().Be("High floor");
        service.LastUpdateRequest.InternalNotes.Should().Be("Updated arrival note");
    }

    [Fact]
    public async Task UI_RES_U_023_edit_modal_keeps_preloaded_reservation_instead_of_running_search()
    {
        var detail = MakeDto(guestName: "Guest ready to edit") with
        {
            GuestPhone = "0900000000",
            ArrivalDate = new DateOnly(2026, 7, 21),
            DepartureDate = new DateOnly(2026, 7, 24)
        };
        var service = new FakeReservationUiService
        {
            GetResult = Result<ReservationDto>.Success(detail)
        };
        using var workspace = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        (await workspace.OpenForEditAsync(detail.ReservationId)).Should().BeTrue();
        var modal = new ReservationModalWorkspaceViewModel(workspace, ReservationModalKind.Edit, () => { });

        await modal.InitializeAsync();

        service.SearchCalls.Should().Be(0);
        workspace.SelectedReservation.Should().Be(detail);
        workspace.GuestName.Should().Be("Guest ready to edit");
        workspace.GuestPhone.Should().Be("0900000000");
        modal.Title.Should().Be("Sửa đặt phòng");
        workspace.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task UI_RES_U_022_create_confirms_once_persists_guest_contact_and_ignores_double_submit()
    {
        var service = new FakeReservationUiService { BlockCreate = true };
        using var workspace = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());
        await workspace.NewAsync();
        workspace.GuestName = "New guest";
        workspace.GuestPhone = "0901234567";
        workspace.GuestEmail = "new.guest@example.test";
        var modal = new ReservationModalWorkspaceViewModel(workspace, ReservationModalKind.New, () => { });

        var firstSubmit = modal.CreateNewAsync();
        await service.CreateEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        (await modal.CreateNewAsync()).Should().BeFalse();
        service.CreateCalls.Should().Be(1);

        service.ReleaseCreate.TrySetResult(true);
        (await firstSubmit).Should().BeTrue();

        service.CreateCalls.Should().Be(1);
        service.TentativeCalls.Should().Be(0);
        service.ConfirmCalls.Should().Be(0);
        service.LastCreatedGuestName.Should().Be("New guest");
        service.LastCreatedGuestPhone.Should().Be("0901234567");
        service.LastCreatedGuestEmail.Should().Be("new.guest@example.test");
        service.LastCreateConfirmImmediately.Should().BeTrue();
        workspace.SelectedReservation!.Status.Should().Be(ReservationStatus.Confirmed);
        workspace.InfoMessage.Should().Contain("Tạo đặt phòng RSV-1001 hoàn tất");
    }

    [Fact]
    public async Task UI_RES_U_024_confirmation_updates_status_and_surfaces_success_feedback()
    {
        var service = new FakeReservationUiService();
        service.GetResult = Result<ReservationDto>.Success(service.Current);
        service.ConfirmResult = Result<ReservationDto>.Success(service.Current with { Status = ReservationStatus.Confirmed });
        using var workspace = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        workspace.SelectedReservation = service.Current;
        await WaitUntilAsync(() => workspace.CanConfirm);
        await workspace.ConfirmAsync();

        service.ConfirmCalls.Should().Be(1);
        service.LastConfirmedReservationId.Should().Be(service.Current.ReservationId);
        workspace.SelectedReservation!.Status.Should().Be(ReservationStatus.Confirmed);
        workspace.InfoMessage.Should().Contain("Đã xác nhận đặt phòng RSV-1001");
        workspace.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task UI_RES_U_025_confirmation_mode_only_lists_pending_statuses_and_explains_confirmed_selection()
    {
        var service = new FakeReservationUiService();
        var alreadyConfirmed = service.Current with { Status = ReservationStatus.Confirmed };
        service.GetResult = Result<ReservationDto>.Success(alreadyConfirmed);
        using var workspace = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        workspace.SelectedStatus = ReservationStatus.Confirmed;
        workspace.Mode = WorkspaceMode.Confirmation;

        workspace.StatusFilters.Should().Equal(ReservationStatus.Draft, ReservationStatus.Tentative);
        workspace.SelectedStatus.Should().Be(ReservationStatus.Draft);

        workspace.SelectedReservation = alreadyConfirmed;
        await WaitUntilAsync(() => workspace.SelectedReservationDetail?.Status == ReservationStatus.Confirmed);

        workspace.CanConfirm.Should().BeFalse();
        workspace.ConfirmationReadinessMessage.Should().Contain("đã được xác nhận");
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
        viewModel.InfoMessage.Should().Contain("tải lại");
    }

    [Fact]
    public async Task UI_RES_U_013_permission_disables_create_action()
    {
        var service = new FakeReservationUiService { CanCreateValue = false };
        using var viewModel = new ReservationWorkspaceViewModel(service, new FakePropertyContextHost());

        await viewModel.NewAsync();

        viewModel.IsCreateMode.Should().BeFalse();
        viewModel.ErrorMessage.Should().Contain("không có quyền");
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
            SearchResult = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([MakeDto(number: "RSV-A", guestName: "Guest A")], 1, 1, 50))
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
            SearchResult = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([MakeDto(number: "RSV-B", propertyId: propertyB.PropertyId, guestName: "Guest B")], 1, 1, 50))
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

    internal static ReservationDto MakeDto(Guid? reservationId = null, string number = "RSV-1001", Guid? propertyId = null, string guestName = "Guest") =>
        new(reservationId ?? Guid.NewGuid(), number, propertyId ?? Guid.NewGuid(), guestName, null, null, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), 2, 0, null, null, null, ReservationStatus.Draft, "Direct", null, null, null, false, null, null, null, null, GuaranteeType.None, null, 0, null, DepositStatus.NotRequired, null, null, DateTime.UtcNow, DateTime.UtcNow, [1, 2, 3, 4, 5, 6, 7, 8]);

    internal sealed class FakeReservationUiService : IReservationUiService
    {
        private readonly object _readSync = new();
        private int _activeReads;

        public ReservationDto Current { get; } = ReservationWorkspaceViewModelTests.MakeDto();
        public bool HasContext => true; public bool CanView => true; public bool CanCreate => CanCreateValue; public bool CanUpdate => true; public bool CanAssignRoom => true; public bool CanChangeStatus => true; public bool CanCancel => true;
        public bool CanCreateValue { get; set; } = true;
        public int UpdateCalls { get; private set; }
        public int CreateCalls { get; private set; }
        public int TentativeCalls { get; private set; }
        public int ConfirmCalls { get; private set; }
        public Guid? LastConfirmedReservationId { get; private set; }
        public int ResetCalls { get; private set; }
        public int GetCalls { get; private set; }
        public int SearchCalls { get; private set; }
        public int MaxConcurrentReads { get; private set; }
        public byte[]? LastUpdateToken { get; private set; }
        public ReservationEditRequest? LastUpdateRequest { get; private set; }
        public bool BlockSearch { get; set; }
        public bool BlockCreate { get; set; }
        public TaskCompletionSource<bool> SearchEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReleaseSearch { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> CreateEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReleaseCreate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? LastCreatedGuestName { get; private set; }
        public string? LastCreatedGuestPhone { get; private set; }
        public string? LastCreatedGuestEmail { get; private set; }
        public bool LastCreateConfirmImmediately { get; private set; }
        public Result<PageResult<ReservationDto>> SearchResult { get; set; } = Result<PageResult<ReservationDto>>.Success(new PageResult<ReservationDto>([], 0, 1, 50));
        public Result<ReservationDto> GetResult { get; set; } = Result<ReservationDto>.Success(ReservationWorkspaceViewModelTests.MakeDto());
        public Result<ReservationDto> UpdateResult { get; set; } = Result<ReservationDto>.Success(ReservationWorkspaceViewModelTests.MakeDto());
        public Result<ReservationDto>? ConfirmResult { get; set; }
        public async Task<Result<PageResult<ReservationDto>>> SearchAsync(string? keyword, ReservationStatus? status, DateOnly? arrivalFrom, DateOnly? arrivalTo, int page, int pageSize, CancellationToken ct, string? source = null)
        {
            SearchCalls++;
            BeginRead();
            try
            {
                if (BlockSearch)
                {
                    SearchEntered.TrySetResult(true);
                    await ReleaseSearch.Task;
                }

                return SearchResult;
            }
            finally { EndRead(); }
        }

        public async Task<Result<ReservationDto>> GetAsync(Guid reservationId, CancellationToken ct)
        {
            BeginRead();
            try { GetCalls++; return GetResult; }
            finally { EndRead(); }
        }
        public Task<Result<PageResult<HotelRoomTypeDto>>> GetActiveRoomTypesAsync(CancellationToken ct) => Task.FromResult(Result<PageResult<HotelRoomTypeDto>>.Success(new([], 0, 1, 200)));
        public Task<Result<PageResult<HotelRoomDto>>> GetAssignableRoomsAsync(Guid roomTypeId, CancellationToken ct) => Task.FromResult(Result<PageResult<HotelRoomDto>>.Success(new([], 0, 1, 200)));
        public Task<Result<RoomConflictResult>> CheckRoomConflictAsync(ReservationDto current, Guid roomId, CancellationToken ct) => Task.FromResult(Result<RoomConflictResult>.Success(new(false, null)));
        public async Task<Result<ReservationDto>> CreateAsync(string guestName, string? guestPhone, string? guestEmail, Guid? guestId, DateOnly arrival, DateOnly departure, int adults, int children, Guid? roomTypeId, string source, CancellationToken ct, bool confirmImmediately = false, Guid? ratePlanId = null, Guid? bookingSourceProfileId = null, Guid? bookingChannelProfileId = null, Guid? marketSegmentProfileId = null, Guid? companyProfileId = null, Guid? travelAgentProfileId = null, Guid? commissionRuleProfileId = null)
        {
            CreateCalls++;
            LastCreatedGuestName = guestName;
            LastCreatedGuestPhone = guestPhone;
            LastCreatedGuestEmail = guestEmail;
            LastCreateConfirmImmediately = confirmImmediately;
            if (BlockCreate)
            {
                CreateEntered.TrySetResult(true);
                await ReleaseCreate.Task;
            }

            return Result<ReservationDto>.Success(Current with
            {
                GuestName = guestName,
                GuestPhone = guestPhone,
                GuestEmail = guestEmail,
                ArrivalDate = arrival,
                DepartureDate = departure,
                AdultCount = adults,
                ChildCount = children,
                RoomTypeId = roomTypeId,
                Source = source,
                Status = confirmImmediately ? ReservationStatus.Confirmed : ReservationStatus.Draft
            });
        }
        public Task<Result<ReservationDto>> UpdateAsync(ReservationDto current, ReservationEditRequest request, CancellationToken ct) { UpdateCalls++; LastUpdateToken = current.RowVersion?.ToArray(); LastUpdateRequest = request; return Task.FromResult(UpdateResult); }
        public Task<Result<ReservationDto>> ChangeStayAsync(ReservationDto current, DateOnly arrival, DateOnly departure, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> ChangeGuestsAsync(ReservationDto current, int adults, int children, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> AssignAsync(ReservationDto current, Guid roomId, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> UnassignAsync(ReservationDto current, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> TentativeAsync(ReservationDto current, CancellationToken ct) { TentativeCalls++; return Task.FromResult(Result<ReservationDto>.Success(Current)); }
        public Task<Result<ReservationDto>> ConfirmAsync(ReservationDto current, CancellationToken ct)
        {
            ConfirmCalls++;
            LastConfirmedReservationId = current.ReservationId;
            return Task.FromResult(ConfirmResult ?? Result<ReservationDto>.Success(current with { Status = ReservationStatus.Confirmed }));
        }
        public Task<Result<ReservationDto>> RecordDepositAsync(ReservationDto current, decimal amount, DateOnly dueDate, CancellationToken ct)
            => Task.FromResult(Result<ReservationDto>.Success(Current with { DepositAmount = amount, DepositDueDate = dueDate, DepositStatus = DepositStatus.Pending }));
        public Task<Result<ReservationDto>> CancelAsync(ReservationDto current, string reason, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public Task<Result<ReservationDto>> NoShowAsync(ReservationDto current, CancellationToken ct) => Task.FromResult(Result<ReservationDto>.Success(Current));
        public void ResetForContextChange() => ResetCalls++;
        public int DisposeCalls { get; private set; }
        public void Dispose() { DisposeCalls++; }

        private void BeginRead()
        {
            lock (_readSync)
            {
                _activeReads++;
                MaxConcurrentReads = Math.Max(MaxConcurrentReads, _activeReads);
            }
        }

        private void EndRead()
        {
            lock (_readSync) _activeReads--;
        }
    }

    internal sealed class FakePropertyContextHost : IFrontOfficePropertyContextHost
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
