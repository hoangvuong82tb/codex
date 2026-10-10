namespace ASMILE.BO.App.Views;

using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.App.ViewModels;

public partial class WorkforceWindow
{
    public WorkforceWindow()
    {
        InitializeComponent();
        DataContext = new WorkforceViewModel();
    }

    public WorkforceWindow(
        IWorkforceQueryService workforceQueryService,
        IWorkforceCommandService workforceCommandService,
        IHousekeepingBoardService housekeepingBoardService,
        IHousekeepingWorkflowService housekeepingWorkflowService,
        IFrontOfficeExecutionContextAccessor executionContext)
    {
        InitializeComponent();
        DataContext = new WorkforceViewModel(
            workforceQueryService,
            workforceCommandService,
            housekeepingBoardService,
            housekeepingWorkflowService,
            executionContext);
    }
}
