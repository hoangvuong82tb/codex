using System.Collections.ObjectModel;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class FiscalPeriodManagementViewModel : ViewModelBase, IDisposable
{
    private readonly IAccountingPeriodService _periodService;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly CancellationTokenSource _disposeCts = new();
    private bool _suppressYearSelectionLoad;

    public ObservableCollection<FiscalYear> FiscalYears { get; } = new();
    public ObservableCollection<AccountingPeriod> Periods { get; } = new();

    [ObservableProperty] private FiscalYear? selectedFiscalYear;
    [ObservableProperty] private AccountingPeriod? selectedPeriod;
    [ObservableProperty] private int fiscalYearToCreate = DateTime.Today.Year;
    [ObservableProperty] private string statusMessage = "Sẵn sàng";
    [ObservableProperty] private string currentScope = string.Empty;

    public FiscalPeriodManagementViewModel(IAccountingPeriodService periodService, ICurrentCompanyContext companyContext)
    {
        _periodService = periodService;
        _companyContext = companyContext;
        _ = LoadAsync(_disposeCts.Token);
    }

    partial void OnSelectedFiscalYearChanged(FiscalYear? value)
    {
        if (value is not null && !_suppressYearSelectionLoad && !IsBusy)
        {
            _ = LoadPeriodsAsync(value.Year, _disposeCts.Token);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(_disposeCts.Token);
    }

    [RelayCommand]
    private async Task CreateFiscalYearAsync()
    {
        await RunAsync(async token =>
        {
            await _periodService.CreateFiscalYearPeriodsAsync(FiscalYearToCreate, "fiscal-period-ui", token);
            StatusMessage = $"Đã tạo năm tài chính {FiscalYearToCreate}.";
            await LoadCoreAsync(token);
        });
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPeriod))]
    private async Task LockPeriodAsync()
    {
        await RunAsync(async token =>
        {
            await _periodService.LockPeriodAsync(SelectedPeriod!.PeriodCode, _companyContext.BranchId, "fiscal-period-ui", "Khóa từ màn hình quản lý kỳ kế toán.", token);
            StatusMessage = $"Đã khóa kỳ {SelectedPeriod.PeriodCode}.";
            await LoadPeriodsAsync(SelectedPeriod.FromDate.Year, token);
        });
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPeriod))]
    private async Task UnlockPeriodAsync()
    {
        await RunAsync(async token =>
        {
            await _periodService.UnlockPeriodAsync(SelectedPeriod!.PeriodCode, _companyContext.BranchId, "fiscal-period-ui", "Mở khóa từ màn hình quản lý kỳ kế toán.", token);
            StatusMessage = $"Đã mở khóa kỳ {SelectedPeriod.PeriodCode}.";
            await LoadPeriodsAsync(SelectedPeriod.FromDate.Year, token);
        });
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPeriod))]
    private async Task ClosePeriodAsync()
    {
        await RunAsync(async token =>
        {
            await _periodService.ClosePeriodAsync(SelectedPeriod!.PeriodCode, "fiscal-period-ui", token);
            StatusMessage = $"Đã đóng kỳ {SelectedPeriod.PeriodCode}.";
            await LoadPeriodsAsync(SelectedPeriod.FromDate.Year, token);
        });
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPeriod))]
    private async Task ReopenPeriodAsync()
    {
        await RunAsync(async token =>
        {
            await _periodService.ReopenPeriodAsync(SelectedPeriod!.PeriodCode, "fiscal-period-ui", "Mở lại từ màn hình quản lý kỳ kế toán.", token);
            StatusMessage = $"Đã mở lại kỳ {SelectedPeriod.PeriodCode}.";
            await LoadPeriodsAsync(SelectedPeriod.FromDate.Year, token);
        });
    }

    private bool HasSelectedPeriod() => SelectedPeriod is not null && !IsBusy;

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        await RunAsync(LoadCoreAsync, cancellationToken);
    }

    private async Task LoadCoreAsync(CancellationToken cancellationToken)
    {
        await _companyContext.EnsureInitializedAsync(cancellationToken);
        CurrentScope = string.IsNullOrWhiteSpace(_companyContext.BranchName)
            ? _companyContext.CompanyName
            : $"{_companyContext.CompanyName} / {_companyContext.BranchName}";

        var years = await _periodService.GetFiscalYearsAsync(cancellationToken);
        FiscalYears.Clear();
        foreach (var year in years)
        {
            FiscalYears.Add(year);
        }

        _suppressYearSelectionLoad = true;
        SelectedFiscalYear = FiscalYears.FirstOrDefault(x => x.Year == FiscalYearToCreate) ?? FiscalYears.FirstOrDefault();
        _suppressYearSelectionLoad = false;

        if (SelectedFiscalYear is not null)
        {
            await LoadPeriodsAsync(SelectedFiscalYear.Year, cancellationToken);
        }
    }

    private async Task LoadPeriodsAsync(int year, CancellationToken cancellationToken)
    {
        var periods = await _periodService.GetPeriodsAsync(year, cancellationToken);
        Periods.Clear();
        foreach (var period in periods)
        {
            Periods.Add(period);
        }

        SelectedPeriod = Periods.FirstOrDefault();
        NotifyCommandStates();
    }

    private async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        NotifyCommandStates();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã hủy thao tác.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStates();
        }
    }

    private void NotifyCommandStates()
    {
        LockPeriodCommand.NotifyCanExecuteChanged();
        UnlockPeriodCommand.NotifyCanExecuteChanged();
        ClosePeriodCommand.NotifyCanExecuteChanged();
        ReopenPeriodCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }
}
