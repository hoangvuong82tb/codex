using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ModuleHubViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _subtitle = string.Empty;

    public ObservableCollection<ModuleHubCard> Cards { get; } = [];
    public ICommand BackCommand { get; }
    public Func<Task>? ReturnToParentAsync { get; set; }

    public static ModuleHubViewModel Create(string title, string subtitle, params (string id, string label, string glyph, string color1, string color2)[] cards)
    {
        var vm = new ModuleHubViewModel { Title = title, Subtitle = subtitle };
        foreach (var (id, label, glyph, c1, c2) in cards)
        {
            var brush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(c1),
                (Color)ColorConverter.ConvertFromString(c2), 45);
            vm.Cards.Add(new ModuleHubCard
            {
                Id = id, Title = label, IconGlyph = glyph, IconColor = brush,
                Command = new RelayCommand(() => vm.ShowComingSoon(label))
            });
        }
        return vm;
    }

    public ModuleHubViewModel()
    {
        BackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    private async Task GoBackAsync()
    {
        if (ReturnToParentAsync is not null)
            await ReturnToParentAsync();
    }

    [ObservableProperty] private string? _comingSoonMessage;

    private void ShowComingSoon(string name) => ComingSoonMessage = $"{name} - Chức năng đang được phát triển.";

    partial void OnComingSoonMessageChanged(string? value)
    {
        if (value is not null)
        {
            _ = Task.Delay(3000).ContinueWith(_ => ComingSoonMessage = null,
                TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
