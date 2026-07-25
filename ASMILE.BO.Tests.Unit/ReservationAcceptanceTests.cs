using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReservationAcceptanceTests
{
    [Fact]
    public async Task RES_AUTH_U_002_create_without_permission_is_rejected_before_number_allocation()
    {
        var number = new CountingNumberGenerator(); var service = CreateService(new TestAuthorization(false), number);
        using var _ = Context().accessor.BeginScope(Context().context);
        var result = await service.CreateAsync(new CreateReservationCommand(Context().context.Property.PropertyId.Value, Guid.NewGuid(), "Guest", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21), 1, 0, null, null, "Direct", null, null, null, false), default);
        result.Error!.Code.Should().Be("RESERVATION_PERMISSION_DENIED"); number.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("details")]
    [InlineData("stay")]
    [InlineData("guests")]
    [InlineData("assign")]
    [InlineData("unassign")]
    [InlineData("tentative")]
    [InlineData("confirm")]
    [InlineData("cancel")]
    [InlineData("noshow")]
    public async Task RES_CON_U_mutation_without_rowversion_is_rejected(string operation)
    {
        var setup = Context(); var service = CreateService(new TestAuthorization(true), new CountingNumberGenerator(), setup.accessor);
        using var _ = setup.accessor.BeginScope(setup.context);
        var id = Guid.NewGuid(); Task<Result<ReservationDto>> call = operation switch
        {
            "details" => service.UpdateDetailsAsync(new UpdateReservationDetailsCommand(setup.context.Property.PropertyId.Value, id, "Guest", null, null, null, null, null, null, []), default),
            "stay" => service.ChangeStayAsync(new ChangeReservationStayCommand(setup.context.Property.PropertyId.Value, id, new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 21), []), default),
            "guests" => service.ChangeGuestsAsync(new ChangeReservationGuestCountCommand(setup.context.Property.PropertyId.Value, id, 1, 0, []), default),
            "assign" => service.AssignAsync(new AssignReservationRoomCommand(setup.context.Property.PropertyId.Value, id, Guid.NewGuid(), []), default),
            "unassign" => service.UnassignAsync(new UnassignReservationRoomCommand(setup.context.Property.PropertyId.Value, id, []), default),
            "tentative" => service.TentativeAsync(new MarkReservationTentativeCommand(setup.context.Property.PropertyId.Value, id, []), default),
            "confirm" => service.ConfirmAsync(new ConfirmReservationCommand(setup.context.Property.PropertyId.Value, id, []), default),
            "cancel" => service.CancelAsync(new CancelReservationCommand(setup.context.Property.PropertyId.Value, id, "reason", []), default),
            _ => service.NoShowAsync(new MarkReservationNoShowCommand(setup.context.Property.PropertyId.Value, id, []), default)
        };
        (await call).Error!.Code.Should().Be("RESERVATION_ROWVERSION_REQUIRED");
    }

    [Fact]
    public async Task RES_AUTH_U_001_view_query_without_permission_is_rejected_before_repository()
    {
        var setup = Context(); var repository = new TestRepository(); var handler = new GetReservationByIdQueryHandler(repository, setup.accessor, new TestAuthorization(false));
        using var _ = setup.accessor.BeginScope(setup.context);
        var result = await handler.HandleAsync(new GetReservationByIdQuery(setup.context.Property.PropertyId.Value, Guid.NewGuid()), default);
        result.Error!.Code.Should().Be("RESERVATION_PERMISSION_DENIED"); repository.GetDtoCalls.Should().Be(0);
    }

    private static ReservationApplicationService CreateService(IAuthorizationService authorization, CountingNumberGenerator number, FrontOfficeExecutionContextAccessor? accessor = null)
    {
        accessor ??= new FrontOfficeExecutionContextAccessor();
        return new ReservationApplicationService(new TestRepository(), number, new TestReferences(), new TestPersistence(), accessor, authorization, new FixedClock(), new TestOverbooking());
    }
    private static (FrontOfficeExecutionContext context, FrontOfficeExecutionContextAccessor accessor) Context()
    { var property = PropertyId.New(); return (new(new(ActorId.New(), new HashSet<string>(), "test", "test", true), new(property, "Asia/Ho_Chi_Minh", "VND"), new(property, new DateOnly(2026, 7, 18), "test"), new(CorrelationId.New(), null, CommandId.New(), null)), new()); }
    private sealed class FixedClock : IClock { public DateTime UtcNow => new(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc); }
    private sealed class CountingNumberGenerator : IReservationNumberGenerator { public int Calls { get; private set; } public Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult("RSV-TEST"); } }
    private sealed class TestAuthorization(bool allow) : IAuthorizationService { public bool HasPermission(string permissionCode) => allow; public void Demand(string permissionCode) { } public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(allow); public bool HasPermission(Permission permission) => allow; public void Demand(Permission permission) { } public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(allow); }
    private sealed class TestPersistence : IFrontOfficePersistenceSession { public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) { } public void StageAudit(FrontOfficeAuditIntent intent) { } public Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result<int>.Success(1)); }
    private sealed class TestOverbooking : IOverbookingChecker { public Task<bool> IsOverbookingLimitReachedAsync(Guid propertyId, Guid roomTypeId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken ct) => Task.FromResult(false); }
    private sealed class TestReferences : IHotelReservationReferenceReader { public Task<HotelRoomTypeReference?> GetRoomTypeReferenceAsync(Guid propertyId, Guid roomTypeId, CancellationToken ct = default) => Task.FromResult<HotelRoomTypeReference?>(null); public Task<HotelRoomReference?> GetRoomReferenceAsync(Guid propertyId, Guid roomId, CancellationToken ct = default) => Task.FromResult<HotelRoomReference?>(null); public Task<bool> HotelRoomMatchesRoomTypeAsync(Guid propertyId, Guid roomId, Guid roomTypeId, CancellationToken ct = default) => Task.FromResult(false); }
    private sealed class TestRepository : IReservationRepository
    {
        public int GetDtoCalls { get; private set; }
        public Task AddAsync(Reservation reservation, ReservationHistory history, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Reservation?> GetAsync(Guid id, Guid propertyId, CancellationToken ct = default) => Task.FromResult<Reservation?>(null);
        public Task AppendHistoryAsync(ReservationHistory history, CancellationToken ct = default) => Task.CompletedTask;
        public void ApplyExpectedRowVersion(Reservation reservation, byte[] expectedRowVersion) { }
        public Task<ReservationDto?> GetDtoAsync(Guid id, Guid propertyId, CancellationToken ct = default) { GetDtoCalls++; return Task.FromResult<ReservationDto?>(null); }
        public Task<ReservationDto?> GetByNumberAsync(Guid propertyId, string reservationNumber, CancellationToken ct = default) => Task.FromResult<ReservationDto?>(null);
        public Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, string? keyword, ReservationStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken ct = default) => Task.FromResult(new PageResult<ReservationDto>([], 0, page.Page, page.PageSize));
        public Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, ReservationSearchFilter filter, PageRequest page, string? sort, CancellationToken ct = default) => Task.FromResult(new PageResult<ReservationDto>([], 0, page.Page, page.PageSize));
        public Task<PageResult<ReservationHistoryDto>> GetHistoryAsync(Guid reservationId, PageRequest page, CancellationToken ct = default) => Task.FromResult(new PageResult<ReservationHistoryDto>([], 0, page.Page, page.PageSize));
        public Task<RoomConflictResult> CheckAsync(Guid propertyId, Guid roomId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken ct = default) => Task.FromResult(new RoomConflictResult(false, null));
    }
}
