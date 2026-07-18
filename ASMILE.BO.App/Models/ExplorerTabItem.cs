using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public partial class ExplorerTabItem : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string title = "This PC";

    [ObservableProperty]
    private string path = "This PC";

    [ObservableProperty]
    private bool isActive;
}

