using ASMILE.BO.App.Models;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Services;

public interface IWindowManagerService
{
    ObservableCollection<AppWindowViewModel> OpenWindows { get; }
    AppWindowViewModel OpenOrActivate(AppModuleInfo module, Func<object> contentFactory);
    void Activate(AppWindowViewModel window);
    void ToggleFromTaskbar(AppWindowViewModel window);
    void Minimize(AppWindowViewModel window);
    void Restore(AppWindowViewModel window);
    void ToggleMaximize(AppWindowViewModel window);
    void MoveWindow(AppWindowViewModel window, double left, double top);
    void ResizeWindow(AppWindowViewModel window, double width, double height);
    void ResizeWindowEdges(AppWindowViewModel window, double deltaLeft, double deltaTop, double deltaRight, double deltaBottom);
    void UpdateWorkspaceBounds(double width, double height);
    void Close(AppWindowViewModel window);
}
