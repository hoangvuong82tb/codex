using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Threading;

namespace ASMILE.BO.App.ViewModels;

public partial class DepartmentPeoplePageViewModel : ViewModelBase
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+()\-\s]{8,15}$", RegexOptions.Compiled);
    private readonly IDepartmentAppService _departmentService;
    private readonly IPersonAppService _personService;
    private readonly IDepartmentPeopleImportExportService _importExportService;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;

    private readonly TimeSpan _searchDebounce = TimeSpan.FromMilliseconds(260);
    private CancellationTokenSource? _searchDebounceCts;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private bool _suppressSelectedDepartmentChanged;

    public ObservableCollection<DepartmentTreeItemViewModel> Departments { get; } = [];
    public ObservableCollection<PersonDto> People { get; } = [];
    public TablePagerViewModel<PersonDto> PeoplePager { get; } = new() { ItemName = "nhân sự" };

    public ObservableCollection<string> StatusFilters { get; } =
    [
        "Tất cả",
        "Đang làm",
        "Đã nghỉ"
    ];

    [ObservableProperty] private DepartmentTreeItemViewModel? selectedDepartment;
    [ObservableProperty] private PersonDto? selectedPerson;
    [ObservableProperty] private DepartmentDto editingDepartment = new() { IsActive = true, Level = 1 };
    [ObservableProperty] private PersonDto editingPerson = new() { IsActive = true };

    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string selectedStatusFilter = "Tất cả";
    [ObservableProperty] private bool isDepartmentFormOpen;
    [ObservableProperty] private bool isPersonFormOpen;
    [ObservableProperty] private bool isPeopleSectionFocused;
    [ObservableProperty] private string dataErrorMessage = string.Empty;

    [ObservableProperty] private string departmentCodeError = string.Empty;
    [ObservableProperty] private string departmentNameError = string.Empty;
    [ObservableProperty] private string personCodeError = string.Empty;
    [ObservableProperty] private string personNameError = string.Empty;
    [ObservableProperty] private string personPhoneError = string.Empty;

    public string PeopleTitle => SelectedDepartment is null
        ? "Nhân sự - Tất cả bộ phận"
        : $"Nhân sự - {SelectedDepartment.DepartmentName}";

    public int TotalPeople => People.Count;
    public int ActivePeople => People.Count(x => x.IsActive);
    public int InactivePeople => People.Count(x => !x.IsActive);

    public IReadOnlyList<LookupItemDto> DepartmentLookup => Departments
        .Select(x => new LookupItemDto { Id = x.Id, Code = x.DepartmentCode, Name = x.DepartmentName })
        .ToList();

    public IReadOnlyList<LookupItemDto> DepartmentParentLookup
    {
        get
        {
            var items = new List<LookupItemDto>
            {
                new() { Id = 0, Code = string.Empty, Name = "(Không có - cấp 1)" }
            };
            items.AddRange(DepartmentLookup);
            return items;
        }
    }

    public DepartmentPeoplePageViewModel(
        IDepartmentAppService departmentService,
        IPersonAppService personService,
        IDepartmentPeopleImportExportService importExportService,
        IDialogService dialogService,
        INotificationService notificationService)
    {
        _departmentService = departmentService;
        _personService = personService;
        _importExportService = importExportService;
        _dialogService = dialogService;
        _notificationService = notificationService;

        People.CollectionChanged += OnPeopleCollectionChanged;
        _ = LoadAsync();
    }

    partial void OnKeywordChanged(string value) => DebounceReload();

    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadAsync();

    partial void OnSelectedDepartmentChanged(DepartmentTreeItemViewModel? value)
    {
        if (_suppressSelectedDepartmentChanged)
        {
            return;
        }

        OnPropertyChanged(nameof(PeopleTitle));
        _ = LoadPeopleAsync();
    }

    partial void OnEditingDepartmentChanged(DepartmentDto value)
    {
        DepartmentCodeError = string.Empty;
        DepartmentNameError = string.Empty;
    }

    partial void OnEditingPersonChanged(PersonDto value)
    {
        PersonCodeError = string.Empty;
        PersonNameError = string.Empty;
        PersonPhoneError = string.Empty;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await _loadSemaphore.WaitAsync();
        IsBusy = true;
        DataErrorMessage = string.Empty;

        try
        {
            await LoadDepartmentsAsync();
            await LoadPeopleCoreAsync();
        }
        catch (Exception ex)
        {
            DataErrorMessage = ex.Message;
            _dialogService.Error(ex.Message);
        }
        finally
        {
            IsBusy = false;
            _loadSemaphore.Release();
        }
    }

    private async Task LoadDepartmentsAsync()
    {
        var rows = await _departmentService.SearchAsync(Keyword.Trim(), ParseStatusFilter());
        Departments.Clear();
        foreach (var row in rows)
        {
            Departments.Add(new DepartmentTreeItemViewModel
            {
                Id = row.Id,
                DepartmentCode = row.DepartmentCode,
                DepartmentName = row.DepartmentName,
                ParentDepartmentId = row.ParentDepartmentId,
                Level = row.Level,
                IsActive = row.IsActive,
                PeopleCount = row.PeopleCount
            });
        }

        _suppressSelectedDepartmentChanged = true;
        try
        {
            if (SelectedDepartment is not null)
            {
                SelectedDepartment = Departments.FirstOrDefault(x => x.Id == SelectedDepartment.Id);
            }

            if (SelectedDepartment is null)
            {
                SelectedDepartment = Departments.FirstOrDefault();
            }
        }
        finally
        {
            _suppressSelectedDepartmentChanged = false;
        }

        OnPropertyChanged(nameof(DepartmentLookup));
        OnPropertyChanged(nameof(DepartmentParentLookup));
    }

    private async Task LoadPeopleAsync()
    {
        await _loadSemaphore.WaitAsync();
        try
        {
            await LoadPeopleCoreAsync();
        }
        catch (Exception ex)
        {
            DataErrorMessage = ex.Message;
            _dialogService.Error(ex.Message);
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    private async Task LoadPeopleCoreAsync()
    {
        var rows = await _personService.SearchAsync(SelectedDepartment?.Id, Keyword.Trim(), ParseStatusFilter());
        People.Clear();
        foreach (var row in rows)
        {
            People.Add(row);
        }
        PeoplePager.SetSource(People);

        if (SelectedPerson is not null)
        {
            SelectedPerson = People.FirstOrDefault(x => x.Id == SelectedPerson.Id);
        }

        OnPropertyChanged(nameof(PeopleTitle));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenNewDepartment()
    {
        EditingDepartment = new DepartmentDto
        {
            IsActive = true,
            ParentDepartmentId = null,
            Level = 1
        };
        IsDepartmentFormOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDepartmentAsync(DepartmentTreeItemViewModel? item)
    {
        var source = item ?? SelectedDepartment;
        if (source is null)
        {
            return;
        }

        var departments = await _departmentService.SearchAsync(string.Empty, null);
        var department = departments.FirstOrDefault(x => x.Id == source.Id);
        if (department is null)
        {
            return;
        }

        EditingDepartment = new DepartmentDto
        {
            Id = department.Id,
            DepartmentCode = department.DepartmentCode,
            DepartmentName = department.DepartmentName,
            ParentDepartmentId = department.ParentDepartmentId,
            Level = department.Level,
            Note = department.Note,
            IsActive = department.IsActive
        };

        IsDepartmentFormOpen = true;
    }

    [RelayCommand]
    private async Task SaveDepartmentAsync()
    {
        if (!ValidateDepartment())
        {
            return;
        }

        try
        {
            if (EditingDepartment.ParentDepartmentId is <= 0)
            {
                EditingDepartment.ParentDepartmentId = null;
            }

            var saved = await _departmentService.SaveAsync(EditingDepartment);
            _notificationService.Show("Đã lưu bộ phận.");
            IsDepartmentFormOpen = false;
            await LoadDepartmentsAsync();
            SelectedDepartment = Departments.FirstOrDefault(x => x.Id == saved.Id);
            await LoadPeopleAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteDepartmentAsync()
    {
        if (EditingDepartment.Id <= 0)
        {
            return;
        }

        if (!_dialogService.Confirm("Bạn có chắc muốn xóa bộ phận đang chọn?"))
        {
            return;
        }

        try
        {
            await _departmentService.DeleteAsync(EditingDepartment.Id);
            _notificationService.Show("Đã xóa bộ phận.");
            IsDepartmentFormOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ToggleDepartmentStatusAsync(DepartmentTreeItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            await _departmentService.SaveAsync(new DepartmentDto
            {
                Id = item.Id,
                DepartmentCode = item.DepartmentCode,
                DepartmentName = item.DepartmentName,
                ParentDepartmentId = item.ParentDepartmentId,
                Level = item.Level,
                IsActive = !item.IsActive
            });

            await LoadDepartmentsAsync();
            _notificationService.Show("Đã cập nhật trạng thái bộ phận.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenNewPerson()
    {
        var defaultDept = SelectedDepartment?.Id ?? Departments.FirstOrDefault()?.Id ?? 0;
        EditingPerson = new PersonDto { IsActive = true, DepartmentId = defaultDept };
        IsPersonFormOpen = true;
        IsPeopleSectionFocused = true;
    }

    [RelayCommand]
    private void OpenEditPerson(PersonDto? person)
    {
        var source = person ?? SelectedPerson;
        if (source is null)
        {
            return;
        }

        EditingPerson = new PersonDto
        {
            Id = source.Id,
            PersonCode = source.PersonCode,
            FullName = source.FullName,
            DepartmentId = source.DepartmentId,
            DepartmentCode = source.DepartmentCode,
            DepartmentName = source.DepartmentName,
            Position = source.Position,
            Phone = source.Phone,
            Email = source.Email,
            Note = source.Note,
            IsActive = source.IsActive,
            CreatedAt = source.CreatedAt
        };

        IsPersonFormOpen = true;
        IsPeopleSectionFocused = true;
    }

    [RelayCommand]
    private async Task SavePersonAsync()
    {
        if (!ValidatePerson())
        {
            return;
        }

        try
        {
            var saved = await _personService.SaveAsync(EditingPerson);
            _notificationService.Show("Đã lưu nhân sự.");
            IsPersonFormOpen = false;
            await LoadDepartmentsAsync();
            await LoadPeopleAsync();
            SelectedPerson = People.FirstOrDefault(x => x.Id == saved.Id);
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeletePersonAsync()
    {
        if (EditingPerson.Id <= 0)
        {
            return;
        }

        if (!_dialogService.Confirm("Bạn có chắc muốn xóa nhân sự đang chọn?"))
        {
            return;
        }

        try
        {
            await _personService.DeleteAsync(EditingPerson.Id);
            _notificationService.Show("Đã xóa nhân sự.");
            IsPersonFormOpen = false;
            await LoadDepartmentsAsync();
            await LoadPeopleAsync();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task TogglePersonStatusAsync(PersonDto? person)
    {
        var source = person ?? SelectedPerson;
        if (source is null)
        {
            return;
        }

        try
        {
            await _personService.SaveAsync(new PersonDto
            {
                Id = source.Id,
                PersonCode = source.PersonCode,
                FullName = source.FullName,
                DepartmentId = source.DepartmentId,
                Position = source.Position,
                Phone = source.Phone,
                Email = source.Email,
                Note = source.Note,
                IsActive = !source.IsActive,
                CreatedAt = source.CreatedAt
            });

            await LoadDepartmentsAsync();
            await LoadPeopleAsync();
            _notificationService.Show("Đã cập nhật trạng thái nhân sự.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private void CloseDepartmentForm() => IsDepartmentFormOpen = false;

    [RelayCommand]
    private void ClosePersonForm() => IsPersonFormOpen = false;

    [RelayCommand]
    private async Task ExportDepartmentTemplateAsync()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
                FileName = "department_template.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportDepartmentTemplateAsync(saveDialog.FileName);
            _notificationService.Show("Đã xuất mẫu bộ phận.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportPersonTemplateAsync()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
                FileName = "person_template.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportPersonTemplateAsync(saveDialog.FileName);
            _notificationService.Show("Đã xuất mẫu nhân sự.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportCurrentViewAsync()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV UTF-8 (*.csv)|*.csv",
                FileName = $"people_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            await _importExportService.ExportCurrentViewAsync(People, saveDialog.FileName);
            _notificationService.Show("Đã xuất dữ liệu hiện tại.");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        try
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
                CheckFileExists = true
            };

            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            var payload = await _importExportService.ReadImportPayloadAsync(openDialog.FileName);
            var imported = 0;
            var skipped = 0;
            var errors = 0;

            if (payload.Departments.Count > 0)
            {
                var result = await ImportDepartmentsAsync(payload.Departments);
                imported += result.imported;
                skipped += result.skipped;
                errors += result.errors;
            }

            if (payload.People.Count > 0)
            {
                var result = await ImportPeopleAsync(payload.People);
                imported += result.imported;
                skipped += result.skipped;
                errors += result.errors;
            }

            await LoadAsync();
            _dialogService.Info($"Nhập dữ liệu hoàn tất.\nThành công: {imported}\nBỏ qua: {skipped}\nLỗi: {errors}");
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    public void FocusPeopleSection()
    {
        IsPeopleSectionFocused = true;
    }

    private async Task<(int imported, int skipped, int errors)> ImportDepartmentsAsync(IEnumerable<DepartmentImportRow> rows)
    {
        var imported = 0;
        var skipped = 0;
        var errors = 0;

        var existing = (await _departmentService.SearchAsync(string.Empty, null)).ToDictionary(x => x.DepartmentCode, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.DepartmentCode) || string.IsNullOrWhiteSpace(row.DepartmentName))
            {
                skipped++;
                continue;
            }

            try
            {
                existing.TryGetValue(row.DepartmentCode.Trim(), out var current);

                int? parentId = null;
                if (!string.IsNullOrWhiteSpace(row.ParentDepartmentCode)
                    && existing.TryGetValue(row.ParentDepartmentCode.Trim(), out var parent))
                {
                    parentId = parent.Id;
                }

                var level = int.TryParse(row.LevelRaw, out var parsedLevel) && parsedLevel > 0 ? parsedLevel : 1;
                var isActive = ParseBoolean(row.IsActiveRaw, defaultValue: true);

                var saved = await _departmentService.SaveAsync(new DepartmentDto
                {
                    Id = current?.Id ?? 0,
                    DepartmentCode = row.DepartmentCode.Trim(),
                    DepartmentName = row.DepartmentName.Trim(),
                    ParentDepartmentId = parentId,
                    Level = level,
                    Note = row.Note?.Trim() ?? string.Empty,
                    IsActive = isActive
                });

                existing[saved.DepartmentCode] = saved;
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        return (imported, skipped, errors);
    }

    private async Task<(int imported, int skipped, int errors)> ImportPeopleAsync(IEnumerable<PersonImportRow> rows)
    {
        var imported = 0;
        var skipped = 0;
        var errors = 0;

        var departments = (await _departmentService.SearchAsync(string.Empty, null))
            .ToDictionary(x => x.DepartmentCode, StringComparer.OrdinalIgnoreCase);
        var existingPeople = (await _personService.SearchAsync(null, string.Empty, null))
            .ToDictionary(x => x.PersonCode, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PersonCode) || string.IsNullOrWhiteSpace(row.FullName) || string.IsNullOrWhiteSpace(row.DepartmentCode))
            {
                skipped++;
                continue;
            }

            if (!departments.TryGetValue(row.DepartmentCode.Trim(), out var dept))
            {
                errors++;
                continue;
            }

            try
            {
                existingPeople.TryGetValue(row.PersonCode.Trim(), out var current);

                var saved = await _personService.SaveAsync(new PersonDto
                {
                    Id = current?.Id ?? 0,
                    PersonCode = row.PersonCode.Trim(),
                    FullName = row.FullName.Trim(),
                    DepartmentId = dept.Id,
                    Position = row.Position?.Trim() ?? string.Empty,
                    Phone = row.Phone?.Trim() ?? string.Empty,
                    Email = row.Email?.Trim() ?? string.Empty,
                    Note = row.Note?.Trim() ?? string.Empty,
                    IsActive = ParseBoolean(row.IsActiveRaw, defaultValue: true),
                    CreatedAt = current?.CreatedAt ?? DateTime.UtcNow
                });

                existingPeople[saved.PersonCode] = saved;
                imported++;
            }
            catch
            {
                errors++;
            }
        }

        return (imported, skipped, errors);
    }

    private bool ValidateDepartment()
    {
        DepartmentCodeError = string.Empty;
        DepartmentNameError = string.Empty;

        if (string.IsNullOrWhiteSpace(EditingDepartment.DepartmentCode))
        {
            DepartmentCodeError = "Mã bộ phận là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(EditingDepartment.DepartmentName))
        {
            DepartmentNameError = "Tên bộ phận là bắt buộc.";
        }

        if (!string.IsNullOrWhiteSpace(DepartmentCodeError) || !string.IsNullOrWhiteSpace(DepartmentNameError))
        {
            _dialogService.Error("Vui lòng kiểm tra thông tin bộ phận.");
            return false;
        }

        return true;
    }

    private bool ValidatePerson()
    {
        PersonCodeError = string.Empty;
        PersonNameError = string.Empty;
        PersonPhoneError = string.Empty;

        if (string.IsNullOrWhiteSpace(EditingPerson.PersonCode))
        {
            PersonCodeError = "Mã nhân sự là bắt buộc.";
        }

        if (string.IsNullOrWhiteSpace(EditingPerson.FullName))
        {
            PersonNameError = "Tên nhân sự là bắt buộc.";
        }

        if (EditingPerson.DepartmentId <= 0)
        {
            PersonNameError = "Vui lòng chọn bộ phận.";
        }

        if (!string.IsNullOrWhiteSpace(EditingPerson.Phone) && !PhoneRegex.IsMatch(EditingPerson.Phone.Trim()))
        {
            PersonPhoneError = "Số điện thoại không hợp lệ.";
        }

        if (!string.IsNullOrWhiteSpace(PersonCodeError)
            || !string.IsNullOrWhiteSpace(PersonNameError)
            || !string.IsNullOrWhiteSpace(PersonPhoneError))
        {
            _dialogService.Error("Vui lòng kiểm tra thông tin nhân sự.");
            return false;
        }

        return true;
    }

    private void DebounceReload()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = DebounceReloadAsync(token);
    }

    private async Task DebounceReloadAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_searchDebounce, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await LoadAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool? ParseStatusFilter()
    {
        return SelectedStatusFilter switch
        {
            "Đang làm" => true,
            "Đã nghỉ" => false,
            _ => null
        };
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "1" or "true" or "yes" or "y" or "co" or "có" or "active" or "đang hoạt động";
    }

    private void OnPeopleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalPeople));
        OnPropertyChanged(nameof(ActivePeople));
        OnPropertyChanged(nameof(InactivePeople));
    }
}
