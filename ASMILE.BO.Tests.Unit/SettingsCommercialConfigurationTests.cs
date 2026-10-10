using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.ViewModels.Settings;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Models;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Services;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.SettingsEngine;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.Reflection;

namespace ASMILE.BO.Tests.Unit;

public sealed class SettingsCommercialConfigurationTests
{
    [Fact]
    public void App_module_catalog_does_not_seed_a_fabricated_version()
    {
        new SettingsRegistry().GetDefaultApps().Should().OnlyContain(x => x.Version == null);

        var versionMethod = typeof(SettingsViewModel).GetMethod("GetRunningProductVersion", BindingFlags.NonPublic | BindingFlags.Static);
        versionMethod.Should().NotBeNull();
        versionMethod!.Invoke(null, null).Should().Be(Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Chưa xác định");
    }

    [Fact]
    public void Settings_home_health_summary_surfaces_warnings_without_leaking_error_details()
    {
        var formatter = typeof(SettingsViewModel).GetMethod("FormatHomeHealthStatus", BindingFlags.NonPublic | BindingFlags.Static);
        formatter.Should().NotBeNull();

        formatter!.Invoke(null, ["Migrations", new InfrastructureHealthCheckDto("Migrations", "Warning", "Có 2 migration pending.")])
            .Should().Be("Migrations: Cảnh báo — Có 2 migration pending.");
        formatter.Invoke(null, ["E-invoice", new InfrastructureHealthCheckDto("E-invoice", "Warning", "Chưa có adapter hóa đơn điện tử thật.")])
            .Should().Be("E-invoice: Cảnh báo — Chưa có adapter hóa đơn điện tử thật.");
        formatter.Invoke(null, ["Database", new InfrastructureHealthCheckDto("Database", "Unhealthy", "SQL login failed for user [secret details].")])
            .Should().Be("Database: Lỗi");
        formatter.Invoke(null, ["Migrations", null])
            .Should().Be("Migrations: Không khả dụng");
    }

    [Fact]
    public void SETTINGS_U_061_home_is_selected_and_rendered_before_slow_runtime_probes()
    {
        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        var constructorStart = source.IndexOf("public SettingsViewModel(", StringComparison.Ordinal);
        var loadStart = source.IndexOf("private async Task LoadAsync()", StringComparison.Ordinal);
        constructorStart.Should().BeGreaterThanOrEqualTo(0);
        loadStart.Should().BeGreaterThan(constructorStart);

        var constructor = source[constructorStart..loadStart];
        constructor.Should().Contain("if (CurrentPage is null)")
            .And.Contain("ApplyCategorySelection(initialCategory);")
            .And.Contain("_ = LoadAsync();");

        var loadEnd = source.IndexOf("private void RefreshDynamicRows()", loadStart, StringComparison.Ordinal);
        loadEnd.Should().BeGreaterThan(loadStart);
        var initialLoad = source[loadStart..loadEnd];
        var firstPaint = initialLoad.IndexOf("RefreshDynamicRows();", StringComparison.Ordinal);
        var firstDatabaseProbe = initialLoad.IndexOf("_databaseAdminService.GetPrimaryConnectionAsync()", StringComparison.Ordinal);
        firstPaint.Should().BeGreaterThanOrEqualTo(0)
            .And.BeLessThan(firstDatabaseProbe, "Home should render its pending state before waiting on SQL/runtime providers");
    }

    [Fact]
    public void Settings_home_distinguishes_sql_backup_files_from_hash_validated_file_backups()
    {
        var sqlFormatter = typeof(SettingsViewModel).GetMethod("FormatHomeSqlBackupStatus", BindingFlags.NonPublic | BindingFlags.Static);
        var fileFormatter = typeof(SettingsViewModel).GetMethod("FormatHomeFileBackupStatus", BindingFlags.NonPublic | BindingFlags.Static);
        sqlFormatter.Should().NotBeNull();
        fileFormatter.Should().NotBeNull();

        sqlFormatter!.Invoke(null, [null]).Should().Be("SQL backup file: Chưa tìm thấy");
        fileFormatter!.Invoke(null, [null]).Should().Be("File backup đã kiểm tra: Chưa có");

        var backupAt = new DateTime(2026, 10, 7, 9, 30, 0, DateTimeKind.Local);
        var sqlBackup = new BackupRecordDto { CreatedAt = backupAt, Status = "Completed" };
        var fileBackup = new FileBackupRecordDto { CreatedAt = backupAt, FileCount = 125 };
        ((string)sqlFormatter.Invoke(null, [sqlBackup])!).Should().Be($"SQL backup file: {backupAt:yyyy-MM-dd HH:mm}");
        ((string)fileFormatter.Invoke(null, [fileBackup])!).Should().Be($"File backup đã kiểm tra: {backupAt:yyyy-MM-dd HH:mm} · 125 tệp");
    }

    [Fact]
    public void Settings_path_diagnostic_reports_read_access_without_claiming_write_access()
    {
        var describe = typeof(SettingsViewModel).GetMethod("DescribePhysicalPath", BindingFlags.NonPublic | BindingFlags.Static);
        describe.Should().NotBeNull();

        var directory = Path.Combine(Path.GetTempPath(), $"asmile-settings-path-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var status = (string)describe!.Invoke(null, [directory])!;
            status.Should().Contain("Đọc được nội dung");

            var missing = (string)describe.Invoke(null, [Path.Combine(directory, "missing")])!;
            missing.Should().Contain("không truy cập được");
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public async Task SETTINGS_U_018_client_access_flag_is_not_mutable_without_workstation_identity()
    {
        var repository = new InMemoryAppModuleRepository(new AppModule
        {
            AppKey = "inventory",
            AllowClientAccess = true
        });
        var service = new AppModuleService(repository, null!, null!);

        var request = new UpdateAppModuleRequest
        {
            AppKey = "inventory",
            AllowClientAccess = false
        };

        var act = () => service.UpdateAppAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chưa có định danh vai trò workstation đáng tin cậy*");
        repository.UpsertCount.Should().Be(0);
        repository.App!.AllowClientAccess.Should().BeTrue();
    }

    [Fact]
    public async Task SETTINGS_U_048_app_module_display_flags_are_persisted_without_changing_client_access_policy()
    {
        var repository = new InMemoryAppModuleRepository(new AppModule
        {
            AppKey = "inventory",
            IsEnabled = true,
            ShowOnDesktop = false,
            PinToTaskbar = false,
            AllowClientAccess = true
        });
        var service = new AppModuleService(repository, null!, null!);

        await service.UpdateAppAsync(new UpdateAppModuleRequest
        {
            AppKey = "inventory",
            IsEnabled = false,
            ShowOnDesktop = true,
            PinToTaskbar = true,
            AllowClientAccess = true,
            Reason = "settings UI test"
        });

        repository.App!.IsEnabled.Should().BeFalse();
        repository.App.ShowOnDesktop.Should().BeTrue();
        repository.App.PinToTaskbar.Should().BeTrue();
        repository.App.AllowClientAccess.Should().BeTrue();
        repository.UpsertCount.Should().Be(1);
    }

    [Fact]
    public void SETTINGS_U_049_app_module_controls_and_hotel_status_rows_are_navigable()
    {
        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());

        source.Should().Contain("targetPageKey: \"app-module-toggle-enabled\"")
            .And.Contain("targetPageKey: \"app-module-toggle-desktop\"")
            .And.Contain("targetPageKey: \"app-module-toggle-taskbar\"")
            .And.Contain("ApplyAppModuleToggleAsync(route)")
            .And.Contain("AddReadOnlyVfcItem(\n            status,\n            \"hotel-data-connection\"")
            .And.Contain("AddReadOnlyVfcItem(\n            status,\n            \"hotel-settings-scope\"");
    }

    [Fact]
    public void SETTINGS_U_026_inventory_app_exposes_warehouse_settings_at_its_runtime_scope()
    {
        var prefixMethod = typeof(AppModuleService).GetMethod("GetSettingPrefixes", BindingFlags.NonPublic | BindingFlags.Static);
        prefixMethod.Should().NotBeNull();

        var prefixes = (string[])prefixMethod!.Invoke(null, ["inventory"])!;
        prefixes.Should().Contain("Warehouse.");

        var targetMethod = typeof(SettingsViewModel).GetMethod("ResolveAppSettingTarget", BindingFlags.NonPublic | BindingFlags.Static);
        targetMethod.Should().NotBeNull();
        targetMethod!.Invoke(null, ["Warehouse.LowStockAlertRecipients"]).Should().Be("inventory");

        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .Where(x => x.Scope == SettingScope.App && prefixes.Any(prefix => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        definitions.Should().ContainKey("Warehouse.LowStockThreshold");
        definitions.Should().ContainKey("Warehouse.LowStockAlertRecipients");
        definitions["Warehouse.LowStockAlertRecipients"].IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_027_stock_voucher_default_warehouse_is_optional_and_resolved_from_real_lookup()
    {
        var service = new ModuleSettingsService(null!, null!);
        foreach (var appKey in new[] { "receipts", "issues" })
        {
            var definition = service.GetDefinition(appKey)!;
            var defaultWarehouse = definition.Sections.SelectMany(x => x.Settings)
                .Single(x => x.Key == "defaultWarehouse");

            defaultWarehouse.DefaultValue.Should().BeEmpty();
            defaultWarehouse.IsRequired.Should().BeFalse();
            new ModuleSettingsValidationService()
                .Validate(definition, new Dictionary<string, string>
                {
                    ["defaultWarehouse"] = string.Empty,
                    ["requireWarehouse"] = "true"
                })
                .Should().NotContainKey("defaultWarehouse");
        }
    }

    [Fact]
    public void SETTINGS_U_028_stock_voucher_unsupported_options_are_read_only_and_explain_why()
    {
        var service = new ModuleSettingsService(null!, null!);
        var unsupportedKeys = new[]
        {
            "defaultObject", "requireWarehouse", "requireObject", "allowSpecialStock",
            "updatePriceOrCost", "updateStock", "syncToLedger", "requireApproval", "defaultPrintTemplate"
        };

        foreach (var appKey in new[] { "receipts", "issues" })
        {
            var definition = service.GetDefinition(appKey)!;
            var settings = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

            foreach (var key in unsupportedKeys)
            {
                settings[key].DisabledDescription.Should().NotBeNullOrWhiteSpace($"{appKey}.{key}");
                var item = new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue);
                item.IsEnabled.Should().BeFalse($"{appKey}.{key}");
                item.DisabledReason.Should().Be(settings[key].DisabledDescription);
            }

            var invalidValues = unsupportedKeys.ToDictionary(key => key, _ => string.Empty);
            new ModuleSettingsValidationService().Validate(definition, invalidValues)
                .Should().NotContainKeys(unsupportedKeys);
        }
    }

    [Fact]
    public async Task SETTINGS_U_029_saving_stock_voucher_preserves_unavailable_values()
    {
        var settings = new InMemoryModuleSettingsService();
        settings.Set("Module.receipts.defaultObject", "LEGACY-SUPPLIER");
        settings.Set("Module.receipts.requireObject", "false");
        var warehouses = new InMemoryWarehouseAppService(new LookupItemDto { Id = 2, Code = "WH-02", Name = "Kho 2" });
        var service = new ModuleSettingsService(new InMemoryModuleSettingsRepository(), settings, warehouses);

        await service.SaveAsync("receipts", new Dictionary<string, string>
        {
            ["voucherCodePattern"] = "PN-{yyyyMMdd}-{0000}",
            ["defaultWarehouse"] = "WH-02",
            ["defaultObject"] = "UNSUPPORTED-NEW-VALUE",
            ["requireObject"] = "true"
        });

        var state = await service.LoadAsync("receipts");
        state.Values["defaultWarehouse"].Should().Be("WH-02");
        state.Values["defaultObject"].Should().Be("LEGACY-SUPPLIER");
        state.Values["requireObject"].Should().Be("false");
    }

    [Fact]
    public async Task SETTINGS_U_032_stock_voucher_rejects_unknown_or_inactive_default_warehouse_without_saving()
    {
        var settings = new InMemoryModuleSettingsService();
        var warehouses = new InMemoryWarehouseAppService(new LookupItemDto { Id = 2, Code = "WH-02", Name = "Kho 2" });
        var service = new ModuleSettingsService(new InMemoryModuleSettingsRepository(), settings, warehouses);

        var act = () => service.SaveAsync("receipts", new Dictionary<string, string>
        {
            ["defaultWarehouse"] = "WH-99"
        });

        var error = await act.Should().ThrowAsync<ModuleSettingsValidationException>();
        error.Which.Key.Should().Be("defaultWarehouse");
        error.Which.Message.Should().Contain("không tồn tại hoặc đã ngừng hoạt động");
        var state = await service.LoadAsync("receipts");
        state.Values["defaultWarehouse"].Should().BeEmpty("invalid values must not be persisted");
    }

    [Fact]
    public void SETTINGS_U_030_payment_voucher_options_without_runtime_consumers_are_read_only()
    {
        var service = new ModuleSettingsService(null!, null!);
        var unsupportedKeys = new[]
        {
            "defaultCashAccount", "defaultBankAccount", "defaultCreator", "defaultApprover",
            "requireObject", "requireReason", "allowMultipleDebts", "syncToLedger",
            "requireApproval", "defaultPrintTemplate", "printCopies"
        };

        foreach (var appKey in new[] { "payment-receipt", "payment-voucher" })
        {
            var definition = service.GetDefinition(appKey)!;
            var settings = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

            foreach (var key in unsupportedKeys)
            {
                settings[key].DisabledDescription.Should().NotBeNullOrWhiteSpace($"{appKey}.{key}");
                new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue)
                    .IsEnabled.Should().BeFalse($"{appKey}.{key}");
            }

            new ModuleSettingItemViewModel(settings["voucherCodePattern"], "PT-{yyyyMMdd}-{0000}")
                .IsEnabled.Should().BeTrue("voucher codes are consumed by the receipt/payment flows");

            var invalidValues = unsupportedKeys.ToDictionary(key => key, _ => string.Empty);
            new ModuleSettingsValidationService().Validate(definition, invalidValues)
                .Should().NotContainKeys(unsupportedKeys);
        }
    }

    [Fact]
    public async Task SETTINGS_U_031_saving_payment_voucher_preserves_unavailable_values()
    {
        var settings = new InMemoryModuleSettingsService();
        settings.Set("Module.payment-receipt.defaultCashAccount", "112-LEGACY");
        settings.Set("Module.payment-receipt.printCopies", "3");
        var service = new ModuleSettingsService(new InMemoryModuleSettingsRepository(), settings);

        await service.SaveAsync("payment-receipt", new Dictionary<string, string>
        {
            ["voucherCodePattern"] = "PT-{yyyyMMdd}-{0000}",
            ["defaultCashAccount"] = "111-UNSUPPORTED",
            ["printCopies"] = "5"
        });

        var state = await service.LoadAsync("payment-receipt");
        state.Values["defaultCashAccount"].Should().Be("112-LEGACY");
        state.Values["printCopies"].Should().Be("3");
        state.Values["voucherCodePattern"].Should().Be("PT-{yyyyMMdd}-{0000}");
    }

    [Fact]
    public async Task SETTINGS_U_032_resetting_payment_voucher_defaults_keeps_unavailable_values()
    {
        var settings = new InMemoryModuleSettingsService();
        settings.Set("Module.payment-voucher.voucherCodePattern", "PC-LEGACY-{0000}");
        settings.Set("Module.payment-voucher.defaultBankAccount", "112-LEGACY");
        var service = new ModuleSettingsService(new InMemoryModuleSettingsRepository(), settings);

        var state = await service.ResetDefaultAsync("payment-voucher");

        state.Values["voucherCodePattern"].Should().Be("PC-{yyyyMMdd}-{0000}");
        state.Values["defaultBankAccount"].Should().Be("112-LEGACY");
    }

    [Fact]
    public void SETTINGS_U_033_unconsumed_catalog_module_settings_are_marked_unavailable()
    {
        var service = new ModuleSettingsService(null!, null!);
        var appKeys = new[] { "warehouses", "dashboard", "receivable", "payable" };

        foreach (var appKey in appKeys)
        {
            var definition = service.GetDefinition(appKey);
            definition.Should().NotBeNull(appKey);
            var items = definition!.Sections.SelectMany(x => x.Settings).ToArray();
            items.Should().NotBeEmpty(appKey);
            items.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.DisabledDescription), appKey);
            items.Select(x => new ModuleSettingItemViewModel(x, x.DefaultValue))
                .Should().OnlyContain(x => !x.IsEnabled, appKey);
        }

        var hrSettings = service.GetDefinition("departments")!.Sections.SelectMany(x => x.Settings).ToArray();
        hrSettings.Where(x => x.Key is "defaultDepartment" or "defaultPosition")
            .Select(x => new ModuleSettingItemViewModel(x, x.DefaultValue))
            .Should().OnlyContain(x => x.IsEnabled);
        hrSettings.Where(x => x.Key is not "defaultDepartment" and not "defaultPosition"
            and not "autoEmployeeCode" and not "employeeCodePattern")
            .Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.DisabledDescription));
    }

    [Fact]
    public async Task SETTINGS_U_034_saving_unconsumed_catalog_settings_preserves_legacy_values()
    {
        var settings = new InMemoryModuleSettingsService();
        settings.Set("Module.customers.lookupTaxInfo", "false");
        var service = new ModuleSettingsService(new InMemoryModuleSettingsRepository(), settings);

        await service.SaveAsync("customers", new Dictionary<string, string>
        {
            ["lookupTaxInfo"] = "true",
            ["requirePhone"] = "false"
        });

        var state = await service.LoadAsync("customers");
        state.Values["lookupTaxInfo"].Should().Be("false");
        state.Values["requirePhone"].Should().Be("false");
    }

    [Fact]
    public void SETTINGS_U_036_customer_required_field_policies_are_editable_but_unwired_policies_are_not()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("customers")!;
        var settings = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

        foreach (var key in new[] { "requirePhone", "requireTaxCode", "checkDuplicatePhone", "checkDuplicateTaxCode" })
        {
            settings[key].DisabledDescription.Should().BeNull(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeTrue(key);
        }

        foreach (var key in new[] { "autoCustomerCode", "customerCodePattern" })
        {
            settings[key].DisabledDescription.Should().BeNull(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeTrue(key);
        }

        foreach (var key in new[]
        {
            "defaultCustomerGroup", "lookupTaxInfo",
            "extraFields", "canView", "canEdit", "canDelete", "allowLockCustomer"
        })
        {
            settings[key].DisabledDescription.Should().NotBeNullOrWhiteSpace(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeFalse(key);
        }
    }

    [Fact]
    public void SETTINGS_U_037_supplier_contact_policies_are_editable_and_unimplemented_options_remain_unavailable()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("suppliers")!;
        var settings = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

        foreach (var key in new[] { "requirePhone", "requireTaxCode", "checkDuplicateTaxCode" })
        {
            settings[key].DisabledDescription.Should().BeNull(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeTrue(key);
        }

        foreach (var key in new[] { "autoSupplierCode", "supplierCodePattern" })
        {
            settings[key].DisabledDescription.Should().BeNull(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeTrue(key);
        }

        foreach (var key in new[]
        {
            "defaultSupplierGroup", "lookupTaxInfo",
            "canView", "canEdit", "canDelete", "allowLockSupplier"
        })
        {
            settings[key].DisabledDescription.Should().NotBeNullOrWhiteSpace(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeFalse(key);
        }
    }

    [Fact]
    public void SETTINGS_U_038_auto_supplier_code_requires_a_supported_sequence_token()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("suppliers")!;
        var validator = new ModuleSettingsValidationService();

        validator.Validate(definition, new Dictionary<string, string>
        {
            ["autoSupplierCode"] = "true",
            ["supplierCodePattern"] = "NCC-{00000}"
        }).Should().NotContainKey("supplierCodePattern");

        validator.Validate(definition, new Dictionary<string, string>
        {
            ["autoSupplierCode"] = "true",
            ["supplierCodePattern"] = "NCC-{yyyyMMdd}"
        }).Should().ContainKey("supplierCodePattern");
    }

    [Fact]
    public void SETTINGS_U_039_auto_customer_code_requires_a_supported_sequence_token()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("customers")!;
        var validator = new ModuleSettingsValidationService();

        validator.Validate(definition, new Dictionary<string, string>
        {
            ["autoCustomerCode"] = "true",
            ["customerCodePattern"] = "KH-{00000}"
        }).Should().NotContainKey("customerCodePattern");

        validator.Validate(definition, new Dictionary<string, string>
        {
            ["autoCustomerCode"] = "true",
            ["customerCodePattern"] = "KH-{yyyyMMdd}"
        }).Should().ContainKey("customerCodePattern");
    }

    [Fact]
    public void SETTINGS_U_040_product_code_generation_is_editable_but_unwired_product_policies_are_read_only()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("items")!;
        var settings = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

        foreach (var key in new[] { "autoProductCode", "productCodePattern", "defaultProductGroup", "defaultUnit", "defaultVat" })
        {
            settings[key].DisabledDescription.Should().BeNull(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeTrue(key);
        }

        foreach (var key in new[]
        {
            "defaultPriceMode",
            "trackInventory", "trackBatchExpiry", "trackSerialImei", "checkDuplicateCodeNameBarcode",
            "multiUnit", "allowProductImage"
        })
        {
            settings[key].DisabledDescription.Should().NotBeNullOrWhiteSpace(key);
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue).IsEnabled.Should().BeFalse(key);
        }
    }

    [Fact]
    public void SETTINGS_U_041_auto_product_code_requires_a_supported_sequence_token()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("items")!;
        var validator = new ModuleSettingsValidationService();

        validator.Validate(definition, new Dictionary<string, string>
        {
            ["autoProductCode"] = "true",
            ["productCodePattern"] = "HH-{00000}"
        }).Should().NotContainKey("productCodePattern");

        validator.Validate(definition, new Dictionary<string, string>
        {
            ["autoProductCode"] = "true",
            ["productCodePattern"] = "HH-{yyyyMMdd}"
        }).Should().ContainKey("productCodePattern");
    }

    [Fact]
    public void SETTINGS_U_035_inventory_keeps_only_the_cost_method_setting_editable()
    {
        var service = new ModuleSettingsService(null!, null!);
        var definition = service.GetDefinition("inventory")!;
        var items = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

        items["costMethod"].DisabledDescription.Should().BeNull();
        new ModuleSettingItemViewModel(items["costMethod"], items["costMethod"].DefaultValue).IsEnabled.Should().BeTrue();

        var unsupportedKeys = new[]
        {
            "allowNegativeStock", "syncToLedger", "autoProductCode", "stockInCodePattern", "stockOutCodePattern",
            "defaultWarehouse", "defaultUnit", "roundingMode", "minStockAlert", "maxStockAlert",
            "checkDuplicateProductCode", "requireWarehouse", "requireObject"
        };
        foreach (var key in unsupportedKeys)
        {
            items[key].DisabledDescription.Should().NotBeNullOrWhiteSpace(key);
            new ModuleSettingItemViewModel(items[key], items[key].DefaultValue).IsEnabled.Should().BeFalse(key);
        }
    }

    [Fact]
    public void SETTINGS_U_019_client_access_detail_is_not_advertised_as_enabled_or_disabled()
    {
        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());

        source.Should().Contain("Chính sách truy cập máy trạm");
        source.Should().Contain("Chưa áp dụng: hệ thống chưa phân biệt vai trò máy chủ trung tâm và máy trạm.");
        source.Should().Contain("chưa phải định danh thiết bị đáng tin cậy");
        source.Should().NotContain("Registered clients");
    }

    [Fact]
    public void SETTINGS_U_020_system_mode_exposes_only_the_runtime_backed_maintenance_policy()
    {
        var maintenanceDefinition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == "System.MaintenanceMode");
        maintenanceDefinition.Scope.Should().Be(SettingScope.System);
        maintenanceDefinition.ValueType.Should().Be(SettingValueType.Boolean);
        maintenanceDefinition.DefaultValue.Should().Be("false");
        maintenanceDefinition.IsReadOnly.Should().BeFalse();
        maintenanceDefinition.RequiredPermission.Should().Be("System.Maintenance");
        maintenanceDefinition.Description.Should().Contain("tối đa 30 giây").And.Contain("lưu công việc");

        var routeField = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        var routes = (IReadOnlyDictionary<string, string[]>)routeField!.GetValue(null)!;
        routes.Should().NotContainKey("system-mode", "database migration and timeout settings do not define deployment mode");

        var builder = typeof(SettingsViewModel).GetMethod("BuildSystemModeDetail", BindingFlags.NonPublic | BindingFlags.Static);
        builder.Should().NotBeNull();
        var row = new SettingItemViewModel("system-mode", "", "Chế độ vận hành", "Vai trò chưa được hỗ trợ.");
        var detail = (SettingsDetailViewModel)builder!.Invoke(null, [row, true, true])!;
        var items = detail.Cards.SelectMany(card => card.Items).ToArray();

        items.Should().HaveCount(2);
        var deployment = items.Single(item => item.Id == "system-mode-deployment");
        deployment.IsClickable.Should().BeTrue();
        deployment.IsReadOnly.Should().BeTrue();
        deployment.TargetPageKey.Should().Be("settings-info:system-mode-deployment");
        var maintenance = items.Single(item => item.Id == "system-mode-maintenance");
        maintenance.IsClickable.Should().BeTrue();
        maintenance.IsToggle.Should().BeTrue();
        maintenance.ToggleValue.Should().BeTrue();
        maintenance.RequiredPermission.Should().Be(Permission.ManageServices);
        items.Select(item => item.Id).Should().Contain(["system-mode-deployment", "system-mode-maintenance"]);
    }

    [Fact]
    public void SETTINGS_U_021_audit_detail_uses_persisted_metadata_without_exposing_value_snapshots()
    {
        var builder = typeof(SettingsViewModel).GetMethod("AddAuditLogRows", BindingFlags.NonPublic | BindingFlags.Static);
        builder.Should().NotBeNull();
        var card = new SettingCardViewModel("Audit", "Persisted records");
        var record = new AuditLog
        {
            Id = 17,
            CreatedAt = new DateTime(2026, 10, 2, 8, 30, 0, DateTimeKind.Utc),
            Username = "auditor",
            Action = "Update",
            Detail = "Update property profile",
            EntityName = "Property",
            EntityId = "P-1",
            IpAddress = "10.0.0.5",
            OldValuesJson = "old-secret-value",
            NewValuesJson = "new-secret-value"
        };

        builder!.Invoke(null, [card, new[] { record }]);

        card.Items.Should().ContainSingle();
        var item = card.Items.Single();
        item.Title.Should().Be("Property/P-1");
        item.Description.Should().Contain("Update property profile");
        item.Value.Should().Contain("auditor").And.Contain("10.0.0.5");
        item.IsClickable.Should().BeTrue();
        item.IsReadOnly.Should().BeTrue();
        item.TargetPageKey.Should().Be("settings-info:security-audit-17");
        string.Join(" ", item.Title, item.Description, item.Value)
            .Should().NotContain("old-secret-value").And.NotContain("new-secret-value");
    }

    [Fact]
    public void SETTINGS_U_022_login_audit_detail_shows_real_outcome_and_origin()
    {
        var builder = typeof(SettingsViewModel).GetMethod("AddLoginAuditRows", BindingFlags.NonPublic | BindingFlags.Static);
        builder.Should().NotBeNull();
        var card = new SettingCardViewModel("Login", "Persisted records");
        var record = new LoginAuditDto
        {
            CreatedAt = new DateTime(2026, 10, 2, 8, 30, 0, DateTimeKind.Utc),
            UserName = "frontdesk",
            Success = false,
            FailureReason = "Invalid credentials",
            MachineName = "FO-01",
            IpAddress = "10.0.0.9"
        };

        builder!.Invoke(null, [card, new[] { record }]);

        card.Items.Should().ContainSingle();
        var item = card.Items.Single();
        item.Title.Should().Be("frontdesk");
        item.Description.Should().Contain("Đăng nhập thất bại")
            .And.Contain("Invalid credentials")
            .And.Contain("FO-01")
            .And.Contain("10.0.0.9");
        item.IsClickable.Should().BeTrue();
        item.IsReadOnly.Should().BeTrue();
        item.TargetPageKey.Should().StartWith("settings-info:security-login-");
    }

    [Fact]
    public void SETTINGS_U_023_security_log_routes_keep_their_existing_editable_policy_settings()
    {
        var routeField = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        var routes = (IReadOnlyDictionary<string, string[]>)routeField!.GetValue(null)!;
        routes["sec-error"].Should().Contain("Logs.LogLevel");
        routes["sec-error"].Should().Contain("Logs.RetentionDays");
        routes["logs-security"].Should().Contain("Logs.RetentionDays");
        routes["sec-login"].Should().Contain("Security.MaxFailedLoginAttempts");
        routes["sec-sensitive"].Should().Contain("Security.RequireAdminConfirmation");

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        var routeHandler = source.IndexOf("route.StartsWith(\"sec-\"", StringComparison.Ordinal);
        var settingsLoad = source.IndexOf("await LoadEditableDetailAsync(route);", routeHandler, StringComparison.Ordinal);
        var logLoad = source.IndexOf("BuildSecurityOperationalDetailAsync(route, row)", routeHandler, StringComparison.Ordinal);

        routeHandler.Should().BeGreaterThanOrEqualTo(0);
        settingsLoad.Should().BeGreaterThanOrEqualTo(routeHandler);
        logLoad.Should().BeGreaterThan(settingsLoad);
    }

    [Fact]
    public void SETTINGS_U_047_read_only_provider_rows_open_a_read_only_detail_instead_of_being_dead_rows()
    {
        var card = new SettingCardViewModel("Provider", "Live runtime information");
        var addRow = typeof(SettingsViewModel).GetMethod("AddReadOnlyVfcItem", BindingFlags.NonPublic | BindingFlags.Static);
        addRow.Should().NotBeNull();

        addRow!.Invoke(null, [card, "database-server", "Server", "SQL Server host", ".\\SQLEXPRESS", null]);

        var item = card.Items.Should().ContainSingle().Subject;
        item.IsClickable.Should().BeTrue();
        item.IsReadOnly.Should().BeTrue();
        item.TargetPageKey.Should().Be("settings-info:database-server");

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("const string readOnlyInfoPrefix = \"settings-info:\"")
            .And.Contain("infoItem.IsReadOnly = true;")
            .And.Contain("item.IsClickable = true;")
            .And.NotContain("allowStandaloneDetails");
    }

    [Fact]
    public void SETTINGS_U_060_runtime_configuration_rows_open_their_real_editors()
    {
        var sourcePath = ResolveSettingsViewModelSourcePath();
        var source = File.ReadAllText(sourcePath);
        var appDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, ".."));
        var view = File.ReadAllText(Path.Combine(appDirectory, "Views", "SettingsView.xaml"));
        var viewCodeBehind = File.ReadAllText(Path.Combine(appDirectory, "Views", "SettingsView.xaml.cs"));

        source.Should().Contain("\"db-connection-type\",\n            \"Provider\"")
            .And.Contain("connection.Status,\n            \"database-sql-connection\")")
            .And.Contain("_serviceProvider.GetRequiredService<IDatabaseConnectionConfigurationService>()")
            .And.Contain("IsDatabaseConnectionEditorVisible = true;")
            .And.Contain("\"system-machine\", \"Machine\", Environment.MachineName, Environment.UserName, \"windows-settings-about\")")
            .And.Contain("\"system-timezone\", \"Múi giờ\", TimeZoneInfo.Local.Id, TimeZoneInfo.Local.DisplayName, \"windows-settings-date-time\")")
            .And.Contain("\"system-database\", \"Database\", connection.DatabaseName, connection.Status, \"database-sql-connection\")")
            .And.Contain("[\"network-api-server\"] = [\"Network.ApiBaseUrl\", \"Network.ApiPort\", \"Network.EnableHttps\"]")
            .And.Contain("await LoadEditableDetailAsync(route);")
            .And.Contain("BuildNetworkEndpointDetailAsync(route, row)")
            .And.Contain("private async Task SaveEditableAsync()")
            .And.Contain("await _settingsService.SaveValuesAsync(requests);");

        view.Should().Contain("Visibility=\"{Binding IsDatabaseConnectionEditorVisible, Converter={StaticResource BooleanToVisibilityConverter}}\"")
            .And.Contain("Command=\"{Binding TestDatabaseConfigurationCommand}\"")
            .And.Contain("Click=\"OnSaveDatabaseConfigurationClick\"");
        viewCodeBehind.Should().Contain("await vm.SaveDatabaseConfigurationCommand.ExecuteAsync(null);");
    }

    [Fact]
    public void SETTINGS_U_055_empty_and_unavailable_rows_open_safe_read_only_details()
    {
        var addRow = typeof(SettingsViewModel).GetMethod("AddReadOnlyVfcItem", BindingFlags.NonPublic | BindingFlags.Static);
        addRow.Should().NotBeNull();

        foreach (var id in new[]
        {
            "backup-restore-denied", "backup-restore-empty", "backup-history-empty", "file-backup-history-empty",
            "client-service-empty", "client-registry-empty", "printer-empty", "printer-permissions-empty"
        })
        {
            var card = new SettingCardViewModel("Empty state", "No runtime data");
            addRow!.Invoke(null, [card, id, "Thông tin", "Không có dữ liệu runtime.", "Chưa có dữ liệu", null]);

            var item = card.Items.Should().ContainSingle().Subject;
            item.IsClickable.Should().BeTrue(id);
            item.IsReadOnly.Should().BeTrue(id);
            item.TargetPageKey.Should().Be($"settings-info:{id}", id);
        }

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("targetPageKey: \"settings-info:backup-db-restore-policy\"")
            .And.Contain("targetPageKey: canRestoreFiles ? \"backup-files\" : \"settings-info:backup-files-restore-policy\"")
            .And.Contain("AddReadOnlyVfcItem(restoreActions, \"backup-restore-denied\"")
            .And.Contain("AddReadOnlyVfcItem(restoreActions, \"backup-restore-empty\"")
            .And.NotContain("restore.IsClickable = false;");
    }

    [Fact]
    public void SETTINGS_U_056_clients_health_page_uses_real_infrastructure_checks()
    {
        typeof(SettingsViewModel).GetMethod(
                "BuildClientHealthDetailAsync",
                BindingFlags.Instance | BindingFlags.NonPublic)
            .Should().NotBeNull("the health item must not fall through to a generic Windows service list");

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("string.Equals(route, \"clients-health\", StringComparison.OrdinalIgnoreCase)")
            .And.Contain("await BuildClientHealthDetailAsync(row)")
            .And.Contain("_serviceProvider.GetService<IInfrastructureHealthService>()")
            .And.Contain("FormatHomeHealthStatus(check.Name, check)")
            .And.NotContain("Row(\"clients-health\", \"\\uEA39\", \"Service health\", \"Heartbeat và cảnh báo\")");
    }

    [Fact]
    public void SETTINGS_U_057_network_discovery_shows_real_configured_endpoint_probes_and_edit_routes()
    {
        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("var networkNodes = await virtualExplorer.GetChildrenAsync(\"Network\")")
            .And.Contain("(\"API Server\", \"API Server\", \"network-api-server\")")
            .And.Contain("(\"POS Terminals\", \"POS terminals\", \"net-pos\")")
            .And.Contain("(\"FO Clients\", \"FO clients\", \"net-fo\")")
            .And.Contain("endpoint?.Description ?? \"Probe không trả về endpoint này; không suy diễn trạng thái.\"")
            .And.Contain("targetPage);")
            .And.Contain("không dò subnet hoặc tự phát hiện máy trong LAN")
            .And.Contain("Network.AllowClientDiscovery")
            .And.Contain("Chưa triển khai");
    }

    [Fact]
    public void SETTINGS_U_058_windows_service_rows_open_the_real_services_management_console()
    {
        var sourcePath = ResolveSettingsViewModelSourcePath();
        var solutionRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", ".."));
        var viewModel = File.ReadAllText(sourcePath);
        var launcherContract = File.ReadAllText(Path.Combine(
            solutionRoot, "ASMILE.BO.Application", "Interfaces", "IWindowsSettingsLauncher.cs"));
        var launcher = File.ReadAllText(Path.Combine(
            solutionRoot, "ASMILE.BO.Infrastructure", "Services", "WindowsSettingsLauncher.cs"));

        launcherContract.Should().Contain("void OpenServicesConsole();");
        launcher.Should().Contain("OpenServicesConsole()")
            .And.Contain("Path.Combine(Environment.SystemDirectory, \"services.msc\")")
            .And.Contain("FileName = servicesConsole")
            .And.Contain("UseShellExecute = true");
        viewModel.Should().Contain("string.Equals(route, \"windows-settings-services\", StringComparison.OrdinalIgnoreCase)")
            .And.Contain("settingsLauncher.OpenServicesConsole()")
            .And.Contain("service.Status,\n                    \"windows-settings-services\"")
            .And.Contain("targetPageKey: \"windows-settings-services\"");
    }

    [Fact]
    public void SETTINGS_U_059_app_paths_include_the_persisted_export_root_editor()
    {
        var viewModel = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        var routeField = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        routeField.Should().NotBeNull();
        var routes = (IReadOnlyDictionary<string, string[]>)routeField!.GetValue(null)!;

        routes["system-folders"].Should().Contain("Storage.ExportRootPath");
        viewModel.Should().Contain("GetValueAsync(\"Storage.ExportRootPath\", SettingScope.System, null)")
            .And.Contain("\"app-export-root\"")
            .And.Contain("\"Export root hệ thống\"")
            .And.Contain("\"system-folders\"");
        viewModel.Should().Contain("\"Template mapping theo module\"")
            .And.Contain("đây không phải export root chung");
    }

    [Fact]
    public void SETTINGS_U_050_landing_row_factory_keeps_all_settings_sections_navigable()
    {
        var rowFactory = typeof(SettingsViewModel).GetMethod("Row", BindingFlags.NonPublic | BindingFlags.Static);
        rowFactory.Should().NotBeNull();

        var row = (SettingItemViewModel)rowFactory!.Invoke(null,
            ["coverage-row", "\uE8A4", "Coverage", "Landing row", "", "", false, null])!;

        row.IsClickable.Should().BeTrue();
        row.TargetPageKey.Should().Be("coverage-row");
        row.RequiredPermission.Should().Be(ASMILE.BO.Core.Security.Permission.ViewSystemSettings);

        var resolveRoute = typeof(SettingsViewModel).GetMethod("ResolveSettingsRowRoute", BindingFlags.NonPublic | BindingFlags.Static);
        resolveRoute.Should().NotBeNull();
        var dynamicRow = new SettingItemViewModel("dynamic-row", "\uE8A4", "Dynamic", "Uses default target");
        resolveRoute!.Invoke(null, [dynamicRow]).Should().Be("dynamic-row");
        var routedRow = new SettingItemViewModel("dynamic-row", "\uE8A4", "Dynamic", "Has explicit target", targetPageKey: "target-route");
        resolveRoute.Invoke(null, [routedRow]).Should().Be("target-route");

        var appDirectory = Directory.GetParent(ResolveSettingsViewModelSourcePath())!.Parent!.FullName;
        var viewPath = Path.Combine(appDirectory, "Views", "SettingsView.xaml");
        File.Exists(viewPath).Should().BeTrue();
        var view = File.ReadAllText(viewPath);
        view.Should().Contain("DataContext.NavigateToSettingsDetailCommand")
            .And.Contain("IsEnabled=\"{Binding IsClickable}\"")
            .And.Contain("Click=\"OnSettingRowClick\"")
            .And.Contain("SelectedItem=\"{Binding SelectedCategory, Mode=TwoWay}\"")
            .And.NotContain("SelectionChanged=\"OnCategorySelectionChanged\"")
            .And.NotContain("MouseLeftButtonUp=\"OnCategoryMouseLeftButtonUp\"");

        var codeBehindPath = viewPath + ".cs";
        File.Exists(codeBehindPath).Should().BeTrue();
        var codeBehind = File.ReadAllText(codeBehindPath);
        codeBehind.Should().Contain("sender is not Button { DataContext: SettingItemViewModel item }")
            .And.Contain("await vm.NavigateToSettingsDetail(item);")
            .And.NotContain("OnCategorySelectionChanged")
            .And.NotContain("OnCategoryMouseLeftButtonUp");

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("ResolveSettingsRowRoute(row)");
        source.Should().Contain("partial void OnSelectedCategoryChanged(SettingsCategoryItem? value)")
            .And.Contain("ApplyCategorySelection(value);")
            .And.Contain("SelectCategoryForNavigation(item);")
            .And.Contain("if (ReferenceEquals(SelectedCategory, item))")
            .And.Contain("SelectedCategory = item;");
        var pagesStart = source.IndexOf("private void BuildPages()", StringComparison.Ordinal);
        var rowFactoryStart = source.IndexOf("private static SettingItemViewModel Row(", pagesStart, StringComparison.Ordinal);
        pagesStart.Should().BeGreaterThanOrEqualTo(0);
        rowFactoryStart.Should().BeGreaterThan(pagesStart);
        var pagesSource = source[pagesStart..rowFactoryStart];
        var rowIds = System.Text.RegularExpressions.Regex.Matches(pagesSource, "Row\\(\\\"([^\\\"]+)\\\"")
            .Select(match => match.Groups[1].Value)
            .ToArray();
        rowIds.Should().HaveCountGreaterThan(80, "the landing-page inventory must be checked as a whole");
        rowIds.Should().OnlyHaveUniqueItems();
        pagesSource.Should().NotContain("new SettingItemViewModel", "all landing rows must go through the clickable row factory");
    }

    [Fact]
    public void SETTINGS_U_051_app_details_open_the_persisted_editor_for_writable_app_scoped_settings()
    {
        var prefixesMethod = typeof(AppModuleService).GetMethod("GetSettingPrefixes", BindingFlags.NonPublic | BindingFlags.Static);
        prefixesMethod.Should().NotBeNull();
        var definitions = new SettingsRegistry().GetDefaultDefinitions();

        var hasWritableDefinitions = typeof(SettingsViewModel).GetMethod(
            "HasWritableAppSettingDefinitions", BindingFlags.NonPublic | BindingFlags.Static);
        hasWritableDefinitions.Should().NotBeNull();

        foreach (var (appKey, expectedKeys) in new[]
        {
            ("inventory", new[] { "Warehouse.LowStockThreshold", "Warehouse.LowStockAlertRecipients" }),
            ("pos-client", new[] { "POS.ApiEndpoint" }),
            ("fo-client", new[] { "FO.ApiEndpoint" })
        })
        {
            var prefixes = (string[])prefixesMethod!.Invoke(null, [appKey])!;
            var appDefinitions = definitions
                .Where(x => x.Scope == SettingScope.App
                    && prefixes.Any(prefix => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            foreach (var expectedKey in expectedKeys)
            {
                appDefinitions.Should().Contain(x => x.Key == expectedKey && !x.IsReadOnly, appKey);
            }
            hasWritableDefinitions!.Invoke(null, [appDefinitions]).Should().Be(true, appKey);
        }

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("if (HasWritableAppSettingDefinitions(appDefinitions))")
            .And.Contain("await LoadAppAdvancedEditorAsync(appKey);")
            .And.Contain("await OpenModuleSettingsAsync(appKey);");
    }

    [Fact]
    public void SETTINGS_U_052_app_settings_keep_separate_writable_module_configuration_reachable()
    {
        var inventory = new ModuleSettingsService(null!, null!).GetDefinition("inventory");
        inventory.Should().NotBeNull();
        inventory!.Sections.SelectMany(section => section.Settings)
            .Should().Contain(setting => setting.Key == "costMethod"
                && string.IsNullOrWhiteSpace(setting.DisabledDescription));

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("app-module-settings:")
            .And.Contain("_moduleSettingsService.GetDefinition(appKey)")
            .And.Contain("targetPageKey: $\"app-module-settings:{appKey}\"")
            .And.Contain("await OpenModuleSettingsAsync(appKey);");
    }

    [Fact]
    public void SETTINGS_U_053_every_writable_definition_has_a_reachable_editor_route()
    {
        var routeField = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        routeField.Should().NotBeNull();
        var routes = (IReadOnlyDictionary<string, string[]>)routeField!.GetValue(null)!;
        var definitions = new SettingsRegistry().GetDefaultDefinitions();
        var editableRoutes = routes.Values.SelectMany(keys => keys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var globalWritable = definitions
            .Where(x => x.Scope != SettingScope.App && !x.IsReadOnly)
            .Select(x => x.Key)
            .Where(key => !editableRoutes.Contains(key) && !string.Equals(key, "System.MaintenanceMode", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        globalWritable.Should().BeEmpty("system and user settings must be reachable from a persisted editor route; maintenance mode uses its dedicated toggle");
        definitions.Should().Contain(x => x.Key == "System.MaintenanceMode" && !x.IsReadOnly);

        var resolveTarget = typeof(SettingsViewModel).GetMethod("ResolveAppSettingTarget", BindingFlags.NonPublic | BindingFlags.Static);
        var getPrefixes = typeof(AppModuleService).GetMethod("GetSettingPrefixes", BindingFlags.NonPublic | BindingFlags.Static);
        resolveTarget.Should().NotBeNull();
        getPrefixes.Should().NotBeNull();

        var appWritable = definitions.Where(x => x.Scope == SettingScope.App && !x.IsReadOnly).ToArray();
        appWritable.Should().NotBeEmpty();
        foreach (var definition in appWritable)
        {
            var appKey = resolveTarget!.Invoke(null, [definition.Key]) as string;
            appKey.Should().NotBeNullOrWhiteSpace(definition.Key);
            var prefixes = (string[])getPrefixes!.Invoke(null, [appKey])!;
            prefixes.Should().Contain(prefix => definition.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase), definition.Key);
        }

        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("if (HasWritableAppSettingDefinitions(appDefinitions))")
            .And.Contain("await LoadAppAdvancedEditorAsync(appKey);")
            .And.Contain("await LoadGeneralLedgerConfigurationAsync();")
            .And.Contain("System.MaintenanceMode");
    }

    [Fact]
    public void SETTINGS_U_001_editable_landing_routes_resolve_to_persisted_definitions()
    {
        var field = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var routes = (IReadOnlyDictionary<string, string[]>)field!.GetValue(null)!;
        var definitions = new SettingsRegistry()
            .GetDefaultDefinitions()
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        routes.Should().NotBeEmpty();
        routes.Should().ContainKey("db-command-policy");
        routes["db-command-policy"].Should().Equal("Database.CommandTimeout");
        routes["system-folders"].Should().Contain("Storage.DataRootPath", "Storage.ExportRootPath", "Backup.BackupPath");
        routes.Should().NotContainKey("database-sql-connection");
        routes.Should().NotContainKey("db-connection");
        typeof(SettingsViewModel).GetMethod("BuildDatabaseConnectionDetailAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            .Should().NotBeNull("the connection screen must use live DbConnection metadata instead of Settings Engine defaults");
        routes.Should().ContainKey("network-api-server");
        routes.Should().ContainKey("net-pos");
        routes.Should().ContainKey("net-fo");
        routes["network-api-server"].Should().Equal("Network.ApiBaseUrl", "Network.ApiPort", "Network.EnableHttps");
        routes["net-pos"].Should().Equal("POS.ApiEndpoint");
        routes["net-fo"].Should().Equal("FO.ApiEndpoint");
        typeof(SettingsViewModel).GetMethod("BuildNetworkEndpointDetailAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            .Should().NotBeNull("endpoint settings show persisted values, allow edits, and run the actual VFC probe");
        routes.Should().ContainKey("ui-theme");
        routes.Should().ContainKey("ui-snap");
        routes["ui-snap"].Should().Equal("Appearance.SnapDesktopIconsToGrid");
        routes.Should().NotContainKey("ui-window", "window-position memory is not implemented and must not be advertised as a setting");
        routes.Should().ContainKey("users-policy");
        routes.Should().ContainKey("vfc-root");
        routes.Should().ContainKey("maint-channel");
        routes.Should().NotContainKey("net-discovery");
        routes.Should().NotContainKey("sec-audit");
        routes.Should().ContainKey("sec-error");
        routes["sec-error"].Should().Equal("Logs.LogLevel", "Logs.RetentionDays");
        routes.Should().NotContainKey("users-audit");
        routes.Should().NotContainKey("clients-sync");
        definitions.Should().ContainKey("Maintenance.UpdateChannel");
        definitions.Should().ContainKey("Appearance.SnapDesktopIconsToGrid");
        definitions["Appearance.SnapDesktopIconsToGrid"].IsReadOnly.Should().BeFalse();

        foreach (var (route, keys) in routes)
        {
            keys.Should().NotBeEmpty(route);
            keys.Should().OnlyHaveUniqueItems(route);
            keys.Should().OnlyContain(key => definitions.ContainsKey(key), route);
            keys.Should().OnlyContain(key => !definitions[key].IsReadOnly, route);
        }
    }

    [Theory]
    [InlineData("POS.ApiEndpoint", "pos-client")]
    [InlineData("FO.ApiEndpoint", "fo-client")]
    [InlineData("GeneralLedger.CashAccount", "general-ledger")]
    [InlineData("ImportVoucher.CodePrefix", "receipts")]
    [InlineData("ExportVoucher.CodePrefix", "issues")]
    [InlineData("Warehouse.LowStockThreshold", "inventory")]
    public void SETTINGS_U_002_app_scoped_routes_use_stable_targets(string key, string expectedTarget)
    {
        var method = typeof(SettingsViewModel).GetMethod("ResolveAppSettingTarget", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var target = method!.Invoke(null, [key]) as string;

        target.Should().Be(expectedTarget);
    }

    [Fact]
    public void SETTINGS_U_003_generic_printing_templates_use_the_persisted_hotel_workspace()
    {
        var field = typeof(SettingsViewModel).GetField("HotelConfigurationRoutes", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();

        var routes = (IReadOnlyDictionary<string, string>)field!.GetValue(null)!;

        routes["print-voucher"].Should().Be("hotel-forms-printers");
        routes["print-invoice"].Should().Be("hotel-forms-printers");
        routes["print-debt"].Should().Be("hotel-forms-printers");
        routes["print-hr"].Should().Be("hotel-forms-printers");

        File.ReadAllText(ResolveSettingsViewModelSourcePath())
            .Should().Contain("targetPageKey: \"hotel-forms-printers\"");
    }

    [Fact]
    public void SETTINGS_U_045_maintenance_index_and_repair_routes_open_real_vfc_workflows()
    {
        var routePredicate = typeof(SettingsViewModel).GetMethod("IsMaintenanceDetailRoute", BindingFlags.NonPublic | BindingFlags.Static);
        routePredicate.Should().NotBeNull();

        foreach (var route in new[] { "maint-index", "maint-repair" })
        {
            routePredicate!.Invoke(null, [route]).Should().Be(true, $"{route} must open a dedicated maintenance detail instead of the generic read-only fallback");
        }

        routePredicate!.Invoke(null, ["not-a-maintenance-route"]).Should().Be(false);

        var builder = typeof(SettingsViewModel).GetMethod("BuildMaintenanceDetailAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        builder.Should().NotBeNull("maintenance routes must resolve to a page-specific detail builder");
    }

    [Fact]
    public void SETTINGS_U_046_system_remote_page_does_not_pretend_probe_settings_configure_the_api_listener()
    {
        var routesField = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        routesField.Should().NotBeNull();
        var routes = (IReadOnlyDictionary<string, string[]>)routesField!.GetValue(null)!;

        routes.Should().NotContainKey("system-remote");
        routes.Should().ContainKey("network-api-server");
        var source = File.ReadAllText(ResolveSettingsViewModelSourcePath());
        source.Should().Contain("\"API endpoint probe\"")
            .And.Contain("không cấu hình API listener")
            .And.Contain("Đích HTTP để kiểm tra kết nối; không khởi chạy API listener");
        typeof(SettingsViewModel).GetMethod("BuildSystemRemoteDetailAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Should().NotBeNull("the system page should show actual probe/firewall state and link to their owning providers");
    }

    [Fact]
    public void SETTINGS_U_004_unwired_runtime_settings_are_explicitly_read_only()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var runtimeUnwiredKeys = new[]
        {
            "Database.EnableAutoMigration", "GeneralLedger.AllowDeleteEntries"
        };

        foreach (var key in runtimeUnwiredKeys)
        {
            definitions[key].IsReadOnly.Should().BeTrue(key);
            definitions[key].Description.Should().Contain("provider runtime", key);
        }

        definitions["Database.CommandTimeout"].IsReadOnly.Should().BeFalse();
        definitions["Database.CommandTimeout"].DefaultValue.Should().Be("30");
        definitions["Database.EnableAutoMigration"].DefaultValue.Should().BeNull();

        definitions["Database.ConnectionString"].IsReadOnly.Should().BeTrue();
        definitions["Database.ConnectionString"].Description.Should().Contain("provider runtime");
        definitions["Network.ApiBaseUrl"].IsReadOnly.Should().BeFalse("the saved endpoint is consumed by the VFC network probe");
        definitions["Network.ApiPort"].IsReadOnly.Should().BeFalse();
        definitions["Network.EnableHttps"].IsReadOnly.Should().BeFalse();
        foreach (var key in new[] { "Network.AllowClientDiscovery", "Network.ScanIntervalSeconds" })
        {
            definitions[key].IsReadOnly.Should().BeTrue(key);
            definitions[key].DefaultValue.Should().BeNull(key);
            definitions[key].Description.Should().Contain("dịch vụ discovery/sync", key);
        }
        definitions["POS.ApiEndpoint"].IsReadOnly.Should().BeFalse();
        definitions["FO.ApiEndpoint"].IsReadOnly.Should().BeFalse();

        definitions["Security.MaxFailedLoginAttempts"].IsReadOnly.Should().BeFalse();
        definitions["Maintenance.UpdateChannel"].IsReadOnly.Should().BeFalse();
        definitions["Logs.LogLevel"].IsReadOnly.Should().BeFalse();
        definitions["Logs.RetentionDays"].IsReadOnly.Should().BeFalse();
        definitions["Logs.RetentionDays"].DefaultValue.Should().Be("14");
        definitions["Maintenance.UpdateChannel"].DefaultValue.Should().Be("Stable");
        definitions["Backup.RetentionCount"].IsReadOnly.Should().BeFalse();
        definitions["Appearance.Theme"].IsReadOnly.Should().BeFalse();
        definitions["VFC.AccountingDrivePath"].IsReadOnly.Should().BeFalse();
        definitions["Appearance.WallpaperPath"].IsReadOnly.Should().BeFalse();
        definitions["Appearance.ShowTaskbar"].IsReadOnly.Should().BeFalse();
        definitions["Warehouse.LowStockThreshold"].IsReadOnly.Should().BeFalse();
        definitions["GeneralLedger.CashAccount"].IsReadOnly.Should().BeFalse();

        definitions["Network.ApiBaseUrl"].DefaultValue.Should().BeEmpty("an unconfigured install must not advertise localhost as its API");
        definitions["POS.ApiEndpoint"].DefaultValue.Should().BeEmpty();
        definitions["FO.ApiEndpoint"].DefaultValue.Should().BeEmpty();
    }

    [Fact]
    public void SETTINGS_U_054_database_command_timeout_is_persisted_validated_and_applied_to_ef_commands()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == RuntimeDatabaseCommandTimeout.SettingKey);
        definition.IsReadOnly.Should().BeFalse();
        definition.DefaultValue.Should().Be(RuntimeDatabaseCommandTimeout.DefaultSeconds.ToString());

        var validator = new SettingsValidator();
        validator.Validate(definition, "4").IsValid.Should().BeFalse();
        validator.Validate(definition, "5").IsValid.Should().BeTrue();
        validator.Validate(definition, "600").IsValid.Should().BeTrue();
        validator.Validate(definition, "601").IsValid.Should().BeFalse();

        var runtimeTimeout = new RuntimeDatabaseCommandTimeout();
        runtimeTimeout.TrySet("75").Should().BeTrue();
        runtimeTimeout.Seconds.Should().Be(75);
        runtimeTimeout.TrySet("0").Should().BeFalse();
        runtimeTimeout.Seconds.Should().Be(75, "invalid values must not change the runtime policy");

        var command = new SqlCommand();
        var interceptor = new DatabaseCommandTimeoutInterceptor(runtimeTimeout);
        interceptor.ReaderExecuting(command, null!, default);
        command.CommandTimeout.Should().Be(75);
        interceptor.ScalarExecuting(command, null!, default);
        command.CommandTimeout.Should().Be(75);
        interceptor.NonQueryExecuting(command, null!, default);
        command.CommandTimeout.Should().Be(75);

        var field = typeof(SettingsViewModel).GetField("EditableSettingsByRoute", BindingFlags.NonPublic | BindingFlags.Static);
        var routes = (IReadOnlyDictionary<string, string[]>)field!.GetValue(null)!;
        routes["db-command-policy"].Should().Contain(RuntimeDatabaseCommandTimeout.SettingKey);
    }

    [Fact]
    public void SETTINGS_U_021_restore_confirmation_policy_is_editable_and_describes_the_permission_guard()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == "Security.RequireAdminConfirmation");

        definition.IsReadOnly.Should().BeFalse();
        definition.DefaultValue.Should().Be("true");
        definition.Description.Should().Contain("Database.Restore");
        definition.Description.Should().Contain("ghi audit");
    }

    [Fact]
    public void SETTINGS_U_022_general_ledger_period_settings_never_default_to_the_machine_month()
    {
        var periodKeys = new[]
        {
            "GeneralLedger.CurrentPeriod", "GeneralLedger.PeriodFromDate", "GeneralLedger.PeriodToDate",
            "GeneralLedger.PeriodStatus", "GeneralLedger.LastLockedDate", "GeneralLedger.LockedBy"
        };
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .Where(x => periodKeys.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        definitions.Should().HaveCount(periodKeys.Length);
        foreach (var key in periodKeys)
        {
            definitions[key].IsReadOnly.Should().BeTrue(key);
            definitions[key].DefaultValue.Should().BeNull(key);
            definitions[key].Description.Should().Contain("AccountingPeriods", key);
        }

        var missing = GeneralLedgerPeriodDisplay.From(null);
        missing.PeriodCode.Should().Be("Chưa cấu hình kỳ kế toán hiện tại");
        missing.Status.Should().Be("Chưa mở kỳ");

        var persisted = GeneralLedgerPeriodDisplay.From(new AccountingPeriod
        {
            PeriodCode = "2026-09",
            FromDate = new DateTime(2026, 9, 1),
            ToDate = new DateTime(2026, 9, 30),
            Status = ASMILE.BO.Core.Enums.AccountingPeriodStatus.Closed
        });
        persisted.PeriodCode.Should().Be("2026/09");
        persisted.FromDate.Should().Be("01/09/2026");
        persisted.ToDate.Should().Be("30/09/2026");
        persisted.Status.Should().Be("Đã đóng");
    }

    [Fact]
    public void SETTINGS_U_014_update_channel_is_editable_and_validated_against_signed_manifest_channels()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == "Maintenance.UpdateChannel");
        var validator = new SettingsValidator();

        definition.IsReadOnly.Should().BeFalse();
        validator.Validate(definition, "Stable").IsValid.Should().BeTrue();
        validator.Validate(definition, "Beta").IsValid.Should().BeTrue();
        validator.Validate(definition, "Internal").IsValid.Should().BeTrue();
        validator.Validate(definition, "Untrusted").IsValid.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_015_log_level_is_editable_and_validated_against_runtime_supported_levels()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == SettingsLogLevelPolicy.SettingKey);
        var validator = new SettingsValidator();

        definition.IsReadOnly.Should().BeFalse();
        definition.RequiresRestart.Should().BeFalse();
        validator.Validate(definition, "Debug").IsValid.Should().BeTrue();
        validator.Validate(definition, "Information").IsValid.Should().BeTrue();
        validator.Validate(definition, "Warning").IsValid.Should().BeTrue();
        validator.Validate(definition, "Error").IsValid.Should().BeTrue();
        validator.Validate(definition, "Verbose").IsValid.Should().BeFalse();
        validator.Validate(definition, null).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_024_theme_is_validated_against_the_runtime_supported_palettes()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == "Appearance.Theme");
        var validator = new SettingsValidator();

        foreach (var theme in new[] { "Light", "Dark", "System" })
            validator.Validate(definition, theme).IsValid.Should().BeTrue(theme);

        validator.Validate(definition, "Neon").IsValid.Should().BeFalse();
        validator.Validate(definition, null).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_025_boolean_settings_reject_non_boolean_values_and_stock_threshold_cannot_be_negative()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var validator = new SettingsValidator();

        foreach (var definition in definitions.Values.Where(x => x.ValueType == SettingValueType.Boolean))
        {
            validator.Validate(definition, "true").IsValid.Should().BeTrue(definition.Key);
            validator.Validate(definition, "false").IsValid.Should().BeTrue(definition.Key);
            validator.Validate(definition, "sometimes").IsValid.Should().BeFalse(definition.Key);
        }

        validator.Validate(definitions["Warehouse.LowStockThreshold"], "0").IsValid.Should().BeTrue();
        validator.Validate(definitions["Warehouse.LowStockThreshold"], "-1").IsValid.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_042_endpoint_settings_accept_safe_http_urls_and_reject_credentials_or_query_tokens()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var validator = new SettingsValidator();

        foreach (var key in new[] { "Network.ApiBaseUrl", "POS.ApiEndpoint", "FO.ApiEndpoint" })
        {
            validator.Validate(definitions[key], string.Empty).IsValid.Should().BeTrue(key);
            validator.Validate(definitions[key], "https://api.example.com/v1").IsValid.Should().BeTrue(key);
            validator.Validate(definitions[key], "http://localhost:5001").IsValid.Should().BeTrue(key);
            validator.Validate(definitions[key], "https://user:password@api.example.com").IsValid.Should().BeFalse(key);
            validator.Validate(definitions[key], "https://api.example.com?token=secret").IsValid.Should().BeFalse(key);
            validator.Validate(definitions[key], "file:///tmp/api").IsValid.Should().BeFalse(key);
        }
    }

    [Theory]
    [InlineData("Debug", LogEventLevel.Debug)]
    [InlineData("Information", LogEventLevel.Information)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("Error", LogEventLevel.Error)]
    public void SETTINGS_U_016_saved_log_level_maps_to_the_runtime_serilog_switch(string value, LogEventLevel expected)
    {
        var levelSwitch = new LoggingLevelSwitch();

        SettingsLogLevelPolicy.TryApply(levelSwitch, value).Should().BeTrue();

        levelSwitch.MinimumLevel.Should().Be(expected);
    }

    [Fact]
    public void SETTINGS_U_017_unsupported_log_level_does_not_change_the_runtime_switch()
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Warning);

        SettingsLogLevelPolicy.TryApply(levelSwitch, "Verbose").Should().BeFalse();

        levelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void SETTINGS_U_008_vfc_logical_drives_do_not_default_to_fake_physical_paths()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        var dataRoot = definitions["Storage.DataRootPath"];
        Path.IsPathRooted(dataRoot.DefaultValue).Should().BeTrue();
        dataRoot.DefaultValue.Should().Contain(Path.Combine("ASMILE-BO", "Data"));
        var exportRoot = definitions["Storage.ExportRootPath"];
        exportRoot.IsReadOnly.Should().BeFalse();
        exportRoot.ValueType.Should().Be(SettingValueType.Path);
        Path.IsPathRooted(exportRoot.DefaultValue).Should().BeTrue();

        foreach (var key in new[]
        {
            "VFC.SystemDrivePath", "VFC.AccountingDrivePath", "VFC.SalesDrivePath",
            "VFC.HrDrivePath", "VFC.LogDrivePath"
        })
        {
            definitions[key].DefaultValue.Should().BeEmpty(key);
            definitions[key].Description.Should().Contain("danh mục logic", key);
        }
    }

    [Fact]
    public void SETTINGS_U_009_vfc_module_configuration_uses_the_canonical_system_settings_page()
    {
        var moduleSettings = new ModuleSettingsService(null!, null!);
        moduleSettings.GetDefinition("virtual-file-center").Should().BeNull(
            "the old module editor exposed settings that had no runtime consumer");

        var resolver = typeof(SettingsViewModel).GetMethod(
            "ResolveModuleSettingsCategory",
            BindingFlags.NonPublic | BindingFlags.Static);
        resolver.Should().NotBeNull();
        resolver!.Invoke(null, ["virtual-file-center"]).Should().Be("vfc");
        resolver.Invoke(null, ["vfc"]).Should().Be("vfc");
    }

    [Fact]
    public void SETTINGS_U_010_home_and_recommended_navigation_commands_are_generated()
    {
        var viewModelType = typeof(SettingsViewModel);
        var commandProperties = new[]
        {
            "OpenMachineInfoCommand",
            "OpenWorkstationSettingsCommand",
            "OpenInfrastructureHealthCommand",
            "OpenBackupConfigurationCommand",
            "OpenServiceStatusCommand"
        };

        foreach (var commandProperty in commandProperties)
        {
            viewModelType.GetProperty(commandProperty, BindingFlags.Instance | BindingFlags.Public)
                .Should()
                .NotBeNull(commandProperty);
        }
    }

    [Fact]
    public void SETTINGS_U_005_read_only_definition_is_not_editable_in_the_generic_editor()
    {
        var definition = new SettingDefinitionDto
        {
            Key = "Network.ApiBaseUrl",
            DisplayName = "API Base URL",
            IsReadOnly = true,
            ValueType = SettingValueType.String
        };

        var item = new EditableSettingItemViewModel(definition, "https://localhost");

        item.IsReadOnly.Should().BeTrue();
        item.CanEdit.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_011_editor_identifies_default_and_persisted_values()
    {
        var definition = new SettingDefinitionDto
        {
            Key = "Storage.DataRootPath",
            DisplayName = "Data root path",
            ValueType = SettingValueType.Path
        };

        var fallback = new EditableSettingItemViewModel(definition, "C:\\ProgramData\\ASMILE-BO\\Data");
        var persisted = new EditableSettingItemViewModel(definition, "D:\\ASMILE\\Data", isPersisted: true);

        fallback.ValueSourceText.Should().Be("Giá trị mặc định · chưa lưu");
        persisted.ValueSourceText.Should().Be("Đã lưu trong cơ sở dữ liệu");
    }

    [Fact]
    public void SETTINGS_U_012_backup_schedule_is_validated_by_the_runtime_scheduler_parser()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == "Backup.Schedule");
        var validator = new SettingsValidator();

        definition.IsRequired.Should().BeTrue();
        validator.Validate(definition, "*/15 * * * *").IsValid.Should().BeTrue();
        validator.Validate(definition, "not-a-cron").IsValid.Should().BeFalse();
        validator.Validate(definition, "").IsValid.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_042_sql_backup_path_explains_the_engine_identity_permission_boundary()
    {
        var settings = new SettingsRegistry().GetDefaultDefinitions()
            .Where(x => x.Key is "Backup.BackupPath" or "Backup.EnableAutoBackup")
            .ToDictionary(x => x.Key);
        var helper = typeof(SettingsViewModel).GetMethod("FormatSqlBackupPathStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        var appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ASMILE-BO", "Backups");
        var status = (string)helper.Invoke(null, new object?[] { appPath, true })!;

        settings["Backup.BackupPath"].DefaultValue.Should().Be(appPath);
        settings["Backup.BackupPath"].Description.Should().Contain("tài khoản dịch vụ SQL Server");
        settings["Backup.BackupPath"].Description.Should().Contain("share UNC");
        settings["Backup.EnableAutoBackup"].DefaultValue.Should().Be("false");
        settings["Backup.EnableAutoBackup"].Description.Should().Contain("backup thật");
        status.Should().Contain("quyền của ứng dụng");
        status.Should().Contain("quyền SQL Server");
    }

    [Fact]
    public void SETTINGS_U_043_home_backup_row_reports_the_runtime_scheduler_state_not_the_last_backup_time()
    {
        var helper = typeof(SettingsViewModel).GetMethod("FormatBackupScheduleStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        var nextRun = DateTime.UtcNow.AddHours(2);
        var enabled = new BackgroundSchedulerDefinitionDto
        {
            Code = "DATABASE_BACKUP",
            IsEnabled = true,
            CronExpression = "0 0 * * *",
            NextRunAtUtc = nextRun
        };
        var disabled = new BackgroundSchedulerDefinitionDto { Code = "DATABASE_BACKUP", IsEnabled = false };
        var paused = new BackgroundSchedulerDefinitionDto { Code = "DATABASE_BACKUP", IsEnabled = true, IsPaused = true };

        ((string)helper.Invoke(null, new object?[] { true, enabled })!).Should().Contain("Đang bật · lần tới");
        ((string)helper.Invoke(null, new object?[] { true, disabled })!).Should().Be("Đang tắt");
        ((string)helper.Invoke(null, new object?[] { true, paused })!).Should().Be("Tạm dừng");
        ((string)helper.Invoke(null, new object?[] { false, null })!).Should().Be("Scheduler không khả dụng");
    }

    [Fact]
    public void SETTINGS_U_044_home_api_status_requires_a_probe_result()
    {
        var helper = typeof(SettingsViewModel).GetMethod("ResolveApiStatus", BindingFlags.NonPublic | BindingFlags.Static)!;

        ((string)helper.Invoke(null, new object?[] { true, "Đã kết nối" })!).Should().Be("Đã kết nối");
        ((string)helper.Invoke(null, new object?[] { true, null })!).Should().Be("Probe không trả kết quả");
        ((string)helper.Invoke(null, new object?[] { false, "Đã kết nối" })!).Should().Be("Probe không khả dụng");
    }

    [Fact]
    public void SETTINGS_U_036_session_timeout_validation_matches_the_runtime_clamp_range()
    {
        var definition = new SettingsRegistry().GetDefaultDefinitions()
            .Single(x => x.Key == "Security.SessionTimeoutMinutes");
        definition.Description.Should().Contain("không có thao tác").And.Contain("gia hạn khi có hoạt động");
        var validator = new SettingsValidator();

        validator.Validate(definition, "5").IsValid.Should().BeTrue();
        validator.Validate(definition, "1440").IsValid.Should().BeTrue();
        validator.Validate(definition, "4").IsValid.Should().BeFalse();
        validator.Validate(definition, "1441").IsValid.Should().BeFalse();
    }

    [Fact]
    public void SETTINGS_U_037_hr_person_defaults_expose_only_runtime_wired_fields()
    {
        var definition = new ModuleSettingsService(null!, null!).GetDefinition("departments")!;
        var settings = definition.Sections.SelectMany(x => x.Settings).ToDictionary(x => x.Key);

        new ModuleSettingItemViewModel(settings["defaultDepartment"], settings["defaultDepartment"].DefaultValue)
            .IsEnabled.Should().BeTrue();
        new ModuleSettingItemViewModel(settings["defaultPosition"], settings["defaultPosition"].DefaultValue)
            .IsEnabled.Should().BeTrue();

        foreach (var key in new[]
        {
            "defaultHrStatus", "requirePhoneEmail", "allowLockInactiveEmployee",
            "departmentPermission", "departmentApprover"
        })
        {
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue)
                .IsEnabled.Should().BeFalse(key);
        }

        foreach (var key in new[] { "autoEmployeeCode", "employeeCodePattern" })
        {
            new ModuleSettingItemViewModel(settings[key], settings[key].DefaultValue)
                .IsEnabled.Should().BeTrue(key);
        }

        new ModuleSettingsValidationService().Validate(definition, new Dictionary<string, string>
        {
            ["autoEmployeeCode"] = "true",
            ["employeeCodePattern"] = "NV-{00000}-{00}"
        }).Should().ContainKey("employeeCodePattern");
    }

    [Fact]
    public void SETTINGS_U_038_hr_department_default_uses_active_context_then_configured_lookup_then_first()
    {
        var resolver = typeof(DepartmentPeoplePageViewModel).GetMethod(
            "ResolveNewPersonDepartmentId",
            BindingFlags.NonPublic | BindingFlags.Static);
        resolver.Should().NotBeNull();
        var departments = new[]
        {
            new LookupItemDto { Id = 10, Code = "OPS", Name = "Operations" },
            new LookupItemDto { Id = 20, Code = "FIN", Name = "Finance" }
        };

        resolver!.Invoke(null, new object?[] { 20, "OPS", departments }).Should().Be(20);
        resolver.Invoke(null, new object?[] { 999, "ops", departments }).Should().Be(10);
        resolver.Invoke(null, new object?[] { null, "missing", departments }).Should().Be(10);
    }

    [Fact]
    public async Task SETTINGS_U_039_auto_employee_code_uses_saved_pattern_and_real_existing_codes()
    {
        var settings = new InMemoryModuleSettingsService();
        settings.Set("Module.departments.autoEmployeeCode", "true");
        settings.Set("Module.departments.employeeCodePattern", "NV-{00000}");
        var people = new FakeEmployeeCodePersonRepository(new Person { Id = 1, PersonCode = "NV-00002" });
        var service = new PersonAppService(
            people,
            new FakeEmployeeCodeDepartmentRepository(),
            new DirectSettingsUnitOfWork(),
            new NoopSettingsAuditLog(),
            settings);

        var saved = await service.SaveAsync(new PersonDto
        {
            FullName = "Test Employee",
            DepartmentId = 7
        });

        saved.PersonCode.Should().Be("NV-00003");
        people.People.Should().ContainSingle(x => x.PersonCode == "NV-00003");
    }

    [Fact]
    public void SETTINGS_U_013_physical_paths_require_absolute_or_unc_paths()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var validator = new SettingsValidator();
        var storagePath = definitions["Storage.DataRootPath"];
        var exportPath = definitions["Storage.ExportRootPath"];
        var vfcPath = definitions["VFC.AccountingDrivePath"];

        validator.Validate(storagePath, Path.GetTempPath()).IsValid.Should().BeTrue();
        validator.Validate(exportPath, Path.GetTempPath()).IsValid.Should().BeTrue();
        validator.Validate(exportPath, @"relative\exports").IsValid.Should().BeFalse();
        validator.Validate(storagePath, @"relative\data").IsValid.Should().BeFalse();
        validator.Validate(vfcPath, @"\\fileserver\accounting").IsValid.Should().BeTrue();
        validator.Validate(vfcPath, string.Empty).IsValid.Should().BeTrue("VFC physical paths are optional");
    }

    [Fact]
    public void SETTINGS_U_006_stock_voucher_pattern_is_rendered_and_sequenced_from_persisted_format()
    {
        var date = new DateTime(2026, 9, 30);
        var pattern = InventoryDocumentCodePattern.Normalize("PN-{yyyyMMdd}-{0000}", "PN-{yyyyMMdd}-{0000}");

        InventoryDocumentCodePattern.GetNextSequence(
            pattern,
            date,
            ["PN-20260930-0001", "PN-20260930-0009", "PN-20260929-0099"])
            .Should().Be(10);

        InventoryDocumentCodePattern.Render(pattern, date, 10)
            .Should().Be("PN-20260930-0010");
    }

    [Fact]
    public void SETTINGS_U_007_invalid_stock_voucher_pattern_fails_closed_to_default()
    {
        InventoryDocumentCodePattern.Normalize("PN-{yyyyMMdd}-{0000}-{00}", "PN-{yyyyMMdd}-{0000}")
            .Should().Be("PN-{yyyyMMdd}-{0000}");
    }

    private static string ResolveSettingsViewModelSourcePath()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("ASMILE_BO_SOLUTION_ROOT");
        var startingDirectories = new[]
        {
            configuredRoot,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        }.Where(path => !string.IsNullOrWhiteSpace(path));

        foreach (var startingDirectory in startingDirectories)
        {
            var directory = new DirectoryInfo(startingDirectory!);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "ASMILE.BO.App", "ViewModels", "SettingsViewModel.cs");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        throw new FileNotFoundException(
            "Could not locate ASMILE.BO.App/ViewModels/SettingsViewModel.cs. Set ASMILE_BO_SOLUTION_ROOT to the repository root.");
    }

    private sealed class FakeEmployeeCodePersonRepository(params Person[] people) : IPersonRepository
    {
        private readonly List<Person> _people = people.ToList();
        public IReadOnlyList<Person> People => _people;

        public Task<IReadOnlyList<Person>> SearchAsync(int? departmentId, string keyword, bool? isActive = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Person>>(_people);

        public Task<IReadOnlyList<string>> GetCodesStartingWithAsync(string prefix, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(_people.Where(x => x.PersonCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Select(x => x.PersonCode).ToArray());

        public Task<bool> ExistsCodeAsync(string personCode, int excludingId = 0, CancellationToken cancellationToken = default)
            => Task.FromResult(_people.Any(x => x.Id != excludingId && string.Equals(x.PersonCode, personCode, StringComparison.OrdinalIgnoreCase)));

        public Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_people.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Person>>(_people);

        public Task AddAsync(Person entity, CancellationToken cancellationToken = default)
        {
            entity.Id = _people.Count == 0 ? 1 : _people.Max(x => x.Id) + 1;
            _people.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Person entity) { }
        public void Delete(Person entity) => _people.Remove(entity);
    }

    private sealed class FakeEmployeeCodeDepartmentRepository : IDepartmentRepository
    {
        public Task<IReadOnlyList<Department>> SearchAsync(string keyword, bool? isActive = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Department>>([new Department { Id = 7, DepartmentCode = "OPS", DepartmentName = "Operations" }]);

        public Task<bool> ExistsCodeAsync(string departmentCode, int excludingId = 0, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> HasChildrenAsync(int departmentId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> HasPeopleAsync(int departmentId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Department?>(new Department { Id = id, DepartmentCode = "OPS", DepartmentName = "Operations" });

        public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default)
            => SearchAsync(string.Empty, null, cancellationToken);

        public Task AddAsync(Department entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Department entity) { }
        public void Delete(Department entity) { }
    }

    private sealed class DirectSettingsUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
            => operation(cancellationToken);
    }

    private sealed class NoopSettingsAuditLog : IAuditLogService
    {
        public Task LogAsync(string action, string detail, string username = "system", CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class InMemoryWarehouseAppService(params LookupItemDto[] warehouses) : IWarehouseAppService
    {
        public Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LookupItemDto>>(warehouses);

        public Task<IReadOnlyList<WarehouseDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WarehouseDto>>([]);

        public Task<WarehouseDto> SaveAsync(WarehouseDto dto, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class InMemoryAppModuleRepository(AppModule app) : IAppModuleRepository
    {
        public AppModule? App { get; } = app;
        public int UpsertCount { get; private set; }

        public Task<IReadOnlyList<AppModule>> GetAppsAsync()
            => Task.FromResult<IReadOnlyList<AppModule>>([App!]);

        public Task<AppModule?> GetAppAsync(string appKey)
            => Task.FromResult(App is not null && string.Equals(App.AppKey, appKey, StringComparison.OrdinalIgnoreCase) ? App : null);

        public Task UpsertAppAsync(AppModule value)
        {
            UpsertCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryModuleSettingsService : ISettingsService
    {
        private readonly Dictionary<string, SettingValueDto> _values = new(StringComparer.OrdinalIgnoreCase);

        public void Set(string key, string value)
            => _values[key] = new SettingValueDto { Key = key, Scope = SettingScope.App, ScopeTarget = "receipts", Value = value, IsPersisted = true };

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingDefinitionDto>>([]);

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingValueDto>>(_values.Values.ToArray());

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

        public Task SaveValueAsync(SaveSettingValueRequest request)
        {
            SaveValues([request]);
            return Task.CompletedTask;
        }

        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests)
        {
            SaveValues(requests);
            return Task.CompletedTask;
        }

        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        private void SaveValues(IEnumerable<SaveSettingValueRequest> requests)
        {
            foreach (var request in requests)
            {
                _values[request.Key] = new SettingValueDto
                {
                    Key = request.Key,
                    Scope = request.Scope,
                    ScopeTarget = request.ScopeTarget,
                    Value = request.Value,
                    IsPersisted = true,
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }
    }

    private sealed class InMemoryModuleSettingsRepository : ISettingsRepository
    {
        private readonly Dictionary<string, SettingDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<SettingDefinition>> GetDefinitionsAsync()
            => Task.FromResult<IReadOnlyList<SettingDefinition>>(_definitions.Values.ToArray());

        public Task<IReadOnlyList<SettingValue>> GetValuesAsync()
            => Task.FromResult<IReadOnlyList<SettingValue>>([]);

        public Task<SettingValue?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult<SettingValue?>(null);

        public Task UpsertValueAsync(SettingValue value) => Task.CompletedTask;
        public Task UpsertValuesAsync(IReadOnlyList<SettingValue> values) => Task.CompletedTask;
        public Task CommitValuesAndAuditAsync(IReadOnlyList<SettingValue> values, IReadOnlyList<SettingChangeLog> changeLogs) => Task.CompletedTask;

        public Task UpsertDefinitionAsync(SettingDefinition definition)
        {
            _definitions[definition.Key] = definition;
            return Task.CompletedTask;
        }
    }
}
