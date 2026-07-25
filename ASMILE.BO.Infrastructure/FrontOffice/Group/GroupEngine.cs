using ASMILE.BO.Application.FrontOffice.Group;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class RoomBlockService : IRoomBlockService
{
    public Task<RoomBlockDto> CreateBlockAsync(Guid groupId, Guid roomTypeId, int rooms, DateOnly arrival, DateOnly departure, DateOnly? cutoff, decimal? negotiatedRate, CancellationToken ct) => Task.FromResult(new RoomBlockDto(Guid.NewGuid(), groupId, "GRP-001", "Tech Conference", roomTypeId, "DLX", rooms, rooms, 0, rooms, arrival, departure, cutoff, cutoff is not null ? CutoffType.Automatic : CutoffType.None, BlockStatus.Confirmed, negotiatedRate, DateTime.UtcNow));
    public Task<RoomBlockDto> ModifyBlockAsync(Guid blockId, int? additionalRooms, DateOnly? newCutoff, CancellationToken ct) => Task.FromResult(new RoomBlockDto(blockId, Guid.NewGuid(), "GRP-001", "Tech Conference", Guid.NewGuid(), "DLX", 30, 35, 10, 25, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), newCutoff, CutoffType.Automatic, BlockStatus.Confirmed, 1200000, DateTime.UtcNow));
    public Task<RoomBlockDto> ReleaseRoomsAsync(Guid blockId, int count, CancellationToken ct) => Task.FromResult(new RoomBlockDto(blockId, Guid.NewGuid(), "GRP-001", "Tech Conference", Guid.NewGuid(), "DLX", 30, 30, 15, 15, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), CutoffType.Automatic, BlockStatus.Confirmed, 1200000, DateTime.UtcNow));
    public Task<PickupAnalysisDto> GetPickupAsync(Guid blockId, DateOnly? asOf, CancellationToken ct) => Task.FromResult(new PickupAnalysisDto(blockId, DateOnly.FromDateTime(DateTime.UtcNow), 40, 28, 70m, 12, 7, "OnTrack"));
    public Task<RoomBlockDashboardDto> GetBlockDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new RoomBlockDashboardDto(5, 4, 120, 85, 71m, 1, 0, [], []));
    public Task<IReadOnlyList<RoomBlockDto>> GetActiveBlocksAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RoomBlockDto>>([new RoomBlockDto(Guid.NewGuid(), Guid.NewGuid(), "GRP-001", "Conference", Guid.NewGuid(), "DLX", 40, 40, 28, 12, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), CutoffType.Automatic, BlockStatus.Confirmed, 1200000, DateTime.UtcNow)]);
    public Task<IReadOnlyList<PickupAnalysisDto>> GetPickupTrendsAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<PickupAnalysisDto>>([new PickupAnalysisDto(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 40, 28, 70m, 12, 7, "OnTrack")]);
}

public sealed class RoomingListService : IRoomingListService
{
    public Task<IReadOnlyList<RoomingListItemDto>> GetRoomingListAsync(Guid groupId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RoomingListItemDto>>([new RoomingListItemDto(Guid.NewGuid(), Guid.NewGuid(), "101", "DLX", Guid.NewGuid(), "John Smith", 2, 0, false, false, false, null, null, null)]);
    public Task<RoomingListImportResult> ImportAsync(Guid groupId, IReadOnlyList<RoomingListItemDto> items, CancellationToken ct) => Task.FromResult(new RoomingListImportResult(items.Count, items.Count, 0, 0, []));
    public Task<IReadOnlyList<RoomingListItemDto>> ExportAsync(Guid groupId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RoomingListItemDto>>([new RoomingListItemDto(Guid.NewGuid(), Guid.NewGuid(), "101", "DLX", Guid.NewGuid(), "John Smith", 2, 0, false, false, false, null, null, null)]);
    public Task AssignGuestAsync(Guid assignmentId, Guid guestId, string guestName, CancellationToken ct) => Task.CompletedTask;
    public Task MarkArrivalAsync(Guid assignmentId, DateTime checkInAt, CancellationToken ct) => Task.CompletedTask;
    public Task MarkDepartureAsync(Guid assignmentId, DateTime checkOutAt, CancellationToken ct) => Task.CompletedTask;
}

public sealed class GroupBillingService : IGroupBillingService
{
    public Task<GroupBillingSetup> ConfigureBillingAsync(GroupBillingSetup setup, CancellationToken ct) => Task.FromResult(setup);
    public Task<BillingSummaryDto> GetBillingSummaryAsync(Guid groupId, CancellationToken ct) => Task.FromResult(new BillingSummaryDto(groupId, "Conference 2026", 85000000, 50000000, 20000000, 15000000, 1, 25, false));
    public Task<GroupRevenueDashboardDto> GetRevenueDashboardAsync(Guid groupId, CancellationToken ct) => Task.FromResult(new GroupRevenueDashboardDto(groupId, "Conference 2026", 65000000, 15000000, 5000000, 85000000, 3100000, 2800000, 92m));
}

public sealed class GroupDashboardService : IGroupDashboardService
{
    public Task<GroupDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new GroupDashboardDto(5, 120, 85, 71m, 2, 1, 250000000, 45000000, [], []));
}
