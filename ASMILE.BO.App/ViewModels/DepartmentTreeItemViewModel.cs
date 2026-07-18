using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public partial class DepartmentTreeItemViewModel : ObservableObject
{
    public int Id { get; init; }
    public string DepartmentCode { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public int? ParentDepartmentId { get; init; }
    public int Level { get; init; } = 1;

    [ObservableProperty] private int peopleCount;
    [ObservableProperty] private bool isActive;

    public string StatusText => IsActive ? "Đang hoạt động" : "Ngưng hoạt động";
    public Thickness ItemMargin => new(Math.Max(Level - 1, 0) * 16, 0, 0, 0);
}
