using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReservationWorkspaceModalServiceTests
{
    [Fact]
    public async Task MODAL_LIFE_U_001_to_004_open_creates_one_scope_and_close_is_idempotent()
    {
        using var fixture = new ModalFixture();

        (await fixture.Modal.OpenAsync(ReservationModalKind.New, "new")).Should().BeTrue();
        fixture.Modal.IsOpen.Should().BeTrue();

        fixture.Modal.Close();
        fixture.Modal.Close();

        fixture.Modal.IsOpen.Should().BeFalse();
        fixture.Services.Should().ContainSingle();
        fixture.Services.Single().DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task MODAL_LIFE_U_005_to_008_double_close_and_reopen_are_safe()
    {
        using var fixture = new ModalFixture();

        (await fixture.Modal.OpenAsync(ReservationModalKind.New, "new")).Should().BeTrue();
        fixture.Modal.Close();
        fixture.Modal.Close();
        (await fixture.Modal.OpenAsync(ReservationModalKind.Search, "search")).Should().BeTrue();

        fixture.Modal.Close();
        fixture.Services.Should().HaveCount(2);
        fixture.Services.Should().OnlyContain(service => service.DisposeCalls == 1);
    }

    [Fact]
    public async Task MODAL_LIFE_U_009_open_close_twenty_times_has_one_disposal_path_per_scope()
    {
        using var fixture = new ModalFixture();

        for (var attempt = 0; attempt < 20; attempt++)
        {
            (await fixture.Modal.OpenAsync(ReservationModalKind.New, "new")).Should().BeTrue();
            fixture.Modal.Close();
        }

        fixture.Services.Should().HaveCount(20);
        fixture.Services.Should().OnlyContain(service => service.DisposeCalls == 1);
    }

    [Fact]
    public async Task MODAL_LIFE_U_010_to_012_dispose_closes_active_scope_and_restores_source_card()
    {
        using var fixture = new ModalFixture();

        (await fixture.Modal.OpenAsync(ReservationModalKind.New, "new")).Should().BeTrue();
        fixture.Modal.Close();
        fixture.Modal.FocusRestoreCardId.Should().Be("new");

        (await fixture.Modal.OpenAsync(ReservationModalKind.Search, "search")).Should().BeTrue();
        fixture.Modal.Dispose();
        fixture.Modal.Dispose();

        fixture.Modal.IsOpen.Should().BeFalse();
        fixture.Services.Should().HaveCount(2);
        fixture.Services.Should().OnlyContain(service => service.DisposeCalls == 1);
    }

    private sealed class ModalFixture : IDisposable
    {
        private readonly ServiceProvider root;

        public ModalFixture()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IFrontOfficePropertyContextHost>(new ReservationWorkspaceViewModelTests.FakePropertyContextHost());
            services.AddTransient<IReservationUiService>(_ =>
            {
                var service = new ReservationWorkspaceViewModelTests.FakeReservationUiService();
                Services.Add(service);
                return service;
            });
            services.AddTransient<ReservationWorkspaceViewModel>();
            root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            Modal = new ReservationWorkspaceModalService(root.GetRequiredService<IServiceScopeFactory>());
        }

        public List<ReservationWorkspaceViewModelTests.FakeReservationUiService> Services { get; } = [];
        public ReservationWorkspaceModalService Modal { get; }

        public void Dispose()
        {
            Modal.Dispose();
            root.Dispose();
        }
    }
}
