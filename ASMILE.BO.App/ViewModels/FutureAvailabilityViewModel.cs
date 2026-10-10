using System.Collections.ObjectModel;
using System.Globalization;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.Availability;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class FutureAvailabilityViewModel : ViewModelBase
{
    private readonly IAvailabilityEngine availability;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;

    public FutureAvailabilityViewModel(
        IAvailabilityEngine availability,
        IFrontOfficePropertyContextHost propertyContextHost)
    {
        this.availability = availability;
        this.propertyContextHost = propertyContextHost;
    }

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today;

    [ObservableProperty]
    private int days = 14;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasLoaded;

    public ObservableCollection<DailyInventoryRow> Rows { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasRows => Rows.Count > 0;
    public string PropertyDisplay => propertyContextHost.CurrentProperty?.DisplayName ?? "Chưa chọn chi nhánh";
    public string PeriodDisplay => Rows.Count == 0
        ? "Chưa có dữ liệu"
        : $"{Rows[0].Date:dd/MM/yyyy} - {Rows[^1].Date:dd/MM/yyyy}";
    public int TotalRooms => Rows.Count == 0 ? 0 : Rows.Max(x => x.TotalPhysicalRooms);
    public int PeakOccupancy => Rows.Count == 0 ? 0 : Rows.Max(x => x.OccupancyPercent);
    public int LowestRemaining => Rows.Count == 0 ? 0 : Rows.Min(x => x.Remaining);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        HasLoaded = false;
        Rows.Clear();
        NotifySummary();

        if (!propertyContextHost.HasCurrentProperty || propertyContextHost.CurrentProperty is null)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office trước khi xem khả năng phòng.";
            NotifyState();
            return;
        }

        if (Days is < 1 or > 90)
        {
            ErrorMessage = "Khoảng xem phải từ 1 đến 90 ngày.";
            NotifyState();
            return;
        }

        try
        {
            IsBusy = true;
            var from = DateOnly.FromDateTime(FromDate.Date);
            var forecast = await availability.CalculateForecastAsync(
                propertyContextHost.CurrentProperty.PropertyId,
                from,
                Days,
                cancellationToken);

            foreach (var item in forecast.DailyInventories)
                Rows.Add(new DailyInventoryRow(item));

            HasLoaded = true;
            NotifySummary();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải khả năng phòng: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyState();
        }
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    private void NotifySummary()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(PropertyDisplay));
        OnPropertyChanged(nameof(PeriodDisplay));
        OnPropertyChanged(nameof(TotalRooms));
        OnPropertyChanged(nameof(PeakOccupancy));
        OnPropertyChanged(nameof(LowestRemaining));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(PropertyDisplay));
    }
}

public sealed record DailyInventoryRow(
    DateOnly Date,
    int TotalPhysicalRooms,
    int OutOfOrder,
    int OutOfService,
    int Occupied,
    int Reserved,
    int ExpectedArrivals,
    int ExpectedDepartures,
    int Available,
    int OccupancyPercent,
    int Remaining,
    decimal Adr,
    decimal RevPar)
{
    public DailyInventoryRow(DailyInventory source)
        : this(source.Date, source.TotalPhysicalRooms, source.OutOfOrder, source.OutOfService,
            source.Occupied, source.Reserved, source.ExpectedArrivals, source.ExpectedDepartures,
            source.Available, source.OccupancyPercent, source.Remaining, source.Adr, source.RevPar)
    {
    }

    public string DateDisplay => Date.ToString("ddd, dd/MM", CultureInfo.GetCultureInfo("vi-VN"));
    public string OccupancyDisplay => $"{OccupancyPercent}%";
}
