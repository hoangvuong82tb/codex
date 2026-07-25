using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Infrastructure.FrontOffice.Concierge;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ConciergeTests
{
    [Fact] public void CON_001_create_service_request() { var r = new ServiceRequestService().CreateAsync(Guid.NewGuid(), Guid.NewGuid(), ServiceType.AirportTransfer, ServicePriority.Vip, "VIP pickup", DateTime.UtcNow.AddHours(2), default).Result; r.Status.Should().Be(ServiceStatus.Requested); r.IsVip.Should().BeTrue(); }
    [Fact] public void CON_002_assign_service() => new ServiceRequestService().AssignAsync(Guid.NewGuid(), Guid.NewGuid(), default).Result.Status.Should().Be(ServiceStatus.Assigned);
    [Fact] public void CON_003_complete_service() => new ServiceRequestService().TransitionAsync(Guid.NewGuid(), ServiceStatus.Completed, "Done", default).Result.Status.Should().Be(ServiceStatus.Completed);
    [Fact] public void CON_004_sla_report_has_on_time_pct() => new ServiceRequestService().GetSlaReportAsync(Guid.NewGuid(), default).Result[0].OnTimePct.Should().BeGreaterThan(0);
    [Fact] public void CON_005_schedule_wakeup() => new WakeUpCallService().ScheduleAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(8), "Early flight", default).Result.Status.Should().Be(WakeUpCallStatus.Scheduled);
    [Fact] public void CON_006_complete_wakeup() => new WakeUpCallService().CompleteAsync(Guid.NewGuid(), WakeUpCallStatus.Completed, default).Result.Status.Should().Be(WakeUpCallStatus.Completed);
    [Fact] public void CON_007_retry_wakeup() => new WakeUpCallService().RetryAsync(Guid.NewGuid(), default).Result.RetryCount.Should().Be(2);
    [Fact] public void CON_008_store_luggage() { var r = new LuggageService().StoreAsync(new LuggageItemDto(Guid.NewGuid(), null, Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", 3, "Bags", null, LuggageStatus.InTransit, DateTime.UtcNow, null, null, null, null), default).Result; r.Status.Should().Be(LuggageStatus.Stored); r.TagNumber.Should().NotBeNull(); }
    [Fact] public void CON_009_deliver_luggage() => new LuggageService().DeliverAsync(Guid.NewGuid(), Guid.NewGuid(), default).Result.Status.Should().Be(LuggageStatus.Delivered);
    [Fact] public void CON_010_book_transport() => new TransportationService().BookAsync(new TransportationDto(Guid.NewGuid(), null, TransportType.AirportPickup, Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", null, null, null, DateTime.UtcNow.AddHours(4), "Airport", "Hotel", "VN456", 1, ServiceStatus.Requested, null, null), default).Result.BookingNumber.Should().NotBeNull();
    [Fact] public void CON_011_concierge_dashboard() => new ConciergeDashboardService().GetDashboardAsync(Guid.NewGuid(), default).Result.OpenRequests.Should().BeGreaterThan(0);
    [Fact] public void CON_012_events_unique() => typeof(ConciergeEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems();
}
