using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace ASMILE.BO.App.ViewModels;

public partial class SupplierImportPreviewDialogViewModel : ObservableObject
{
    private readonly ISupplierImportValidator _validator;
    private readonly IReadOnlyList<SupplierDto> _existingSuppliers;

    public ObservableCollection<SupplierImportPreviewRowViewModel> Rows { get; } = [];
    public ICollectionView RowsView { get; }

    public ObservableCollection<string> FilterOptions { get; } =
    [
        "Tất cả",
        "Hợp lệ",
        "Cảnh báo",
        "Trùng dữ liệu",
        "Lỗi"
    ];

    [ObservableProperty] private string selectedFilter = "Tất cả";
    [ObservableProperty] private bool canConfirmImport;

    public bool IsAllFilterActive => SelectedFilter == "Tất cả";
    public bool IsValidFilterActive => SelectedFilter == "Hợp lệ";
    public bool IsWarningFilterActive => SelectedFilter == "Cảnh báo";
    public bool IsDuplicateFilterActive => SelectedFilter == "Trùng dữ liệu";
    public bool IsErrorFilterActive => SelectedFilter == "Lỗi";

    public SupplierImportSummaryViewModel Summary { get; } = new();

    public event Action<bool>? RequestClose;

    public SupplierImportPreviewDialogViewModel(
        IEnumerable<SupplierImportRow> rows,
        IReadOnlyList<SupplierDto> existingSuppliers,
        ISupplierImportValidator validator)
    {
        _existingSuppliers = existingSuppliers;
        _validator = validator;

        foreach (var row in rows)
        {
            var vm = new SupplierImportPreviewRowViewModel
            {
                RowNumber = row.RowNumber,
                SupplierCode = row.SupplierCode,
                SupplierName = row.SupplierName,
                Phone = row.Phone,
                BankAccountNo = row.BankAccountNo,
                Address = row.Address,
                Note = row.Note,
                Email = row.Email,
                TaxCode = row.TaxCode,
                IsActive = row.IsActive
            };

            vm.PropertyChanged += PreviewRowOnPropertyChanged;
            Rows.Add(vm);
        }

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = FilterRow;

        Revalidate();
    }

    public IReadOnlyList<SupplierDto> BuildImportDtos()
    {
        return Rows
            .Where(x => x.IsSelected && !x.HasBlockingError)
            .Select(x => new SupplierDto
            {
                SupplierCode = x.SupplierCode.Trim(),
                SupplierName = x.SupplierName.Trim(),
                Phone = x.Phone.Trim(),
                BankAccountNo = x.BankAccountNo.Trim(),
                Address = x.Address.Trim(),
                Note = x.Note.Trim(),
                Email = x.Email.Trim(),
                TaxCode = x.TaxCode.Trim(),
                IsActive = x.IsActive
            })
            .ToList();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        RowsView.Refresh();
        OnPropertyChanged(nameof(IsAllFilterActive));
        OnPropertyChanged(nameof(IsValidFilterActive));
        OnPropertyChanged(nameof(IsWarningFilterActive));
        OnPropertyChanged(nameof(IsDuplicateFilterActive));
        OnPropertyChanged(nameof(IsErrorFilterActive));
    }

    [RelayCommand]
    private void SetFilter(string? filter)
    {
        SelectedFilter = string.IsNullOrWhiteSpace(filter) ? "Tất cả" : filter;
    }

    [RelayCommand]
    private void ValidateImportRows()
    {
        Revalidate();
    }

    [RelayCommand]
    private void ConfirmImport()
    {
        if (!CanConfirmImport)
        {
            return;
        }

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void CancelImport()
    {
        RequestClose?.Invoke(false);
    }

    private void PreviewRowOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SupplierImportPreviewRowViewModel.IsSelected))
        {
            RefreshConfirmState();
        }
    }

    private bool FilterRow(object obj)
    {
        if (obj is not SupplierImportPreviewRowViewModel row)
        {
            return true;
        }

        return SelectedFilter switch
        {
            "Hợp lệ" => row.State == SupplierImportRowState.Valid,
            "Cảnh báo" => row.State == SupplierImportRowState.Warning,
            "Trùng dữ liệu" => row.State == SupplierImportRowState.Duplicate,
            "Lỗi" => row.State == SupplierImportRowState.Error,
            _ => true
        };
    }

    private void Revalidate()
    {
        var models = Rows.Select(x => x.ToModel()).ToList();
        var issuesByRow = new Dictionary<int, IReadOnlyList<SupplierImportValidationIssue>>();
        _validator.ValidateRows(models, _existingSuppliers, issuesByRow);

        foreach (var row in Rows)
        {
            var model = models.First(x => x.RowNumber == row.RowNumber);
            var issues = issuesByRow.TryGetValue(row.RowNumber, out var values)
                ? values
                : [];

            row.SupplierCode = model.SupplierCode;
            row.SupplierName = model.SupplierName;
            row.Phone = model.Phone;
            row.BankAccountNo = model.BankAccountNo;
            row.Address = model.Address;
            row.Note = model.Note;
            row.Email = model.Email;
            row.TaxCode = model.TaxCode;
            row.IsActive = model.IsActive;

            row.Issues.Clear();
            row.Issues.AddRange(issues);
            ApplyState(row);
        }

        UpdateSummary();
        RefreshConfirmState();
        RowsView.Refresh();
    }

    private void ApplyState(SupplierImportPreviewRowViewModel row)
    {
        var hasError = row.Issues.Any(x => x.Severity == SupplierImportIssueSeverity.Error);
        var hasWarning = row.Issues.Any(x => x.Severity == SupplierImportIssueSeverity.Warning);
        var hasDuplicate = row.Issues.Any(x => x.Code.Contains("dup_", StringComparison.OrdinalIgnoreCase));

        row.HasBlockingError = hasError;

        if (hasDuplicate)
        {
            row.State = SupplierImportRowState.Duplicate;
            row.StateText = "Trùng dữ liệu";
            row.StateBackground = "#FDEEEF";
            row.StateForeground = "#B54A4A";
        }
        else if (hasError)
        {
            row.State = SupplierImportRowState.Error;
            row.StateText = "Lỗi";
            row.StateBackground = "#FDE9EC";
            row.StateForeground = "#C74343";
        }
        else if (hasWarning)
        {
            row.State = SupplierImportRowState.Warning;
            row.StateText = "Cảnh báo";
            row.StateBackground = "#FFF7E8";
            row.StateForeground = "#B87410";
        }
        else
        {
            row.State = SupplierImportRowState.Valid;
            row.StateText = "Hợp lệ";
            row.StateBackground = "#E8F5ED";
            row.StateForeground = "#1E7C48";
        }

        var messages = row.Issues.Select(x => x.Message).ToList();
        row.StateMessage = messages.Count == 0 ? "Dữ liệu hợp lệ" : string.Join(" | ", messages);
        row.IssueTooltip = messages.Count == 0
            ? "Dữ liệu hợp lệ"
            : string.Join(Environment.NewLine, messages.Select(x => "• " + x));
        row.IssueSummary = BuildIssueSummary(row.Issues);
    }

    private void UpdateSummary()
    {
        Summary.TotalRows = Rows.Count;
        Summary.ValidRows = Rows.Count(x => x.State == SupplierImportRowState.Valid);
        Summary.WarningRows = Rows.Count(x => x.State == SupplierImportRowState.Warning);
        Summary.DuplicateRows = Rows.Count(x => x.State == SupplierImportRowState.Duplicate);
        Summary.ErrorRows = Rows.Count(x => x.State == SupplierImportRowState.Error);
    }

    private void RefreshConfirmState()
    {
        CanConfirmImport = Rows.Any(x => x.IsSelected && !x.HasBlockingError);
    }

    private static string BuildIssueSummary(IReadOnlyList<SupplierImportValidationIssue> issues)
    {
        if (issues.Count == 0)
        {
            return "Hợp lệ";
        }

        var labels = issues
            .Select(MapIssueCodeToShortLabel)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (labels.Count == 0)
        {
            return issues.Count == 1 ? "1 vấn đề" : $"{issues.Count} vấn đề";
        }

        if (labels.Count == 1)
        {
            return labels[0];
        }

        if (labels.Count == 2)
        {
            return $"{labels[0]} + {labels[1]}";
        }

        return $"{labels[0]} + {labels[1]} +{labels.Count - 2}";
    }

    private static string MapIssueCodeToShortLabel(SupplierImportValidationIssue issue)
    {
        return issue.Code switch
        {
            "dup_file_code" or "dup_db_code" => "Trùng mã",
            "dup_file_phone" or "dup_db_phone" => "Trùng SĐT",
            "required_code" => "Thiếu mã",
            "required_name" => "Thiếu tên",
            "missing_phone" => "Thiếu SĐT",
            "invalid_phone" => "Sai SĐT",
            "invalid_active" => "Sai kích hoạt",
            _ => string.IsNullOrWhiteSpace(issue.Message) ? "Lỗi dữ liệu" : issue.Message
        };
    }
}
