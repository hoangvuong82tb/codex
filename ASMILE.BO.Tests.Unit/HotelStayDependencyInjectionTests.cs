using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelStayDependencyInjectionTests
{
    [Fact]
    public void STAY_DI_U_001_repository_number_generator_handlers_and_persistence_resolve_as_scoped_services()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=(localdb)\\MSSQLLocalDB;Database=ASMILE_BO_DI_VALIDATION;Integrated Security=True;TrustServerCertificate=True;"
        }).Build();
        var services = new ServiceCollection();
        ASMILE.BO.Application.DependencyInjection.AddApplication(services);
        services.AddInfrastructure(configuration);
        ASMILE.BO.App.DependencyInjection.AddPresentation(services);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHotelStayRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IHotelStayOccupantRepository>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IHotelStayOccupancyReader>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IHotelStayNumberGenerator>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<HotelStayApplicationService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<PrepareStayFromReservationCommandHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<CheckInHotelStayCommandHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<GetStayByIdQueryHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IFrontOfficePersistenceSession>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IReceptionUiService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<CheckInStayModalViewModel>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ICheckInStayModalService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<RateEngineViewModel>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ItemsViewModel>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<MinibarViewModel>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ReservationWorkspaceViewModel>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<TelephoneCenterViewModel>().Should().NotBeNull();
    }
}
