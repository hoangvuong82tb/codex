using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.Tests.Unit;

public sealed class GuestFeedbackViewModelTests
{
    [Fact]
    public async Task Load_does_not_fabricate_feedback_without_persistence_service()
    {
        var vm = new GuestFeedbackViewModel();

        await vm.InitializeAsync(CancellationToken.None);

        Assert.Empty(vm.Feedbacks);
        Assert.Equal(0, vm.TotalNew);
        Assert.Equal(0, vm.TotalResolved);
        Assert.Contains("không hiển thị dữ liệu mẫu", vm.InfoMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_is_blocked_without_property_context()
    {
        var vm = new GuestFeedbackViewModel();

        vm.CreateFeedbackCommand.Execute(null);

        Assert.Empty(vm.Feedbacks);
        Assert.Contains("chưa chọn cơ sở", vm.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
