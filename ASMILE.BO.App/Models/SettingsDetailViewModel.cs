using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Models;

public sealed class SettingsDetailViewModel
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Breadcrumb { get; init; }
    public ObservableCollection<SettingCardViewModel> Cards { get; } = [];
}
