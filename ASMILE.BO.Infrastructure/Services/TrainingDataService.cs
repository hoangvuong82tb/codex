using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class TrainingDataService : ITrainingDataService
{
    private readonly AppDbContext _dbContext;
    private readonly ITrainingDataCleaner _cleaner;
    private readonly ITrainingDataSeeder _seeder;
    private readonly ITrainingDataValidator _validator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly TrainingDataOptions _options;
    private const string MarkerTableMissingWarningCode = "TRAINING_MARKER_TABLE_MISSING";

    public TrainingDataService(
        AppDbContext dbContext,
        ITrainingDataCleaner cleaner,
        ITrainingDataSeeder seeder,
        ITrainingDataValidator validator,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IHostEnvironment hostEnvironment,
        IOptions<TrainingDataOptions> options)
    {
        _dbContext = dbContext;
        _cleaner = cleaner;
        _seeder = seeder;
        _validator = validator;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _hostEnvironment = hostEnvironment;
        _options = options.Value;
    }

    public async Task<TrainingDataStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var journals = await _dbContext.JournalHeaders.CountAsync(cancellationToken);
        var inventoryDocs = await _dbContext.StockReceipts.CountAsync(cancellationToken) + await _dbContext.StockIssues.CountAsync(cancellationToken);
        var cashTransactions = await _dbContext.CashTransactions.CountAsync(cancellationToken);
        var bankTransactions = await _dbContext.BankTransactions.CountAsync(cancellationToken);
        var customers = await _dbContext.Customers.CountAsync(cancellationToken);
        var suppliers = await _dbContext.Suppliers.CountAsync(cancellationToken);
        var items = await _dbContext.Items.CountAsync(cancellationToken);
        var warehouses = await _dbContext.Warehouses.CountAsync(cancellationToken);

        var hasBusinessData = journals > 0 || inventoryDocs > 0 || cashTransactions > 0 || bankTransactions > 0
            || customers > 0 || suppliers > 0 || items > 0 || warehouses > 0;

        var trainingCompany = await _dbContext.Companies.AnyAsync(x => x.Name.Contains("Demo ASMILE"), cancellationToken);
        var signatureItem = await _dbContext.Items.AnyAsync(x => x.ItemCode.StartsWith("HH00"), cancellationToken);

        return new TrainingDataStatusDto
        {
            HasBusinessData = hasBusinessData,
            HasTrainingDataSignature = trainingCompany || signatureItem,
            JournalCount = journals,
            InventoryDocumentCount = inventoryDocs,
            CashTransactionCount = cashTransactions,
            BankTransactionCount = bankTransactions,
            CustomerCount = customers,
            SupplierCount = suppliers,
            ItemCount = items,
            WarehouseCount = warehouses,
            CheckedAtUtc = DateTime.UtcNow,
            Summary = hasBusinessData ? "Database đang có dữ liệu nghiệp vụ." : "Database đang ở trạng thái gần trắng."
        };
    }

    public async Task<TrainingDataPreflightDto> GetPreflightAsync(CancellationToken cancellationToken = default)
    {
        var effectiveOptions = ResolveEffectiveOptions();
        var warnings = new List<TrainingDataSafetyWarningDto>();
        var journalCount = await _dbContext.JournalHeaders.CountAsync(cancellationToken);
        var businessDocCount = await _dbContext.StockReceipts.CountAsync(cancellationToken)
            + await _dbContext.StockIssues.CountAsync(cancellationToken)
            + await _dbContext.CashTransactions.CountAsync(cancellationToken)
            + await _dbContext.BankTransactions.CountAsync(cancellationToken)
            + journalCount;
        var markerTableReady = await IsMarkerTableReadyAsync(cancellationToken);
        var markerCount = markerTableReady
            ? await _dbContext.TrainingDataMarkers.CountAsync(cancellationToken)
            : 0;
        var hasRealDataRisk = businessDocCount > 0 && markerCount == 0;
        if (!markerTableReady)
        {
            warnings.Add(new TrainingDataSafetyWarningDto
            {
                Code = MarkerTableMissingWarningCode,
                Severity = "Warning",
                Message = "Cần cập nhật cơ sở dữ liệu/migration trước khi dùng chức năng này.",
                IsBlocking = false
            });
        }

        if (!effectiveOptions.AllowDestructiveReset)
        {
            warnings.Add(new TrainingDataSafetyWarningDto
            {
                Code = "DESTRUCTIVE_DISABLED",
                Severity = "Warning",
                Message = "Tùy chọn destructive reset đang tắt.",
                IsBlocking = false
            });
        }

        if (_hostEnvironment.IsProduction() && !effectiveOptions.AllowInProduction)
        {
            warnings.Add(new TrainingDataSafetyWarningDto
            {
                Code = "PRODUCTION_GUARD",
                Severity = "Warning",
                Message = "Môi trường Production đang khóa destructive reset.",
                IsBlocking = false
            });
        }

        if (hasRealDataRisk)
        {
            warnings.Add(new TrainingDataSafetyWarningDto
            {
                Code = "REAL_DATA_RISK",
                Severity = "Warning",
                Message = "Phát hiện dữ liệu nghiệp vụ không có marker training, cần kiểm tra trước khi reset.",
                IsBlocking = false
            });
        }

        return new TrainingDataPreflightDto
        {
            CheckedAtUtc = DateTime.UtcNow,
            ProviderName = _dbContext.Database.ProviderName ?? "unknown",
            EnvironmentName = _hostEnvironment.EnvironmentName,
            AllowDestructiveReset = effectiveOptions.AllowDestructiveReset,
            AllowInProduction = effectiveOptions.AllowInProduction,
            AllowResetWithRealDataRisk = effectiveOptions.AllowResetWithRealDataRisk,
            CompanyCount = await _dbContext.Companies.CountAsync(cancellationToken),
            BranchCount = await _dbContext.Branches.CountAsync(cancellationToken),
            JournalCount = journalCount,
            BusinessDocumentCount = businessDocCount,
            SystemUserCount = await _dbContext.AppUsers.CountAsync(cancellationToken),
            RoleCount = await _dbContext.AppRoles.CountAsync(cancellationToken),
            PermissionCount = await _dbContext.AppPermissions.CountAsync(cancellationToken),
            AccountingStandardCount = await _dbContext.AccountingStandards.CountAsync(cancellationToken),
            SettingsCount = await _dbContext.AppSettings.CountAsync(cancellationToken),
            TrainingMarkerCount = markerCount,
            HasRealDataRisk = hasRealDataRisk,
            RiskLevel = hasRealDataRisk ? "High" : "Low",
            Summary = hasRealDataRisk
                ? "Có rủi ro dữ liệu thật, cần kiểm tra kỹ trước thao tác destructive."
                : "Checklist an toàn đã sẵn sàng.",
            Warnings = warnings
        };
    }

    public Task<IReadOnlyList<TrainingDataSeedProfileDto>> GetSeedProfilesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TrainingDataSeedProfileDto> profiles =
        [
            new() { Profile = TrainingDataSeedProfile.Blank, Name = "Blank", Description = "Xóa dữ liệu nghiệp vụ, giữ dữ liệu hệ thống bắt buộc." },
            new() { Profile = TrainingDataSeedProfile.FoundationOnly, Name = "FoundationOnly", Description = "Nạp dữ liệu nền chuẩn." },
            new() { Profile = TrainingDataSeedProfile.FullTraining, Name = "FullTraining", Description = "Nạp dữ liệu nền + giao dịch mẫu đầy đủ." },
            new() { Profile = TrainingDataSeedProfile.AccountingDemo, Name = "AccountingDemo", Description = "Bộ mẫu tập trung kế toán." },
            new() { Profile = TrainingDataSeedProfile.InventoryDemo, Name = "InventoryDemo", Description = "Bộ mẫu tập trung kho và giá vốn." },
            new() { Profile = TrainingDataSeedProfile.NewCompanyBlank, Name = "NewCompanyBlank", Description = "Onboarding doanh nghiệp mới ở trạng thái trắng." },
            new() { Profile = TrainingDataSeedProfile.NewCompanyFoundation, Name = "NewCompanyFoundation", Description = "Onboarding dữ liệu nền doanh nghiệp mới." },
            new() { Profile = TrainingDataSeedProfile.NewCompanyFullTraining, Name = "NewCompanyFullTraining", Description = "Onboarding dữ liệu nền + training đầy đủ." },
            new() { Profile = TrainingDataSeedProfile.RestaurantDemo, Name = "RestaurantDemo", Description = "Dữ liệu demo nhà hàng." },
            new() { Profile = TrainingDataSeedProfile.HotelDemo, Name = "HotelDemo", Description = "Dữ liệu demo khách sạn." },
            new() { Profile = TrainingDataSeedProfile.RestaurantHotelFullDemo, Name = "RestaurantHotelFullDemo", Description = "Dữ liệu demo khách sạn + nhà hàng." }
        ];
        return Task.FromResult(profiles);
    }

    public Task<IReadOnlyList<TrainingDataDemoProfileDto>> GetDemoProfilesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TrainingDataDemoProfileDto> profiles =
        [
            new() { Profile = TrainingDataSeedProfile.RestaurantDemo, DisplayName = "Nhà hàng", Description = "Mô hình vận hành nhà hàng.", IndustryType = "Restaurant" },
            new() { Profile = TrainingDataSeedProfile.HotelDemo, DisplayName = "Khách sạn", Description = "Mô hình vận hành khách sạn.", IndustryType = "Hotel" },
            new() { Profile = TrainingDataSeedProfile.RestaurantHotelFullDemo, DisplayName = "Khách sạn + Nhà hàng", Description = "Mô hình khách sạn có nhà hàng.", IndustryType = "RestaurantHotel" }
        ];
        return Task.FromResult(profiles);
    }

    public Task<TrainingDataDemoPreviewDto> GetDemoPreviewAsync(TrainingDataSeedProfile profile, CancellationToken cancellationToken = default)
    {
        var effective = profile == TrainingDataSeedProfile.FullTraining ? TrainingDataSeedProfile.RestaurantHotelFullDemo : profile;
        TrainingDataDemoPreviewDto dto = effective switch
        {
            TrainingDataSeedProfile.RestaurantDemo => new TrainingDataDemoPreviewDto
            {
                Profile = effective,
                DisplayName = "Nhà hàng",
                Description = "Seed dữ liệu demo cho vận hành nhà hàng.",
                IndustryType = "Restaurant",
                EstimatedCounts =
                [
                    new() { Name = "Khách hàng/NCC", EstimatedCount = 8 },
                    new() { Name = "Hàng hóa/Dịch vụ", EstimatedCount = 16 },
                    new() { Name = "Chứng từ", EstimatedCount = 20 },
                    new() { Name = "Journal", EstimatedCount = 16 },
                    new() { Name = "VAT", EstimatedCount = 8 },
                    new() { Name = "Kho/Quỹ/NH", EstimatedCount = 7 }
                ],
                KeyModules = ["Kho", "Sổ cái", "Công nợ", "VAT", "Quỹ", "Ngân hàng"],
                SafetyNotes = ["Chỉ dùng cho training/demo.", "Có thể xóa bằng ResetTrainingOnly."]
            },
            TrainingDataSeedProfile.HotelDemo => new TrainingDataDemoPreviewDto
            {
                Profile = effective,
                DisplayName = "Khách sạn",
                Description = "Seed dữ liệu demo cho vận hành khách sạn.",
                IndustryType = "Hotel",
                EstimatedCounts =
                [
                    new() { Name = "Khách hàng/Đối tác", EstimatedCount = 10 },
                    new() { Name = "Dịch vụ/Loại phòng", EstimatedCount = 12 },
                    new() { Name = "Chứng từ", EstimatedCount = 18 },
                    new() { Name = "Journal", EstimatedCount = 14 },
                    new() { Name = "VAT", EstimatedCount = 6 },
                    new() { Name = "Quỹ/NH", EstimatedCount = 4 }
                ],
                KeyModules = ["Sổ cái", "Doanh thu", "Công nợ", "VAT", "Quỹ", "Ngân hàng"],
                SafetyNotes = ["Phù hợp demo lễ tân/kế toán khách sạn.", "Có marker để reset chọn lọc."]
            },
            _ => new TrainingDataDemoPreviewDto
            {
                Profile = TrainingDataSeedProfile.RestaurantHotelFullDemo,
                DisplayName = "Khách sạn + Nhà hàng",
                Description = "Seed dữ liệu demo kết hợp khách sạn và nhà hàng.",
                IndustryType = "RestaurantHotel",
                EstimatedCounts =
                [
                    new() { Name = "Khách hàng/NCC/Đối tác", EstimatedCount = 16 },
                    new() { Name = "Hàng hóa/Dịch vụ", EstimatedCount = 28 },
                    new() { Name = "Chứng từ", EstimatedCount = 36 },
                    new() { Name = "Journal", EstimatedCount = 28 },
                    new() { Name = "VAT", EstimatedCount = 14 },
                    new() { Name = "Kho/Quỹ/NH", EstimatedCount = 10 }
                ],
                KeyModules = ["Dashboard", "Sổ cái", "Kho", "Công nợ", "VAT", "Quỹ", "Ngân hàng"],
                SafetyNotes = ["Dùng cho demo tổng thể doanh nghiệp.", "Cần chạy preflight trước full reset."]
            }
        };
        return Task.FromResult(dto);
    }

    public async Task<TrainingDataOperationResult> ExecuteAsync(TrainingDataOperationRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var correlationId = Guid.NewGuid().ToString("N");
        var preflight = await GetPreflightAsync(cancellationToken);
        if (!request.IsDryRun && RequiresMarkerTable(request.OperationType) && preflight.Warnings.Any(x => x.Code == MarkerTableMissingWarningCode))
        {
            var reason = "Thiếu bảng TrainingDataMarkers. Cần cập nhật migration trước khi chạy thao tác phụ thuộc marker.";
            await SafeAuditBlockedAsync(request, preflight, reason, correlationId, cancellationToken);
            throw new InvalidOperationException("Cần cập nhật cơ sở dữ liệu/migration trước khi dùng chức năng này.");
        }
        try
        {
            ValidateGuard(request, preflight);
        }
        catch (Exception ex)
        {
            await SafeAuditBlockedAsync(request, preflight, ex.Message, correlationId, cancellationToken);
            throw;
        }

        var now = DateTime.UtcNow;
        var run = new TrainingDataRun
        {
            OperationType = request.OperationType.ToString(),
            SeedProfile = request.SeedProfile?.ToString(),
            StartedAtUtc = now,
            Status = "Running",
            RequestedByUserId = _currentUserService.CurrentUser?.Id,
            IsDryRun = request.IsDryRun
        };
        _dbContext.TrainingDataRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var stepResults = new List<TrainingDataStepResult>();
        var warnings = new List<string>();
        var totalDeleted = 0;
        var totalCreated = 0;
        var success = false;
        string? error = null;
        var hasAmbientTransaction = _dbContext.Database.CurrentTransaction is not null;
        var hasNestedTransactionalServices = request.OperationType is TrainingDataOperationType.SeedFoundation
            or TrainingDataOperationType.SeedFullTraining;
        var useTransaction = !request.IsDryRun
            && _dbContext.Database.IsRelational()
            && !hasAmbientTransaction
            && !hasNestedTransactionalServices;
        var tx = useTransaction ? await _dbContext.Database.BeginTransactionAsync(cancellationToken) : null;

        try
        {
            switch (request.OperationType)
            {
                case TrainingDataOperationType.CheckStatus:
                    var status = await GetStatusAsync(cancellationToken);
                    stepResults.Add(new TrainingDataStepResult
                    {
                        StepOrder = 1,
                        StepCode = "CHECK_STATUS",
                        StepName = "Kiểm tra dữ liệu hiện tại",
                        Success = true,
                        Message = status.Summary,
                        StartedAtUtc = now,
                        FinishedAtUtc = DateTime.UtcNow
                    });
                    break;
                case TrainingDataOperationType.PreflightCheck:
                    var preflightResult = await GetPreflightAsync(cancellationToken);
                    stepResults.Add(new TrainingDataStepResult
                    {
                        StepOrder = 1,
                        StepCode = "PREFLIGHT",
                        StepName = "Kiểm tra preflight",
                        Success = true,
                        Status = "Success",
                        Message = preflightResult.Summary,
                        StartedAtUtc = now,
                        FinishedAtUtc = DateTime.UtcNow
                    });
                    warnings.AddRange(preflightResult.Warnings.Select(x => $"{x.Code}: {x.Message}"));
                    break;
                case TrainingDataOperationType.ClearBusinessData:
                case TrainingDataOperationType.DeleteTrainingData:
                case TrainingDataOperationType.ResetToBlank:
                    stepResults.AddRange(await _cleaner.ClearAsync(request.ResetScope, request.IsDryRun, cancellationToken));
                    break;
                case TrainingDataOperationType.ResetTrainingOnly:
                    stepResults.AddRange(await _cleaner.ClearTrainingOnlyAsync(request.SeedProfile, request.IsDryRun, cancellationToken));
                    break;
                case TrainingDataOperationType.SeedFoundation:
                    stepResults.AddRange(await _seeder.SeedAsync(request.SeedProfile ?? TrainingDataSeedProfile.FoundationOnly, request.IsDryRun, run.Id, cancellationToken));
                    break;
                case TrainingDataOperationType.SeedFullTraining:
                    stepResults.AddRange(await _seeder.SeedAsync(request.SeedProfile ?? TrainingDataSeedProfile.FullTraining, request.IsDryRun, run.Id, cancellationToken));
                    break;
            }

            totalDeleted = stepResults.Sum(x => x.DeletedCount);
            totalCreated = stepResults.Sum(x => x.CreatedCount);

            if (request.OperationType is TrainingDataOperationType.SeedFoundation or TrainingDataOperationType.SeedFullTraining)
            {
                warnings.AddRange(await _validator.ValidateAsync(cancellationToken));
            }

            if (!request.IsDryRun && tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            success = stepResults.All(x => x.Success);
        }
        catch (Exception ex)
        {
            if (!request.IsDryRun && tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            success = false;
            error = TruncateForColumn(ResolveDetailedErrorMessage(ex), 2000);
            stepResults.Add(new TrainingDataStepResult
            {
                StepOrder = stepResults.Count + 1,
                StepCode = "UNHANDLED_ERROR",
                StepName = "Lỗi thực thi",
                Success = false,
                ErrorMessage = error,
                Message = "Thao tác thất bại.",
                StartedAtUtc = DateTime.UtcNow,
                FinishedAtUtc = DateTime.UtcNow
            });
        }

        var finish = DateTime.UtcNow;
        run.FinishedAtUtc = finish;
        run.Status = success ? "Success" : "Failed";
        run.ErrorMessage = error;
        run.SummaryJson = JsonSerializer.Serialize(new
        {
            request.OperationType,
            request.SeedProfile,
            request.ResetScope,
            request.IsDryRun,
            correlationId,
            totalDeleted,
            totalCreated,
            preflight.ProviderName,
            preflight.EnvironmentName,
            preflight.HasRealDataRisk,
            preflight.RiskLevel,
            warningCount = warnings.Count
        });

        foreach (var step in stepResults.OrderBy(x => x.StepOrder))
        {
            _dbContext.TrainingDataRunSteps.Add(new TrainingDataRunStep
            {
                TrainingDataRunId = run.Id,
                StepOrder = step.StepOrder,
                StepCode = step.StepCode,
                StepName = step.StepName,
                EntityName = step.EntityName,
                Success = step.Success,
                BeforeCount = step.BeforeCount,
                DeletedCount = step.DeletedCount,
                AfterCount = step.AfterCount,
                CreatedCount = step.CreatedCount,
                Status = step.Status,
                Message = step.Message,
                ErrorMessage = TruncateForColumn(step.ErrorMessage, 2000),
                StartedAtUtc = step.StartedAtUtc,
                FinishedAtUtc = step.FinishedAtUtc
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SafeAuditAsync(run, request, preflight, success, error, correlationId, cancellationToken);

        return new TrainingDataOperationResult
        {
            RunId = run.Id,
            OperationType = request.OperationType,
            SeedProfile = request.SeedProfile,
            ResetScope = request.ResetScope,
            IsDryRun = request.IsDryRun,
            Success = success,
            Message = success ? "Thực thi hoàn tất." : "Thực thi thất bại.",
            ErrorMessage = error,
            TotalDeletedCount = totalDeleted,
            TotalCreatedCount = totalCreated,
            ValidationWarnings = warnings,
            Steps = stepResults.OrderBy(x => x.StepOrder).ToArray(),
            StartedAtUtc = run.StartedAtUtc,
            FinishedAtUtc = finish
        };
    }

    private static bool RequiresMarkerTable(TrainingDataOperationType operationType)
    {
        return operationType is TrainingDataOperationType.ResetTrainingOnly
            or TrainingDataOperationType.SeedFoundation
            or TrainingDataOperationType.SeedFullTraining
            or TrainingDataOperationType.DeleteTrainingData;
    }

    private async Task<bool> IsMarkerTableReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_dbContext.Database.IsRelational())
            {
                return true;
            }

            var provider = _dbContext.Database.ProviderName ?? string.Empty;
            if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var command = _dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TrainingDataMarkers'";
                if (command.Connection!.State != System.Data.ConnectionState.Open)
                {
                    await command.Connection.OpenAsync(cancellationToken);
                }

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is not null && result != DBNull.Value;
            }

            return true;
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(
                "TrainingData",
                "schema-probe",
                "MarkerTableProbeFailed",
                null,
                new { Error = ex.Message },
                _currentUserService.CurrentUser?.UserName ?? "system",
                cancellationToken: cancellationToken);
            return false;
        }
    }

    private async Task SafeAuditBlockedAsync(
        TrainingDataOperationRequest request,
        TrainingDataPreflightDto preflight,
        string reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var expectedConfirm = GetExpectedConfirmationText(request.OperationType);
            var isConfirmMatched = string.Equals((request.ConfirmationText ?? string.Empty).Trim(), expectedConfirm, StringComparison.Ordinal);
            await _auditService.LogAsync(
                "TrainingData",
                "blocked",
                "ExecuteBlocked",
                null,
                new
                {
                    request.OperationType,
                    request.SeedProfile,
                    request.ResetScope,
                    request.IsDryRun,
                    ExpectedConfirmation = expectedConfirm,
                    ConfirmationMatched = isConfirmMatched,
                    RiskLevel = preflight.RiskLevel,
                    preflight.HasRealDataRisk,
                    preflight.ProviderName,
                    preflight.EnvironmentName,
                    correlationId,
                    Reason = reason
                },
                _currentUserService.CurrentUser?.UserName ?? "system",
                correlationId: correlationId,
                cancellationToken: cancellationToken);
        }
        catch
        {
        }
    }

    public async Task<IReadOnlyList<TrainingDataOperationResult>> GetRunHistoryAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var runs = await _dbContext.TrainingDataRuns
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(Math.Max(1, take))
            .ToListAsync(cancellationToken);

        var runIds = runs.Select(x => x.Id).ToArray();
        var steps = await _dbContext.TrainingDataRunSteps.AsNoTracking()
            .Where(x => runIds.Contains(x.TrainingDataRunId))
            .OrderBy(x => x.StepOrder)
            .ToListAsync(cancellationToken);

        return runs.Select(run =>
        {
            Enum.TryParse<TrainingDataOperationType>(run.OperationType, out var op);
            TrainingDataSeedProfile? seed = null;
            if (!string.IsNullOrWhiteSpace(run.SeedProfile) && Enum.TryParse<TrainingDataSeedProfile>(run.SeedProfile, out var parsedSeed))
            {
                seed = parsedSeed;
            }

            var runSteps = steps.Where(s => s.TrainingDataRunId == run.Id).Select(s => new TrainingDataStepResult
            {
                StepOrder = s.StepOrder,
                StepCode = s.StepCode,
                StepName = s.StepName,
                EntityName = s.EntityName,
                Success = s.Success,
                BeforeCount = s.BeforeCount,
                DeletedCount = s.DeletedCount,
                AfterCount = s.AfterCount,
                CreatedCount = s.CreatedCount,
                Status = s.Status,
                Message = s.Message,
                ErrorMessage = s.ErrorMessage,
                StartedAtUtc = s.StartedAtUtc,
                FinishedAtUtc = s.FinishedAtUtc
            }).ToArray();

            return new TrainingDataOperationResult
            {
                RunId = run.Id,
                OperationType = op,
                SeedProfile = seed,
                ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
                IsDryRun = run.IsDryRun,
                Success = string.Equals(run.Status, "Success", StringComparison.OrdinalIgnoreCase),
                Message = run.Status,
                ErrorMessage = run.ErrorMessage,
                TotalDeletedCount = runSteps.Sum(x => x.DeletedCount),
                TotalCreatedCount = runSteps.Sum(x => x.CreatedCount),
                Steps = runSteps,
                StartedAtUtc = run.StartedAtUtc,
                FinishedAtUtc = run.FinishedAtUtc ?? run.StartedAtUtc
            };
        }).ToArray();
    }

    private void ValidateGuard(TrainingDataOperationRequest request, TrainingDataPreflightDto preflight)
    {
        var effectiveOptions = ResolveEffectiveOptions();
        var isDestructive = request.OperationType is TrainingDataOperationType.ClearBusinessData
            or TrainingDataOperationType.DeleteTrainingData
            or TrainingDataOperationType.ResetToBlank
            or TrainingDataOperationType.ResetTrainingOnly
            or TrainingDataOperationType.SeedFoundation
            or TrainingDataOperationType.SeedFullTraining;

        if (!isDestructive)
        {
            return;
        }

        if (!effectiveOptions.AllowDestructiveReset)
        {
            throw new InvalidOperationException("Thao tác destructive đang bị khóa bởi cấu hình TrainingData:AllowDestructiveReset=false.");
        }

        if (_hostEnvironment.IsProduction() && !effectiveOptions.AllowInProduction)
        {
            throw new InvalidOperationException("Không cho phép thao tác destructive trên môi trường Production.");
        }

        if (IsFullResetOperation(request.OperationType) && preflight.HasRealDataRisk && !effectiveOptions.AllowResetWithRealDataRisk)
        {
            throw new InvalidOperationException("Phát hiện rủi ro dữ liệu thật, full reset đang bị chặn. Bật TrainingData:AllowResetWithRealDataRisk=true để tiếp tục.");
        }

        var expected = GetExpectedConfirmationText(request.OperationType, effectiveOptions);
        if (!string.Equals((request.ConfirmationText ?? string.Empty).Trim(), expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Xác nhận không hợp lệ. Cần nhập đúng: {expected}");
        }
    }

    private string GetExpectedConfirmationText(TrainingDataOperationType operationType, TrainingDataOptions? options = null)
    {
        var effectiveOptions = options ?? ResolveEffectiveOptions();
        return operationType switch
        {
            TrainingDataOperationType.ResetToBlank or TrainingDataOperationType.ClearBusinessData or TrainingDataOperationType.DeleteTrainingData
                => "RESET ALL BUSINESS DATA",
            TrainingDataOperationType.ResetTrainingOnly
                => "RESET TRAINING DATA",
            _ => string.IsNullOrWhiteSpace(effectiveOptions.RequireConfirmationText) ? "RESET DATA" : effectiveOptions.RequireConfirmationText.Trim()
        };
    }

    private TrainingDataOptions ResolveEffectiveOptions()
    {
        var effective = new TrainingDataOptions
        {
            AllowDestructiveReset = _options.AllowDestructiveReset,
            AllowInProduction = _options.AllowInProduction,
            AllowResetWithRealDataRisk = _options.AllowResetWithRealDataRisk,
            RequireConfirmationText = _options.RequireConfirmationText
        };

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "Settings", "training-data.json");
            if (!File.Exists(path))
            {
                return effective;
            }

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("Values", out var values) || values.ValueKind != JsonValueKind.Object)
            {
                return effective;
            }

            if (values.TryGetProperty("allowDestructiveReset", out var allowDestructive))
            {
                effective.AllowDestructiveReset = ParseBool(allowDestructive, effective.AllowDestructiveReset);
            }
            if (values.TryGetProperty("allowInProduction", out var allowProd))
            {
                effective.AllowInProduction = ParseBool(allowProd, effective.AllowInProduction);
            }
            if (values.TryGetProperty("allowResetWithRealDataRisk", out var allowRisk))
            {
                effective.AllowResetWithRealDataRisk = ParseBool(allowRisk, effective.AllowResetWithRealDataRisk);
            }
            if (values.TryGetProperty("requireConfirmationText", out var confirm))
            {
                var parsed = confirm.ValueKind == JsonValueKind.String ? confirm.GetString() : confirm.ToString();
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    effective.RequireConfirmationText = parsed.Trim();
                }
            }
        }
        catch
        {
            // Keep base options when local module settings cannot be read.
        }

        return effective;
    }

    private static bool ParseBool(JsonElement value, bool fallback)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when value.TryGetInt32(out var n) => n != 0,
            _ => fallback
        };
    }

    private static string ResolveDetailedErrorMessage(Exception ex)
    {
        var messages = new List<string>();
        var current = ex;
        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                messages.Add(current.Message.Trim());
            }
            current = current.InnerException!;
        }

        return messages.Count == 0
            ? "Có lỗi khi xử lý dữ liệu mẫu."
            : string.Join(" | ", messages.Distinct(StringComparer.Ordinal));
    }

    private static string? TruncateForColumn(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static bool IsFullResetOperation(TrainingDataOperationType operationType)
    {
        return operationType is TrainingDataOperationType.ResetToBlank
            or TrainingDataOperationType.ClearBusinessData
            or TrainingDataOperationType.DeleteTrainingData;
    }

    private async Task SafeAuditAsync(
        TrainingDataRun run,
        TrainingDataOperationRequest request,
        TrainingDataPreflightDto preflight,
        bool success,
        string? error,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var expectedConfirm = GetExpectedConfirmationText(request.OperationType);
            var isConfirmMatched = string.Equals((request.ConfirmationText ?? string.Empty).Trim(), expectedConfirm, StringComparison.Ordinal);
            await _auditService.LogAsync(
                "TrainingData",
                run.Id.ToString(),
                success ? "ExecuteSuccess" : "ExecuteFailed",
                null,
                new
                {
                    request.OperationType,
                    request.SeedProfile,
                    request.ResetScope,
                    request.IsDryRun,
                    ExpectedConfirmation = expectedConfirm,
                    ConfirmationMatched = isConfirmMatched,
                    RiskLevel = preflight.RiskLevel,
                    preflight.HasRealDataRisk,
                    preflight.ProviderName,
                    preflight.EnvironmentName,
                    run.Status,
                    correlationId,
                    ErrorMessage = error
                },
                _currentUserService.CurrentUser?.UserName ?? "system",
                correlationId: correlationId,
                cancellationToken: cancellationToken);
        }
        catch
        {
        }
    }
}

public sealed class TrainingDataCleaner : ITrainingDataCleaner
{
    private readonly AppDbContext _dbContext;
    private const int FallbackBatchSize = 500;

    public TrainingDataCleaner(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TrainingDataStepResult>> ClearAsync(TrainingDataResetScope scope, bool isDryRun, CancellationToken cancellationToken = default)
    {
        var steps = new List<TrainingDataStepResult>();
        var stepOrder = 0;

        async Task AppendAsync<TEntity>(string code, string groupName, string entityName, Func<IQueryable<TEntity>> queryFactory) where TEntity : class
        {
            var result = await DeleteEntityStepAsync(code, groupName, entityName, queryFactory, isDryRun, ++stepOrder, cancellationToken);
            steps.Add(result);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? $"Xóa dữ liệu thất bại tại {entityName}.");
            }
        }

        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "StockIssueDetails", () => _dbContext.StockIssueDetails);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "StockIssues", () => _dbContext.StockIssues);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "StockReceiptDetails", () => _dbContext.StockReceiptDetails);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "StockReceipts", () => _dbContext.StockReceipts);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "PaymentAllocations", () => _dbContext.PaymentAllocations);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "PayableTransactions", () => _dbContext.PayableTransactions);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "ReceivableTransactions", () => _dbContext.ReceivableTransactions);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "CashTransactions", () => _dbContext.CashTransactions);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "BankTransactions", () => _dbContext.BankTransactions);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "VatTransactions", () => _dbContext.VatTransactions);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "ClosingEntryLines", () => _dbContext.ClosingEntryLines);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "ClosingEntries", () => _dbContext.ClosingEntries);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "PostingSources", () => _dbContext.PostingSources);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "JournalLines", () => _dbContext.JournalLines);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "JournalHeaders", () => _dbContext.JournalHeaders);
        await AppendAsync("DEL_TXN_01", "Xóa chứng từ và bút toán", "BusinessDocuments", () => _dbContext.BusinessDocuments);

        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "InventoryCostTransactions", () => _dbContext.InventoryCostTransactions);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "InventoryCostLayers", () => _dbContext.InventoryCostLayers);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "StockBalances", () => _dbContext.StockBalances);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "ToolAllocationRuns", () => _dbContext.ToolAllocationRuns);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "ToolAllocationSchedules", () => _dbContext.ToolAllocationSchedules);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "ToolRecoveries", () => _dbContext.ToolRecoveries);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "ToolDisposals", () => _dbContext.ToolDisposals);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "ToolIssues", () => _dbContext.ToolIssues);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "ToolAssets", () => _dbContext.ToolAssets);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "FixedAssetDisposals", () => _dbContext.FixedAssetDisposals);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "FixedAssetAdjustments", () => _dbContext.FixedAssetAdjustments);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "FixedAssetDepreciations", () => _dbContext.FixedAssetDepreciations);
        await AppendAsync("DEL_TXN_02", "Xóa dữ liệu kho, công cụ, tài sản", "FixedAssets", () => _dbContext.FixedAssets);

        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "Items", () => _dbContext.Items);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "Customers", () => _dbContext.Customers);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "Suppliers", () => _dbContext.Suppliers);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "Warehouses", () => _dbContext.Warehouses);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "People", () => _dbContext.People);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "Departments", () => _dbContext.Departments);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "CashFunds", () => _dbContext.CashFunds);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "BankAccounts", () => _dbContext.BankAccounts);
        await AppendAsync("DEL_MASTER_01", "Xóa danh mục nghiệp vụ", "TaxCodes", () => _dbContext.TaxCodes);

        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "CompanyFiscalPeriods", () => _dbContext.CompanyFiscalPeriods);
        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "BranchSettings", () => _dbContext.BranchSettings);
        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "CompanySettings", () => _dbContext.CompanySettings);
        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "CompanyFeatureFlags", () => _dbContext.CompanyFeatureFlags);
        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "ActiveCompanySessions", () => _dbContext.ActiveCompanySessions);
        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "Branches.NonDefault", () => _dbContext.Branches.Where(x => !x.IsDefault));
        await AppendAsync("DEL_SCOPE_01", "Xóa ngữ cảnh công ty/chi nhánh nghiệp vụ", "Companies.NonDefault", () => _dbContext.Companies.Where(x => !x.IsDefault));

        if (scope == TrainingDataResetScope.BusinessAndOperationalLogs)
        {
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "TaskAttachmentMetadata", () => _dbContext.TaskAttachmentMetadata);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "TaskComments", () => _dbContext.TaskComments);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "TaskInboxItems", () => _dbContext.TaskInboxItems);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "UserNotifications", () => _dbContext.UserNotifications);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "JobLogs", () => _dbContext.JobLogs);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "JobRetries", () => _dbContext.JobRetries);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "JobExecutions", () => _dbContext.JobExecutions);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "BackgroundJobLogs", () => _dbContext.BackgroundJobLogs);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "BackgroundJobExecutions", () => _dbContext.BackgroundJobExecutions);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "BackgroundJobSchedules", () => _dbContext.BackgroundJobSchedules);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "AuditEntitySnapshots", () => _dbContext.AuditEntitySnapshots);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "AuditReplayEntries", () => _dbContext.AuditReplayEntries);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "AuditCorrelations", () => _dbContext.AuditCorrelations);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "AuditSessions", () => _dbContext.AuditSessions);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "AuditLogs", () => _dbContext.AuditLogs);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "TelemetryEvents", () => _dbContext.TelemetryEvents);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "RuntimeMetricSnapshots", () => _dbContext.RuntimeMetricSnapshots);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "SlowOperationLogs", () => _dbContext.SlowOperationLogs);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "RuntimeExceptionLogs", () => _dbContext.RuntimeExceptionLogs);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "UiResponsivenessSnapshots", () => _dbContext.UiResponsivenessSnapshots);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "SystemResourceSnapshots", () => _dbContext.SystemResourceSnapshots);
            await AppendAsync("DEL_OP_01", "Xóa dữ liệu vận hành/audit/telemetry", "RecoveryRecommendations", () => _dbContext.RecoveryRecommendations);
        }

        return steps;
    }

    public async Task<IReadOnlyList<TrainingDataStepResult>> ClearTrainingOnlyAsync(TrainingDataSeedProfile? profile, bool isDryRun, CancellationToken cancellationToken = default)
    {
        var steps = new List<TrainingDataStepResult>();
        var markersQuery = _dbContext.TrainingDataMarkers.AsNoTracking().AsQueryable();
        if (profile.HasValue)
        {
            var profileText = profile.Value.ToString();
            markersQuery = markersQuery.Where(x => x.Profile == profileText);
        }

        var markers = await markersQuery.ToListAsync(cancellationToken);
        var stepOrder = 0;
        async Task AddAsync<TEntity>(string entityName, Func<List<string>, IQueryable<TEntity>> queryFactory) where TEntity : class
        {
            var ids = markers.Where(x => x.EntityName == entityName).Select(x => x.RecordId).Distinct().ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var result = await DeleteEntityStepAsync(
                "DEL_TRAINING_ONLY",
                "Xóa dữ liệu training có marker",
                entityName,
                () => queryFactory(ids),
                isDryRun,
                ++stepOrder,
                cancellationToken);
            steps.Add(result);
        }

        await AddAsync("StockIssueDetails", ids => _dbContext.StockIssueDetails.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("StockIssues", ids => _dbContext.StockIssues.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("StockReceiptDetails", ids => _dbContext.StockReceiptDetails.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("StockReceipts", ids => _dbContext.StockReceipts.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("PostingSources", ids => _dbContext.PostingSources.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("JournalLines", ids => _dbContext.JournalLines.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("JournalHeaders", ids => _dbContext.JournalHeaders.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("VatTransactions", ids => _dbContext.VatTransactions.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("ReceivableTransactions", ids => _dbContext.ReceivableTransactions.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("PayableTransactions", ids => _dbContext.PayableTransactions.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("InventoryCostTransactions", ids => _dbContext.InventoryCostTransactions.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("CashTransactions", ids => _dbContext.CashTransactions.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("BankTransactions", ids => _dbContext.BankTransactions.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("Customers", ids => _dbContext.Customers.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("Suppliers", ids => _dbContext.Suppliers.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("Items", ids => _dbContext.Items.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("Warehouses", ids => _dbContext.Warehouses.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("People", ids => _dbContext.People.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("CashFunds", ids => _dbContext.CashFunds.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("BankAccounts", ids => _dbContext.BankAccounts.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("TaxCodes", ids => _dbContext.TaxCodes.Where(x => ids.Contains(x.Id.ToString())));
        await AddAsync("Branches", ids => _dbContext.Branches.Where(x => ids.Contains(x.Id.ToString()) && !x.IsDefault));
        await AddAsync("Companies", ids => _dbContext.Companies.Where(x => ids.Contains(x.Id.ToString()) && !x.IsDefault));

        var markerStart = DateTime.UtcNow;
        var markerCount = markers.Count;
        if (!isDryRun)
        {
            _dbContext.TrainingDataMarkers.RemoveRange(_dbContext.TrainingDataMarkers.Where(x => markers.Select(m => m.Id).Contains(x.Id)));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        steps.Add(new TrainingDataStepResult
        {
            StepOrder = ++stepOrder,
            StepCode = "DEL_MARKERS",
            StepName = "Dọn marker training",
            EntityName = "TrainingDataMarkers",
            Success = true,
            BeforeCount = markerCount,
            DeletedCount = isDryRun ? 0 : markerCount,
            AfterCount = isDryRun ? markerCount : 0,
            Status = isDryRun ? "DryRun" : "Success",
            Message = isDryRun ? "Dry-run: không xóa marker." : $"Đã xóa {markerCount} marker.",
            StartedAtUtc = markerStart,
            FinishedAtUtc = DateTime.UtcNow
        });

        return steps;
    }

    private async Task<TrainingDataStepResult> DeleteEntityStepAsync<TEntity>(
        string stepCode,
        string stepName,
        string entityName,
        Func<IQueryable<TEntity>> queryFactory,
        bool isDryRun,
        int stepOrder,
        CancellationToken cancellationToken) where TEntity : class
    {
        var started = DateTime.UtcNow;
        var query = queryFactory().IgnoreQueryFilters();
        var beforeCount = await query.CountAsync(cancellationToken);

        if (isDryRun)
        {
            return new TrainingDataStepResult
            {
                StepOrder = stepOrder,
                StepCode = stepCode,
                StepName = stepName,
                EntityName = entityName,
                Success = true,
                BeforeCount = beforeCount,
                DeletedCount = 0,
                AfterCount = beforeCount,
                Status = "DryRun",
                Message = "Dry-run: không thay đổi dữ liệu.",
                StartedAtUtc = started,
                FinishedAtUtc = DateTime.UtcNow
            };
        }

        try
        {
            var deleted = await DeleteQueryWithFallbackAsync(query, cancellationToken);
            var afterCount = await query.CountAsync(cancellationToken);

            return new TrainingDataStepResult
            {
                StepOrder = stepOrder,
                StepCode = stepCode,
                StepName = stepName,
                EntityName = entityName,
                Success = true,
                BeforeCount = beforeCount,
                DeletedCount = deleted,
                AfterCount = afterCount,
                Status = "Success",
                Message = $"Đã xóa {deleted} bản ghi từ {entityName}.",
                StartedAtUtc = started,
                FinishedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new TrainingDataStepResult
            {
                StepOrder = stepOrder,
                StepCode = stepCode,
                StepName = stepName,
                EntityName = entityName,
                Success = false,
                BeforeCount = beforeCount,
                DeletedCount = 0,
                AfterCount = beforeCount,
                Status = "Failed",
                Message = $"Xóa dữ liệu thất bại tại {entityName}.",
                ErrorMessage = ex.Message,
                StartedAtUtc = started,
                FinishedAtUtc = DateTime.UtcNow
            };
        }
    }

    private async Task<int> DeleteQueryWithFallbackAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken)
        where TEntity : class
    {
        try
        {
            return await query.ExecuteDeleteAsync(cancellationToken);
        }
        catch (InvalidOperationException ex) when (NeedsDeleteFallback(ex))
        {
            return await BatchRemoveFallbackAsync(query, cancellationToken);
        }
        catch (NotSupportedException)
        {
            return await BatchRemoveFallbackAsync(query, cancellationToken);
        }
    }

    private async Task<int> BatchRemoveFallbackAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken)
        where TEntity : class
    {
        var deleted = 0;
        while (true)
        {
            var batch = await query.Take(FallbackBatchSize).ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            _dbContext.RemoveRange(batch);
            deleted += batch.Count;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return deleted;
    }

    private static bool NeedsDeleteFallback(InvalidOperationException ex)
    {
        var message = ex.Message;
        return message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase)
               || message.Contains("ExecuteDelete", StringComparison.OrdinalIgnoreCase)
               || message.Contains("translation", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class TrainingDataSeeder : ITrainingDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IAccountingStandardService _accountingStandardService;
    private readonly IInventoryAppService _inventoryAppService;
    private readonly IJournalService _journalService;
    private readonly ICashFundService _cashFundService;
    private readonly IBankingService _bankingService;
    private readonly ITaxService _taxService;
    private readonly IDebtTransactionService _debtTransactionService;

    public TrainingDataSeeder(
        AppDbContext dbContext,
        IAccountingStandardService accountingStandardService,
        IInventoryAppService inventoryAppService,
        IJournalService journalService,
        ICashFundService cashFundService,
        IBankingService bankingService,
        ITaxService taxService,
        IDebtTransactionService debtTransactionService)
    {
        _dbContext = dbContext;
        _accountingStandardService = accountingStandardService;
        _inventoryAppService = inventoryAppService;
        _journalService = journalService;
        _cashFundService = cashFundService;
        _bankingService = bankingService;
        _taxService = taxService;
        _debtTransactionService = debtTransactionService;
    }

    public async Task<IReadOnlyList<TrainingDataStepResult>> SeedAsync(TrainingDataSeedProfile profile, bool isDryRun, Guid? runId = null, CancellationToken cancellationToken = default)
    {
        profile = profile == TrainingDataSeedProfile.FullTraining
            ? TrainingDataSeedProfile.RestaurantHotelFullDemo
            : profile;
        var steps = new List<TrainingDataStepResult>();
        var step = 0;

        async Task AddStepAsync(string code, string name, Func<Task<int>> createAction)
        {
            var start = DateTime.UtcNow;
            var created = isDryRun ? 0 : await createAction();
            steps.Add(new TrainingDataStepResult
            {
                StepOrder = ++step,
                StepCode = code,
                StepName = name,
                Success = true,
                CreatedCount = created,
                Message = isDryRun ? "Dry-run: không ghi dữ liệu." : $"Đã tạo {created} bản ghi.",
                StartedAtUtc = start,
                FinishedAtUtc = DateTime.UtcNow
            });
        }

        await AddStepAsync("SEED_STD", "Đảm bảo chuẩn kế toán và mapping", async () =>
        {
            await _accountingStandardService.SeedStandardTemplatesAsync(cancellationToken);
            await _accountingStandardService.EnsureDefaultProfileAsync(cancellationToken);
            return 1;
        });

        if (profile is TrainingDataSeedProfile.FoundationOnly
            or TrainingDataSeedProfile.FullTraining
            or TrainingDataSeedProfile.AccountingDemo
            or TrainingDataSeedProfile.InventoryDemo
            or TrainingDataSeedProfile.NewCompanyFoundation
            or TrainingDataSeedProfile.NewCompanyFullTraining
            or TrainingDataSeedProfile.RestaurantDemo
            or TrainingDataSeedProfile.HotelDemo
            or TrainingDataSeedProfile.RestaurantHotelFullDemo)
        {
            await AddStepAsync("SEED_FOUNDATION", "Nạp dữ liệu nền chuẩn", async () =>
            {
                var created = await SeedFoundationCoreAsync();
                created += await SeedHospitalityFoundationAsync(profile);
                return created;
            });
        }

        if (profile is TrainingDataSeedProfile.FullTraining
            or TrainingDataSeedProfile.AccountingDemo
            or TrainingDataSeedProfile.InventoryDemo
            or TrainingDataSeedProfile.NewCompanyFullTraining
            or TrainingDataSeedProfile.RestaurantDemo
            or TrainingDataSeedProfile.HotelDemo
            or TrainingDataSeedProfile.RestaurantHotelFullDemo)
        {
            await AddStepAsync("SEED_TXN", "Nạp dữ liệu giao dịch mẫu", () => SeedTrainingTransactionsFullMatrixAsync(profile));
        }

        if (!isDryRun && runId.HasValue)
        {
            await TrackTrainingMarkersAsync(runId.Value, profile, cancellationToken);
        }

        return steps;
    }

    private async Task<int> SeedHospitalityFoundationAsync(TrainingDataSeedProfile profile)
    {
        var created = 0;
        var needRestaurant = profile is TrainingDataSeedProfile.RestaurantDemo or TrainingDataSeedProfile.RestaurantHotelFullDemo;
        var needHotel = profile is TrainingDataSeedProfile.HotelDemo or TrainingDataSeedProfile.RestaurantHotelFullDemo;

        if (needRestaurant)
        {
            if (!await _dbContext.Warehouses.AnyAsync(x => x.WarehouseCode == "KHO-BEP"))
            {
                _dbContext.Warehouses.AddRange(
                    new Warehouse { WarehouseCode = "KHO-TONG", WarehouseName = "Kho tổng", IsActive = true },
                    new Warehouse { WarehouseCode = "KHO-BEP", WarehouseName = "Kho bếp", IsActive = true },
                    new Warehouse { WarehouseCode = "KHO-BAR", WarehouseName = "Kho bar", IsActive = true });
                created += 3;
            }

            if (!await _dbContext.Suppliers.AnyAsync(x => x.SupplierCode == "SUP-TP"))
            {
                _dbContext.Suppliers.AddRange(
                    new Supplier { SupplierCode = "SUP-TP", SupplierName = "NCC thực phẩm", Phone = "0901000101", IsActive = true },
                    new Supplier { SupplierCode = "SUP-DU", SupplierName = "NCC đồ uống", Phone = "0901000102", IsActive = true },
                    new Supplier { SupplierCode = "SUP-VT", SupplierName = "NCC vật tư tiêu hao", Phone = "0901000103", IsActive = true });
                created += 3;
            }

            if (!await _dbContext.Items.AnyAsync(x => x.ItemCode == "RES-GAO"))
            {
                _dbContext.Items.AddRange(
                    new Item { ItemCode = "RES-GAO", ItemName = "Gạo", Unit = "Kg", GroupName = "Nguyên liệu", IsActive = true },
                    new Item { ItemCode = "RES-BO", ItemName = "Thịt bò", Unit = "Kg", GroupName = "Nguyên liệu", IsActive = true },
                    new Item { ItemCode = "RES-GA", ItemName = "Thịt gà", Unit = "Kg", GroupName = "Nguyên liệu", IsActive = true },
                    new Item { ItemCode = "RES-RAU", ItemName = "Rau củ", Unit = "Kg", GroupName = "Nguyên liệu", IsActive = true },
                    new Item { ItemCode = "RES-GV", ItemName = "Gia vị", Unit = "Gói", GroupName = "Nguyên liệu", IsActive = true },
                    new Item { ItemCode = "RES-BIA", ItemName = "Bia", Unit = "Lon", GroupName = "Đồ uống", IsActive = true },
                    new Item { ItemCode = "RES-NGO", ItemName = "Nước ngọt", Unit = "Lon", GroupName = "Đồ uống", IsActive = true },
                    new Item { ItemCode = "RES-KGI", ItemName = "Khăn giấy", Unit = "Gói", GroupName = "Vật tư tiêu hao", IsActive = true },
                    new Item { ItemCode = "RES-MON-CB", ItemName = "Cơm bò", Unit = "Phần", GroupName = "Món bán", IsActive = true },
                    new Item { ItemCode = "RES-MON-CG", ItemName = "Cơm gà", Unit = "Phần", GroupName = "Món bán", IsActive = true },
                    new Item { ItemCode = "RES-MON-LB", ItemName = "Lẩu bò", Unit = "Nồi", GroupName = "Món bán", IsActive = true },
                    new Item { ItemCode = "RES-MON-CB2", ItemName = "Combo gia đình", Unit = "Combo", GroupName = "Món bán", IsActive = true });
                created += 12;
            }
        }

        if (needHotel && !await _dbContext.Items.AnyAsync(x => x.ItemCode == "HOTEL-STD"))
        {
            _dbContext.Items.AddRange(
                new Item { ItemCode = "HOTEL-STD", ItemName = "Phòng Standard", Unit = "Đêm", GroupName = "Loại phòng", IsActive = true },
                new Item { ItemCode = "HOTEL-DLX", ItemName = "Phòng Deluxe", Unit = "Đêm", GroupName = "Loại phòng", IsActive = true },
                new Item { ItemCode = "HOTEL-SUI", ItemName = "Phòng Suite", Unit = "Đêm", GroupName = "Loại phòng", IsActive = true },
                new Item { ItemCode = "HOTEL-MINI", ItemName = "Dịch vụ Minibar", Unit = "Lần", GroupName = "Dịch vụ", IsActive = true },
                new Item { ItemCode = "HOTEL-LAUN", ItemName = "Dịch vụ Giặt là", Unit = "Lần", GroupName = "Dịch vụ", IsActive = true },
                new Item { ItemCode = "HOTEL-REST", ItemName = "Dịch vụ Nhà hàng", Unit = "Lần", GroupName = "Dịch vụ", IsActive = true },
                new Item { ItemCode = "HOTEL-SPA", ItemName = "Dịch vụ Spa", Unit = "Lần", GroupName = "Dịch vụ", IsActive = true },
                new Item { ItemCode = "HOTEL-AIR", ItemName = "Đưa đón sân bay", Unit = "Lần", GroupName = "Dịch vụ", IsActive = true });
            created += 8;
        }

        if (needHotel && !await _dbContext.Customers.AnyAsync(x => x.CustomerCode == "CUS-OTA"))
        {
            _dbContext.Customers.AddRange(
                new Customer { CustomerCode = "CUS-LE", CustomerName = "Khách lẻ", Phone = "0902000001", IsActive = true },
                new Customer { CustomerCode = "CUS-DOAN", CustomerName = "Khách đoàn", Phone = "0902000002", IsActive = true },
                new Customer { CustomerCode = "CUS-CORP", CustomerName = "Công ty đặt phòng", Phone = "0902000003", IsActive = true },
                new Customer { CustomerCode = "CUS-OTA", CustomerName = "OTA/Đại lý du lịch", Phone = "0902000004", IsActive = true });
            created += 4;
        }

        if (needRestaurant || needHotel)
        {
            created += await SeedHospitalityMenuRecipeAsync(needRestaurant, needHotel);
        }

        if (created > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        return created;
    }

    private async Task<int> SeedHospitalityMenuRecipeAsync(bool needRestaurant, bool needHotel)
    {
        var created = 0;
        var now = DateTime.UtcNow;
        if (!await _dbContext.MenuCategories.AnyAsync())
        {
            _dbContext.MenuCategories.AddRange(
                new MenuCategory { Code = "breakfast", Name = "Đồ ăn sáng", SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new MenuCategory { Code = "coffee", Name = "Cà phê", SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new MenuCategory { Code = "tea-drink", Name = "Trà & nước giải khát", SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new MenuCategory { Code = "combo", Name = "Combo", SortOrder = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new MenuCategory { Code = "hotel-service", Name = "Dịch vụ khách sạn", SortOrder = 5, IsActive = true, CreatedAt = now, UpdatedAt = now },
                new MenuCategory { Code = "others", Name = "Món khác", SortOrder = 6, IsActive = true, CreatedAt = now, UpdatedAt = now }
            );
            created += 6;
            await _dbContext.SaveChangesAsync();
        }

        var categories = await _dbContext.MenuCategories.ToDictionaryAsync(x => x.Code);
        var items = await _dbContext.Items.AsNoTracking().ToDictionaryAsync(x => x.ItemCode, x => x.Id);
        var warehouses = await _dbContext.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.WarehouseCode, x => x.Id);
        async Task<MenuItem> EnsureMenuAsync(string code, string name, string categoryCode, decimal price, int sortOrder, bool combo = false, bool hotel = false)
        {
            var existing = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Code == code);
            if (existing is not null)
            {
                return existing;
            }

            var menu = new MenuItem
            {
                Id = Guid.NewGuid(),
                MenuCategoryId = categories[categoryCode].Id,
                Code = code,
                Name = name,
                UnitName = "phần",
                UnitPrice = price,
                VatRate = 0.08m,
                HasRecipeManagement = true,
                IsCombo = combo,
                IsHotelService = hotel,
                IsActive = true,
                SortOrder = sortOrder,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.MenuItems.Add(menu);
            created++;
            return menu;
        }

        if (needRestaurant)
        {
            var cfSua = await EnsureMenuAsync("CF_SUA", "Cà phê sữa", "coffee", 25000, 1);
            var bmTrung = await EnsureMenuAsync("BM_TRUNG", "Bánh mì trứng", "breakfast", 20000, 2);
            await _dbContext.SaveChangesAsync();

            async Task EnsureRecipeAsync(MenuItem menuItem, params (string itemCode, decimal qty, decimal loss, string warehouseCode)[] rows)
            {
                var recipe = await _dbContext.MenuItemRecipes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.MenuItemId == menuItem.Id && x.IsDefault);
                if (recipe is null)
                {
                    recipe = new MenuItemRecipe
                    {
                        Id = Guid.NewGuid(),
                        MenuItemId = menuItem.Id,
                        Name = "Default",
                        IsDefault = true,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _dbContext.MenuItemRecipes.Add(recipe);
                    created++;
                }

                if (recipe.Lines.Count > 0)
                {
                    return;
                }

                var lineNo = 1;
                foreach (var row in rows)
                {
                    if (!items.TryGetValue(row.itemCode, out var itemId))
                    {
                        continue;
                    }

                    var ingredient = await _dbContext.Items.AsNoTracking().FirstAsync(x => x.Id == itemId);
                    recipe.Lines.Add(new MenuItemRecipeLine
                    {
                        Id = Guid.NewGuid(),
                        MenuItemRecipeId = recipe.Id,
                        LineNo = lineNo++,
                        ItemId = itemId,
                        ItemCode = ingredient.ItemCode,
                        ItemName = ingredient.ItemName,
                        UnitName = ingredient.Unit,
                        Quantity = row.qty,
                        LossRate = row.loss,
                        DefaultWarehouseId = warehouses.TryGetValue(row.warehouseCode, out var warehouseId) ? warehouseId : null,
                        IsRequired = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    created++;
                }
            }

            await EnsureRecipeAsync(cfSua,
                ("RES-GAO", 0.02m, 0.03m, "KHO-BAR"),
                ("RES-GV", 0.01m, 0.00m, "KHO-BAR"));
            await EnsureRecipeAsync(bmTrung,
                ("RES-MON-CB", 1.00m, 0.00m, "KHO-BEP"),
                ("RES-RAU", 0.05m, 0.02m, "KHO-BEP"));
        }

        if (needHotel)
        {
            await EnsureMenuAsync("HOTEL_BREAKFAST", "Dịch vụ khách sạn ăn sáng", "hotel-service", 150000, 1, hotel: true);
        }

        return created;
    }

    private async Task TrackTrainingMarkersAsync(Guid runId, TrainingDataSeedProfile profile, CancellationToken cancellationToken)
    {
        var markers = new List<TrainingDataMarker>();
        void Add(string entity, string recordId) => markers.Add(new TrainingDataMarker
        {
            TrainingDataRunId = runId,
            Profile = profile.ToString(),
            EntityName = entity,
            RecordId = recordId
        });

        foreach (var row in await _dbContext.Companies.AsNoTracking().Where(x => x.Code == "ASMILE-DEMO").ToListAsync(cancellationToken)) Add("Companies", row.Id.ToString());
        foreach (var row in await _dbContext.Branches.AsNoTracking().Where(x => x.Code.StartsWith("CN-")).ToListAsync(cancellationToken)) Add("Branches", row.Id.ToString());
        foreach (var row in await _dbContext.Customers.AsNoTracking().Where(x => x.CustomerCode.StartsWith("CUST00")).ToListAsync(cancellationToken)) Add("Customers", row.Id.ToString());
        foreach (var row in await _dbContext.Suppliers.AsNoTracking().Where(x => x.SupplierCode.StartsWith("SUP00")).ToListAsync(cancellationToken)) Add("Suppliers", row.Id.ToString());
        foreach (var row in await _dbContext.Items.AsNoTracking().Where(x => x.ItemCode.StartsWith("HH00")).ToListAsync(cancellationToken)) Add("Items", row.Id.ToString());
        foreach (var row in await _dbContext.Warehouses.AsNoTracking().Where(x => x.WarehouseCode.StartsWith("KHO-")).ToListAsync(cancellationToken)) Add("Warehouses", row.Id.ToString());
        foreach (var row in await _dbContext.People.AsNoTracking().Where(x => x.PersonCode.StartsWith("NV00")).ToListAsync(cancellationToken)) Add("People", row.Id.ToString());
        foreach (var row in await _dbContext.CashFunds.AsNoTracking().Where(x => x.FundCode.StartsWith("QUY-") || x.FundCode.StartsWith("TM-") || x.FundCode == "TMVND").ToListAsync(cancellationToken)) Add("CashFunds", row.Id.ToString());
        foreach (var row in await _dbContext.BankAccounts.AsNoTracking().Where(x => x.AccountNumber == "001100220033" || x.AccountNumber == "001100220044" || x.BankName.Contains("Vietcombank") || x.BankName.Contains("BIDV")).ToListAsync(cancellationToken)) Add("BankAccounts", row.Id.ToString());
        foreach (var row in await _dbContext.TaxCodes.AsNoTracking().Where(x => x.Code.StartsWith("VAT")).ToListAsync(cancellationToken)) Add("TaxCodes", row.Id.ToString());
        foreach (var row in await _dbContext.StockReceipts.AsNoTracking().Where(x => x.ReceiptNo.StartsWith("PN-TD-")).ToListAsync(cancellationToken)) Add("StockReceipts", row.Id.ToString());
        foreach (var row in await _dbContext.StockReceiptDetails.AsNoTracking()
                     .Where(x => _dbContext.StockReceipts.Any(h => h.Id == x.ReceiptId && h.ReceiptNo.StartsWith("PN-TD-"))).ToListAsync(cancellationToken)) Add("StockReceiptDetails", row.Id.ToString());
        foreach (var row in await _dbContext.StockIssues.AsNoTracking().Where(x => x.IssueNo.StartsWith("PX-TD-")).ToListAsync(cancellationToken)) Add("StockIssues", row.Id.ToString());
        foreach (var row in await _dbContext.StockIssueDetails.AsNoTracking()
                     .Where(x => _dbContext.StockIssues.Any(h => h.Id == x.IssueId && h.IssueNo.StartsWith("PX-TD-"))).ToListAsync(cancellationToken)) Add("StockIssueDetails", row.Id.ToString());
        foreach (var row in await _dbContext.JournalHeaders.AsNoTracking().Where(x => x.SourceType.StartsWith("TrainingData")).ToListAsync(cancellationToken)) Add("JournalHeaders", row.Id.ToString());
        foreach (var row in await _dbContext.JournalLines.AsNoTracking()
                     .Where(x => _dbContext.JournalHeaders.Any(h => h.Id == x.JournalHeaderId && h.SourceType.StartsWith("TrainingData"))).ToListAsync(cancellationToken)) Add("JournalLines", row.Id.ToString());
        foreach (var row in await _dbContext.CashTransactions.AsNoTracking()
                     .Where(x => (x.SourceType ?? string.Empty).StartsWith("TrainingData")
                                 || x.TransactionNo.Contains("TD-")
                                 || (x.Description ?? string.Empty).Contains("training")
                                 || (x.Description ?? string.Empty).Contains("công nợ khách hàng mẫu")
                                 || (x.CreatedBy ?? string.Empty).Contains("training-data")
                                 || (x.JournalHeaderId.HasValue
                                     && _dbContext.JournalHeaders.Any(h => h.Id == x.JournalHeaderId.Value && h.SourceType.StartsWith("TrainingData"))))
                     .ToListAsync(cancellationToken)) Add("CashTransactions", row.Id.ToString());
        foreach (var row in await _dbContext.BankTransactions.AsNoTracking()
                     .Where(x => (x.SourceType ?? string.Empty).StartsWith("TrainingData")
                                 || (x.ReferenceNo ?? string.Empty).Contains("TD")
                                 || x.TransactionNo.Contains("TD-")
                                 || (x.Description ?? string.Empty).Contains("training")
                                 || (x.Description ?? string.Empty).Contains("công nợ nhà cung cấp mẫu")
                                 || (x.CreatedBy ?? string.Empty).Contains("training-data")
                                 || (x.JournalHeaderId.HasValue
                                     && _dbContext.JournalHeaders.Any(h => h.Id == x.JournalHeaderId.Value && h.SourceType.StartsWith("TrainingData"))))
                     .ToListAsync(cancellationToken)) Add("BankTransactions", row.Id.ToString());
        foreach (var row in await _dbContext.ReceivableTransactions.AsNoTracking().Where(x => x.SourceType.StartsWith("TrainingData")).ToListAsync(cancellationToken)) Add("ReceivableTransactions", row.Id.ToString());
        foreach (var row in await _dbContext.PayableTransactions.AsNoTracking().Where(x => x.SourceType.StartsWith("TrainingData")).ToListAsync(cancellationToken)) Add("PayableTransactions", row.Id.ToString());
        foreach (var row in await _dbContext.VatTransactions.AsNoTracking().Where(x => x.SourceType.StartsWith("TrainingData")).ToListAsync(cancellationToken)) Add("VatTransactions", row.Id.ToString());
        foreach (var row in await _dbContext.InventoryCostTransactions.AsNoTracking().Where(x => x.DocumentNo.Contains("TD-")).ToListAsync(cancellationToken)) Add("InventoryCostTransactions", row.Id.ToString());

        if (markers.Count == 0)
        {
            return;
        }

        var existing = await _dbContext.TrainingDataMarkers.AsNoTracking()
            .Where(x => x.TrainingDataRunId == runId)
            .Select(x => new { x.EntityName, x.RecordId })
            .ToListAsync(cancellationToken);
        var existingSet = existing.Select(x => $"{x.EntityName}:{x.RecordId}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var insert = markers.Where(x => !existingSet.Contains($"{x.EntityName}:{x.RecordId}")).ToList();
        if (insert.Count > 0)
        {
            _dbContext.TrainingDataMarkers.AddRange(insert);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<int> SeedFoundationCoreAsync()
    {
        var created = 0;
        var fallbackDepartmentId = await EnsureDepartmentForTrainingAsync();
        created += await RepairInvalidTrainingPeopleReferencesAsync(fallbackDepartmentId);
        var cashAccountCode = await ResolvePostingAccountCodeAsync("1111", "111");
        var bankAccountCode = await ResolvePostingAccountCodeAsync("1121", "112");
        var vatInputAccountCode = await ResolvePostingAccountCodeAsync("1331", "133");
        var vatOutputAccountCode = await ResolvePostingAccountCodeAsync("33311", "3331", "333");

        if (!await _dbContext.Companies.AnyAsync(x => x.Name == "Công ty TNHH Demo ASMILE"))
        {
            _dbContext.Companies.Add(new Company
            {
                Id = Guid.NewGuid(),
                Code = "ASMILE-DEMO",
                Name = "Công ty TNHH Demo ASMILE",
                IsDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            created++;
        }

        if (!await _dbContext.Customers.AnyAsync(x => x.CustomerCode == "CUST001"))
        {
            _dbContext.Customers.AddRange(
                new Customer { CustomerCode = "CUST001", CustomerName = "Công ty CP Minh An", Phone = "0901000001", IsActive = true },
                new Customer { CustomerCode = "CUST002", CustomerName = "Công ty TNHH Sao Việt", Phone = "0901000002", IsActive = true });
            created += 2;
        }

        if (!await _dbContext.Suppliers.AnyAsync(x => x.SupplierCode == "SUP001"))
        {
            _dbContext.Suppliers.AddRange(
                new Supplier { SupplierCode = "SUP001", SupplierName = "Công ty TNHH Thiết Bị Nam Phát", Phone = "0911000001", IsActive = true },
                new Supplier { SupplierCode = "SUP002", SupplierName = "Công ty CP Vật Tư Đông Á", Phone = "0911000002", IsActive = true });
            created += 2;
        }

        if (!await _dbContext.Warehouses.AnyAsync(x => x.WarehouseCode == "KHO-CHINH"))
        {
            _dbContext.Warehouses.AddRange(
                new Warehouse { WarehouseCode = "KHO-CHINH", WarehouseName = "Kho chính", IsActive = true },
                new Warehouse { WarehouseCode = "KHO-BAN", WarehouseName = "Kho bán hàng", IsActive = true });
            created += 2;
        }

        if (!await _dbContext.Items.AnyAsync(x => x.ItemCode == "HH001"))
        {
            _dbContext.Items.AddRange(
                new Item { ItemCode = "HH001", ItemName = "Máy in mã vạch", Unit = "Cái", GroupName = "Thiết bị", IsActive = true },
                new Item { ItemCode = "HH002", ItemName = "Giấy in hóa đơn", Unit = "Cuộn", GroupName = "Vật tư", IsActive = true },
                new Item { ItemCode = "HH003", ItemName = "Máy quét mã vạch", Unit = "Cái", GroupName = "Thiết bị", IsActive = true });
            created += 3;
        }

        if (!await _dbContext.People.AnyAsync(x => x.PersonCode == "NV001"))
        {
            _dbContext.People.Add(new Person
            {
                PersonCode = "NV001",
                FullName = "Nguyễn Văn Quản trị",
                DepartmentId = fallbackDepartmentId,
                Position = "Kế toán trưởng",
                Phone = "0988000001",
                IsActive = true
            });
            created++;
        }

        if (!await _dbContext.TaxCodes.AnyAsync(x => x.Code == "VAT10"))
        {
            _dbContext.TaxCodes.AddRange(
                new TaxCode { Id = Guid.NewGuid(), Code = "VAT0", Name = "VAT 0%", TaxRate = 0m, TaxType = "VAT", InputAccountCode = vatInputAccountCode, OutputAccountCode = vatOutputAccountCode, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TaxCode { Id = Guid.NewGuid(), Code = "VAT5", Name = "VAT 5%", TaxRate = 0.05m, TaxType = "VAT", InputAccountCode = vatInputAccountCode, OutputAccountCode = vatOutputAccountCode, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TaxCode { Id = Guid.NewGuid(), Code = "VAT8", Name = "VAT 8%", TaxRate = 0.08m, TaxType = "VAT", InputAccountCode = vatInputAccountCode, OutputAccountCode = vatOutputAccountCode, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new TaxCode { Id = Guid.NewGuid(), Code = "VAT10", Name = "VAT 10%", TaxRate = 0.10m, TaxType = "VAT", InputAccountCode = vatInputAccountCode, OutputAccountCode = vatOutputAccountCode, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            created += 4;
        }

        await _dbContext.SaveChangesAsync();

        var funds = await _cashFundService.GetFundsAsync();
        if (!funds.Any(x => x.FundCode == "TMVND"))
        {
            await _cashFundService.CreateFundAsync(new CashFundRequest
            {
                FundCode = "TMVND",
                FundName = "Quỹ tiền mặt VND",
                CurrencyCode = "VND",
                AccountCode = cashAccountCode,
                OpeningBalance = 200_000_000m,
                IsDefault = true
            });
            created++;
        }

        var accounts = await _bankingService.GetAccountsAsync();
        if (!accounts.Any(x => x.AccountNumber == "001100220033"))
        {
            await _bankingService.CreateAccountAsync(new BankAccountRequest
            {
                BankCode = "VCB",
                BankName = "Vietcombank",
                AccountNumber = "001100220033",
                AccountName = "Công ty TNHH Demo ASMILE",
                CurrencyCode = "VND",
                AccountCode = bankAccountCode,
                OpeningBalance = 500_000_000m,
                IsDefault = true
            });
            created++;
        }

        if (!accounts.Any(x => x.AccountNumber == "001100220044"))
        {
            await _bankingService.CreateAccountAsync(new BankAccountRequest
            {
                BankCode = "BIDV",
                BankName = "BIDV",
                AccountNumber = "001100220044",
                AccountName = "Công ty TNHH Demo ASMILE",
                CurrencyCode = "VND",
                AccountCode = bankAccountCode,
                OpeningBalance = 300_000_000m,
                IsDefault = false
            });
            created++;
        }

        return created;
    }

    private async Task<string> ResolvePostingAccountCodeAsync(params string[] candidates)
    {
        var normalized = candidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var candidate in normalized)
        {
            var exact = await _dbContext.LedgerAccounts.AsNoTracking()
                .Where(x => x.AccountCode == candidate && x.AllowPosting && x.IsActive)
                .Select(x => x.AccountCode)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(exact))
            {
                return exact;
            }

            var leaf = await _dbContext.LedgerAccounts.AsNoTracking()
                .Where(x => x.AccountCode.StartsWith(candidate) && x.AllowPosting && x.IsActive)
                .OrderBy(x => x.AccountCode.Length)
                .ThenBy(x => x.AccountCode)
                .Select(x => x.AccountCode)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(leaf))
            {
                return leaf;
            }
        }

        throw new InvalidOperationException($"Không tìm thấy tài khoản cho phép hạch toán trực tiếp cho nhóm: {string.Join(", ", normalized)}");
    }

    private async Task<int> EnsureDepartmentForTrainingAsync()
    {
        var dept = await _dbContext.Departments
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.DepartmentCode == "HCKT");
        if (dept is not null)
        {
            return dept.Id;
        }

        dept = await _dbContext.Departments
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(x => x.IsActive);
        if (dept is not null)
        {
            return dept.Id;
        }

        var inserted = new Department
        {
            DepartmentCode = "HCKT",
            DepartmentName = "Hành chính - Kế toán",
            Level = 1,
            Note = "Tạo bởi TrainingData Seeder",
            IsActive = true
        };
        _dbContext.Departments.Add(inserted);
        await _dbContext.SaveChangesAsync();
        return inserted.Id;
    }

    private async Task<int> RepairInvalidTrainingPeopleReferencesAsync(int fallbackDepartmentId)
    {
        var departmentIds = await _dbContext.Departments
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync();
        var validDepartmentIds = departmentIds.ToHashSet();

        var candidates = await _dbContext.People
            .Where(x => x.PersonCode.StartsWith("NV00") || x.PersonCode.StartsWith("NS-"))
            .ToListAsync();
        var repaired = 0;
        foreach (var person in candidates)
        {
            if (person.DepartmentId <= 0 || !validDepartmentIds.Contains(person.DepartmentId))
            {
                person.DepartmentId = fallbackDepartmentId;
                repaired++;
            }
        }

        return repaired;
    }

    private async Task<int> SeedTrainingTransactionsAsync()
    {
        var created = 0;
        var receivableCounterAccount = await ResolvePostingAccountCodeAsync("1311", "131");
        var payableCounterAccount = await ResolvePostingAccountCodeAsync("3311", "331");
        var funds = await _cashFundService.GetFundsAsync();
        var fund = funds.FirstOrDefault();
        var accounts = await _bankingService.GetAccountsAsync();
        var bank = accounts.FirstOrDefault();
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync();
        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync();

        if (fund is not null && customer is not null
            && !await _dbContext.CashTransactions.AnyAsync(x => x.SourceType == "TrainingDataCashReceipt"))
        {
            var receipt = await _cashFundService.CreateReceiptAsync(new CashTransactionRequest
            {
                FundId = fund.Id,
                TransactionDate = DateTime.Today.AddDays(-1),
                Amount = 55_000_000m,
                Description = "Thu tiền công nợ khách hàng mẫu",
                PartnerType = "Customer",
                PartnerId = PostingSourceIds.FromInt32(customer.Id),
                CounterAccountCode = receivableCounterAccount,
                SourceType = "TrainingDataCashReceipt",
                CreatedBy = "training-data"
            });
            await _cashFundService.PostTransactionAsync(receipt.Id, "training-data");
            created++;
        }

        if (bank is not null && supplier is not null
            && !await _dbContext.BankTransactions.AnyAsync(x => x.SourceType == "TrainingDataBankPayment"))
        {
            var payment = await _bankingService.CreatePaymentAsync(new BankTransactionRequest
            {
                BankAccountId = bank.Id,
                TransactionDate = DateTime.Today.AddDays(-1),
                Amount = 35_000_000m,
                ReferenceNo = "TD-BANK-PAYMENT",
                Description = "Chi trả công nợ nhà cung cấp mẫu",
                PartnerType = "Supplier",
                PartnerId = PostingSourceIds.FromInt32(supplier.Id),
                CounterAccountCode = payableCounterAccount,
                SourceType = "TrainingDataBankPayment",
                CreatedBy = "training-data"
            });
            await _bankingService.PostTransactionAsync(payment.Id, "training-data");
            created++;
        }

        return created;
    }

    private async Task<int> SeedTrainingTransactionsFullMatrixAsync(TrainingDataSeedProfile profile)
    {
        var created = await SeedTrainingTransactionsAsync();
        var today = DateTime.Today;
        var cashAccountCode = await ResolvePostingAccountCodeAsync("1111", "111");
        var bankAccountCode = await ResolvePostingAccountCodeAsync("1121", "112");
        var arAccountCode = await ResolvePostingAccountCodeAsync("1311", "131");
        var apAccountCode = await ResolvePostingAccountCodeAsync("3311", "331");
        var inventoryAccountCode = await ResolvePostingAccountCodeAsync("1561", "156");
        var cogsAccountCode = await ResolvePostingAccountCodeAsync("6321", "632");
        var revenueAccountCode = await ResolvePostingAccountCodeAsync("5111", "511");
        var vatOutputAccountCode = await ResolvePostingAccountCodeAsync("33311", "3331", "333");
        var expenseAccountCode = await ResolvePostingAccountCodeAsync("6421", "642");
        var vatInputAccountCode = await ResolvePostingAccountCodeAsync("1331", "133");
        await EnsurePostingRoleMappingAsync("Cash", cashAccountCode);
        await EnsurePostingRoleMappingAsync("Bank", bankAccountCode);
        await EnsurePostingRoleMappingAsync("Receivable", arAccountCode);
        await EnsurePostingRoleMappingAsync("Payable", apAccountCode);
        await EnsurePostingRoleMappingAsync("Inventory", inventoryAccountCode);
        await EnsurePostingRoleMappingAsync("COGS", cogsAccountCode);
        await EnsurePostingRoleMappingAsync("Revenue", revenueAccountCode);
        await EnsurePostingRoleMappingAsync("VATOutput", vatOutputAccountCode);
        await EnsurePostingRoleMappingAsync("Expense", expenseAccountCode);
        await EnsurePostingRoleMappingAsync("VATInput", vatInputAccountCode);
        var periodCode = await _dbContext.AccountingPeriods.AsNoTracking()
            .Where(x => x.FromDate <= today && x.ToDate >= today)
            .Select(x => x.PeriodCode)
            .FirstOrDefaultAsync()
            ?? await _dbContext.AccountingPeriods.AsNoTracking().OrderByDescending(x => x.ToDate).Select(x => x.PeriodCode).FirstOrDefaultAsync()
            ?? "2026-05";

        var warehouse = await _dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync();
        var item1 = await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemCode == "HH001");
        var item2 = await _dbContext.Items.AsNoTracking().FirstOrDefaultAsync(x => x.ItemCode == "HH002");
        var supplier = await _dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync();
        var customer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync();
        var vat10 = await _dbContext.TaxCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "VAT10");
        var accountNames = await _dbContext.LedgerAccounts.AsNoTracking().ToDictionaryAsync(x => x.AccountCode, x => x.AccountName);
        var receiptNo = $"PN-TD-{today:yyyyMMdd}";
        var issueNo = $"PX-TD-{today:yyyyMMdd}";
        var hotelJournalNo = $"JV-HOTEL-{today:yyyyMMdd}";
        var salesJournalNo = $"JV-BAN-{today:yyyyMMdd}";
        var expenseJournalNo = $"JV-CP-{today:yyyyMMdd}";

        if (profile is TrainingDataSeedProfile.RestaurantDemo or TrainingDataSeedProfile.RestaurantHotelFullDemo
            && warehouse is not null && item1 is not null && item2 is not null)
        {
            if (!await _dbContext.StockReceipts.AnyAsync(x => x.ReceiptNo == receiptNo))
            {
                var receipt = await _inventoryAppService.SaveReceiptAsync(new StockReceiptDto
                {
                    ReceiptNo = receiptNo,
                    ReceiptDate = today.AddDays(-5),
                    SupplierCode = supplier?.SupplierCode ?? string.Empty,
                    SupplierName = supplier?.SupplierName ?? "Nha cung cap mau",
                    ReceiverName = "Kho Demo",
                    Description = "Nhap hang training",
                    WarehouseId = warehouse.Id,
                    WarehouseCode = warehouse.WarehouseCode,
                    DebitAccountCode = inventoryAccountCode,
                    CreditAccountCode = apAccountCode,
                    Details =
                    [
                        new StockTransactionDetailDto { LineNo = 1, ItemId = item1.Id, ItemCode = item1.ItemCode, ItemName = item1.ItemName, Unit = item1.Unit, WarehouseId = warehouse.Id, WarehouseCode = warehouse.WarehouseCode, Quantity = 20m, UnitPrice = 2_000_000m, Amount = 40_000_000m },
                        new StockTransactionDetailDto { LineNo = 2, ItemId = item2.Id, ItemCode = item2.ItemCode, ItemName = item2.ItemName, Unit = item2.Unit, WarehouseId = warehouse.Id, WarehouseCode = warehouse.WarehouseCode, Quantity = 200m, UnitPrice = 50_000m, Amount = 10_000_000m }
                    ]
                });
                await _inventoryAppService.ApproveReceiptAndPostAsync(receipt.Id, "training-data");
                created++;
            }

            if (!await _dbContext.StockIssues.AnyAsync(x => x.IssueNo == issueNo))
            {
                var issue = await _inventoryAppService.SaveIssueAsync(new StockIssueDto
                {
                    IssueNo = issueNo,
                    IssueDate = today.AddDays(-4),
                    Description = "Xuat ban training",
                    WarehouseId = warehouse.Id,
                    WarehouseCode = warehouse.WarehouseCode,
                    DepartmentCode = "BP01",
                    DebitAccountCode = cogsAccountCode,
                    CreditAccountCode = inventoryAccountCode,
                    Details =
                    [
                        new StockTransactionDetailDto { LineNo = 1, ItemId = item1.Id, ItemCode = item1.ItemCode, ItemName = item1.ItemName, Unit = item1.Unit, WarehouseId = warehouse.Id, WarehouseCode = warehouse.WarehouseCode, Quantity = 4m },
                        new StockTransactionDetailDto { LineNo = 2, ItemId = item2.Id, ItemCode = item2.ItemCode, ItemName = item2.ItemName, Unit = item2.Unit, WarehouseId = warehouse.Id, WarehouseCode = warehouse.WarehouseCode, Quantity = 40m }
                    ]
                });
                await _inventoryAppService.ApproveIssueAndPostAsync(issue.Id, "training-data");
                created++;
            }
        }

        if (profile is TrainingDataSeedProfile.HotelDemo or TrainingDataSeedProfile.RestaurantHotelFullDemo)
        {
            var hotelCustomer = await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerCode == "CUST002")
                               ?? await _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync();
            if (hotelCustomer is not null && !await _dbContext.JournalHeaders.AnyAsync(x => x.JournalNo == hotelJournalNo))
            {
                var hotelCustomerId = PostingSourceIds.FromInt32(hotelCustomer.Id);
                var hotelJournal = await _journalService.CreateDraftAsync(new JournalHeader
                {
                    JournalNo = hotelJournalNo,
                    JournalDate = today.AddDays(-2),
                    PostingDate = today.AddDays(-2),
                    PeriodCode = periodCode,
                    SourceType = "TrainingDataHotel",
                    SourceNo = $"TD-HOTEL-{today:yyyyMMdd}",
                    Description = "Doanh thu phòng và dịch vụ khách sạn",
                    CreatedBy = "training-data",
                    Lines =
                    [
                        new JournalLine { LineNo = 1, AccountCode = arAccountCode, DebitAmount = 44_000_000m, CreditAmount = 0m, CustomerId = hotelCustomerId, Description = "Ghi nhận phải thu khách lưu trú" },
                        new JournalLine { LineNo = 2, AccountCode = revenueAccountCode, DebitAmount = 0m, CreditAmount = 40_000_000m, Description = "Doanh thu lưu trú" },
                        new JournalLine { LineNo = 3, AccountCode = vatOutputAccountCode, DebitAmount = 0m, CreditAmount = 4_000_000m, Description = "VAT đầu ra dịch vụ khách sạn" }
                    ]
                });
                await _journalService.PostDraftAsync(hotelJournal.Id, "training-data");
                created++;
            }
        }

        if (customer is not null && !await _dbContext.JournalHeaders.AnyAsync(x => x.JournalNo == salesJournalNo))
        {
            var customerId = PostingSourceIds.FromInt32(customer.Id);
            var sales = await _journalService.CreateDraftAsync(new JournalHeader
            {
                Id = Guid.NewGuid(),
                JournalNo = salesJournalNo,
                JournalDate = today.AddDays(-3),
                PostingDate = today.AddDays(-3),
                SourceType = "TrainingDataSales",
                SourceNo = $"TD-SALE-{today:yyyyMMdd}",
                Description = "Ban hang training",
                PeriodCode = periodCode,
                CreatedBy = "training-data",
                CreatedAt = DateTime.UtcNow,
                Lines =
                [
                    new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = arAccountCode, AccountName = accountNames.GetValueOrDefault(arAccountCode, arAccountCode), DebitAmount = 33_000_000m, CustomerId = customerId },
                    new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = revenueAccountCode, AccountName = accountNames.GetValueOrDefault(revenueAccountCode, revenueAccountCode), CreditAmount = 30_000_000m },
                    new JournalLine { Id = Guid.NewGuid(), LineNo = 3, AccountCode = vatOutputAccountCode, AccountName = accountNames.GetValueOrDefault(vatOutputAccountCode, vatOutputAccountCode), CreditAmount = 3_000_000m }
                ]
            });
            var postedSales = await _journalService.PostDraftAsync(sales.Id, "training-data");
            await _debtTransactionService.SyncFromJournalAsync(postedSales);
            await _taxService.SyncVatFromJournalAsync(postedSales);
            if (vat10 is not null)
            {
                await _taxService.CreateVatTransactionAsync(new CreateVatTransactionRequest
                {
                    TaxCodeId = vat10.Id,
                    SourceType = postedSales.SourceType,
                    SourceId = postedSales.SourceId,
                    JournalHeaderId = postedSales.Id,
                    InvoiceNo = postedSales.SourceNo,
                    InvoiceDate = postedSales.PostingDate,
                    PartnerType = "Customer",
                    PartnerId = customerId,
                    TaxBaseAmount = 30_000_000m,
                    TaxAmount = 3_000_000m,
                    Direction = "Output",
                    DeclarationPeriod = postedSales.PeriodCode
                });
            }
            created++;
        }

        if (supplier is not null && !await _dbContext.JournalHeaders.AnyAsync(x => x.JournalNo == expenseJournalNo))
        {
            var supplierId = PostingSourceIds.FromInt32(supplier.Id);
            var expense = await _journalService.CreateDraftAsync(new JournalHeader
            {
                Id = Guid.NewGuid(),
                JournalNo = expenseJournalNo,
                JournalDate = today.AddDays(-2),
                PostingDate = today.AddDays(-2),
                SourceType = "TrainingDataExpense",
                SourceNo = $"TD-EXP-{today:yyyyMMdd}",
                Description = "Chi phi training",
                PeriodCode = periodCode,
                CreatedBy = "training-data",
                CreatedAt = DateTime.UtcNow,
                Lines =
                [
                    new JournalLine { Id = Guid.NewGuid(), LineNo = 1, AccountCode = expenseAccountCode, AccountName = accountNames.GetValueOrDefault(expenseAccountCode, expenseAccountCode), DebitAmount = 12_000_000m },
                    new JournalLine { Id = Guid.NewGuid(), LineNo = 2, AccountCode = vatInputAccountCode, AccountName = accountNames.GetValueOrDefault(vatInputAccountCode, vatInputAccountCode), DebitAmount = 1_200_000m },
                    new JournalLine { Id = Guid.NewGuid(), LineNo = 3, AccountCode = apAccountCode, AccountName = accountNames.GetValueOrDefault(apAccountCode, apAccountCode), CreditAmount = 13_200_000m, SupplierId = supplierId }
                ]
            });
            var postedExpense = await _journalService.PostDraftAsync(expense.Id, "training-data");
            await _debtTransactionService.SyncFromJournalAsync(postedExpense);
            await _taxService.SyncVatFromJournalAsync(postedExpense);
            if (vat10 is not null)
            {
                await _taxService.CreateVatTransactionAsync(new CreateVatTransactionRequest
                {
                    TaxCodeId = vat10.Id,
                    SourceType = postedExpense.SourceType,
                    SourceId = postedExpense.SourceId,
                    JournalHeaderId = postedExpense.Id,
                    InvoiceNo = postedExpense.SourceNo,
                    InvoiceDate = postedExpense.PostingDate,
                    PartnerType = "Supplier",
                    PartnerId = supplierId,
                    TaxBaseAmount = 12_000_000m,
                    TaxAmount = 1_200_000m,
                    Direction = "Input",
                    DeclarationPeriod = postedExpense.PeriodCode
                });
            }
            created++;
        }

        return created;
    }

    private async Task EnsurePostingRoleMappingAsync(string roleCode, string accountCode)
    {
        var standardCode = await _dbContext.AccountingStandards.AsNoTracking()
            .Where(x => x.IsDefault && x.IsActive)
            .Select(x => x.Code)
            .FirstOrDefaultAsync()
            ?? await _dbContext.AccountingStandards.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .Select(x => x.Code)
                .FirstOrDefaultAsync()
            ?? "TT99";

        var accountIsPosting = await _dbContext.LedgerAccounts.AsNoTracking()
            .AnyAsync(x => x.AccountCode == accountCode && x.AllowPosting && x.IsActive);
        if (!accountIsPosting)
        {
            throw new InvalidOperationException($"Tài khoản {accountCode} không hợp lệ cho vai trò {roleCode}.");
        }

        var mappings = await _dbContext.AccountRoleMappings
            .Where(x => x.AccountingStandardCode == standardCode && x.RoleCode == roleCode && x.IsActive && x.IsDefault)
            .ToListAsync();

        var validDefaultExists = await _dbContext.AccountRoleMappings.AsNoTracking()
            .Where(x => x.AccountingStandardCode == standardCode && x.RoleCode == roleCode && x.IsActive && x.IsDefault)
            .Join(_dbContext.LedgerAccounts.AsNoTracking(),
                mapping => mapping.AccountCode,
                account => account.AccountCode,
                (mapping, account) => account)
            .AnyAsync(x => x.AllowPosting && x.IsActive);
        if (validDefaultExists)
        {
            return;
        }

        foreach (var mapping in mappings)
        {
            mapping.IsDefault = false;
        }

        var target = await _dbContext.AccountRoleMappings
            .FirstOrDefaultAsync(x => x.AccountingStandardCode == standardCode && x.RoleCode == roleCode && x.AccountCode == accountCode);
        if (target is null)
        {
            _dbContext.AccountRoleMappings.Add(new AccountRoleMapping
            {
                Id = Guid.NewGuid(),
                AccountingStandardCode = standardCode,
                RoleCode = roleCode,
                AccountCode = accountCode,
                IsDefault = true,
                IsActive = true
            });
        }
        else
        {
            target.IsDefault = true;
            target.IsActive = true;
        }

        await _dbContext.SaveChangesAsync();
    }
}

public sealed class TrainingDataValidator : ITrainingDataValidator
{
    private readonly AppDbContext _dbContext;

    public TrainingDataValidator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        var unbalancedJournals = await _dbContext.JournalHeaders
            .AsNoTracking()
            .Where(x => Math.Abs(x.TotalDebit - x.TotalCredit) > 0.0001m)
            .CountAsync(cancellationToken);
        if (unbalancedJournals > 0)
        {
            warnings.Add($"Có {unbalancedJournals} nhật ký chưa cân Nợ/Có.");
        }

        var nonPostingAccountLines = await _dbContext.JournalLines
            .AsNoTracking()
            .Join(_dbContext.LedgerAccounts.AsNoTracking(), jl => jl.AccountCode, la => la.AccountCode, (jl, la) => new { jl, la })
            .Where(x => !x.la.AllowPosting)
            .CountAsync(cancellationToken);
        if (nonPostingAccountLines > 0)
        {
            warnings.Add($"Có {nonPostingAccountLines} dòng nhật ký dùng tài khoản tổng hợp không cho phép hạch toán.");
        }

        var lockedPeriodEntries = await _dbContext.JournalHeaders
            .AsNoTracking()
            .Join(_dbContext.PeriodLocks.AsNoTracking().Where(x => x.IsLocked), j => j.PeriodCode, p => p.PeriodCode, (j, p) => j.Id)
            .CountAsync(cancellationToken);
        if (lockedPeriodEntries > 0)
        {
            warnings.Add($"Có {lockedPeriodEntries} bút toán nằm trong kỳ đã khóa.");
        }
        var invalidReceivablePartner = await _dbContext.ReceivableTransactions.AsNoTracking().CountAsync(x => x.CustomerId == Guid.Empty, cancellationToken);
        if (invalidReceivablePartner > 0)
        {
            warnings.Add($"Có {invalidReceivablePartner} giao dịch công nợ phải thu thiếu CustomerId.");
        }

        var invalidPayablePartner = await _dbContext.PayableTransactions.AsNoTracking().CountAsync(x => x.SupplierId == Guid.Empty, cancellationToken);
        if (invalidPayablePartner > 0)
        {
            warnings.Add($"Có {invalidPayablePartner} giao dịch công nợ phải trả thiếu SupplierId.");
        }

        var vatInputCount = await _dbContext.VatTransactions.AsNoTracking().CountAsync(x => x.Direction == "Input", cancellationToken);
        var vatOutputCount = await _dbContext.VatTransactions.AsNoTracking().CountAsync(x => x.Direction == "Output", cancellationToken);
        if (vatInputCount == 0 || vatOutputCount == 0)
        {
            warnings.Add("Thiếu giao dịch VAT đầu vào hoặc VAT đầu ra sau seed.");
        }

        var issueCount = await _dbContext.StockIssues.AsNoTracking().CountAsync(cancellationToken);
        var issueCostCount = await _dbContext.InventoryCostTransactions.AsNoTracking().CountAsync(x => x.SourceType == "StockIssue", cancellationToken);
        if (issueCount > 0 && issueCostCount == 0)
        {
            warnings.Add("Chưa có InventoryCostTransaction cho phiếu xuất.");
        }

        var negativeStockCount = await _dbContext.StockBalances.AsNoTracking().CountAsync(x => x.ClosingQty < 0m, cancellationToken);
        if (negativeStockCount > 0)
        {
            warnings.Add($"Có {negativeStockCount} dòng tồn kho âm.");
        }

        var ledgerDataCount = await _dbContext.JournalLines.AsNoTracking().CountAsync(cancellationToken);
        if (ledgerDataCount == 0)
        {
            warnings.Add("So cai chua co du lieu journal.");
        }
        return warnings;
    }
}




