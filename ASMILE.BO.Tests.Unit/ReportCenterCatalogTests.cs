using System.Text;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.SettingsEngine;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using System.Collections;
using System.Reflection;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReportCenterCatalogTests
{
    [Fact]
    public void REPORT_CENTER_U_001_fallback_catalog_covers_every_supported_engine_code()
    {
        var viewModelType = typeof(ReportCenterViewModel);
        var supportedField = viewModelType.GetField("SupportedReportCodes", BindingFlags.NonPublic | BindingFlags.Static);
        var fallbackMethod = viewModelType.GetMethod("BuildFallbackCatalog", BindingFlags.NonPublic | BindingFlags.Static);

        supportedField.Should().NotBeNull();
        fallbackMethod.Should().NotBeNull();

        var supported = ((IEnumerable<string>)supportedField!.GetValue(null)!)
            .Select(code => code.Trim().ToUpperInvariant())
            .ToArray();
        var fallbackItems = ((IEnumerable)fallbackMethod!.Invoke(null, null)!)
            .Cast<object>()
            .ToArray();
        var fallbackCodes = fallbackItems
            .Select(item => item.GetType().GetProperty("Code")!.GetValue(item)!.ToString()!.Trim().ToUpperInvariant())
            .ToArray();

        fallbackCodes.Should().OnlyHaveUniqueItems();
        fallbackCodes.Should().BeEquivalentTo(supported);
    }

    [Fact]
    public void REPORT_CENTER_U_002_general_ledger_export_permissions_are_writable_and_scoped_to_ledger_reports()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions();
        definitions.Should().ContainSingle(x => x.Key == "GeneralLedger.AllowExportExcel" && !x.IsReadOnly);
        definitions.Should().ContainSingle(x => x.Key == "GeneralLedger.AllowExportPdf" && !x.IsReadOnly);
        var exportFormat = definitions.Should().ContainSingle(x => x.Key == "GeneralLedger.DefaultExportFormat").Which;
        exportFormat.IsReadOnly.Should().BeFalse();
        exportFormat.DefaultValue.Should().Be("Excel");
        var validator = new SettingsValidator();
        validator.Validate(exportFormat, "Excel").IsValid.Should().BeTrue();
        validator.Validate(exportFormat, "PDF").IsValid.Should().BeTrue();
        validator.Validate(exportFormat, "CSV").IsValid.Should().BeFalse();

        var classifier = typeof(ReportCenterViewModel).GetMethod("IsGeneralLedgerReport", BindingFlags.NonPublic | BindingFlags.Static);
        classifier.Should().NotBeNull();
        classifier!.Invoke(null, ["GENERAL_LEDGER"]).Should().Be(true);
        classifier.Invoke(null, ["TRIAL_BALANCE"]).Should().Be(true);
        classifier.Invoke(null, ["INVENTORY_SUMMARY"]).Should().Be(false);

        var settingParser = typeof(ReportCenterViewModel).GetMethod("IsSettingEnabled", BindingFlags.NonPublic | BindingFlags.Static);
        settingParser.Should().NotBeNull();
        settingParser!.Invoke(null, ["false"]).Should().Be(false);
        settingParser.Invoke(null, ["true"]).Should().Be(true);
        settingParser.Invoke(null, [null]).Should().Be(true, "missing persisted values use the registry's enabled-by-default policy");
    }

    [Fact]
    public void REPORT_CENTER_U_003_pdf_signature_and_footer_settings_are_writable_and_validated()
    {
        var definitions = new SettingsRegistry().GetDefaultDefinitions()
            .Where(x => x.Key is "GeneralLedger.ShowAccountantSignature"
                or "GeneralLedger.ShowDirectorSignature"
                or "GeneralLedger.ReportFooterNote")
            .ToDictionary(x => x.Key);
        definitions.Should().HaveCount(3);
        definitions.Values.Should().OnlyContain(x => !x.IsReadOnly);

        var validator = new SettingsValidator();
        validator.Validate(definitions["GeneralLedger.ShowAccountantSignature"], "false").IsValid.Should().BeTrue();
        validator.Validate(definitions["GeneralLedger.ShowDirectorSignature"], "true").IsValid.Should().BeTrue();
        validator.Validate(definitions["GeneralLedger.ReportFooterNote"], new string('x', 500)).IsValid.Should().BeTrue();
        validator.Validate(definitions["GeneralLedger.ReportFooterNote"], new string('x', 501)).IsValid.Should().BeFalse();
        validator.Validate(definitions["GeneralLedger.ReportFooterNote"], "footer\nsecond line").IsValid.Should().BeFalse();

        definitions["GeneralLedger.ShowAccountantSignature"].Description.Should().Contain("PDF Sổ cái và Cân đối phát sinh");
        definitions["GeneralLedger.ReportFooterNote"].Description.Should().Contain("PDF Sổ cái và Cân đối phát sinh");
    }

    [Fact]
    public void REPORT_CENTER_U_004_pdf_output_uses_footer_and_selected_signature_lines()
    {
        var engineType = typeof(ReportEngine);
        var writerType = engineType.GetNestedType("SimplePdfWriter", BindingFlags.NonPublic);
        var presentationType = engineType.GetNestedType("LedgerPdfPresentation", BindingFlags.NonPublic);
        writerType.Should().NotBeNull();
        presentationType.Should().NotBeNull();
        var write = writerType!.GetMethod("Write", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        write.Should().NotBeNull();
        var presentation = Activator.CreateInstance(
            presentationType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: ["End of report", true, false],
            culture: null);
        var report = new ReportResultDto
        {
            ReportCode = "GENERAL_LEDGER",
            ReportName = "General ledger",
            GeneratedAt = new DateTime(2026, 10, 8),
            Columns = [new ReportColumnDto { Name = "Account", DisplayName = "Account" }],
            Rows = Enumerable.Range(1, 45)
                .Select(index => new ReportRowDto { Values = new Dictionary<string, object?> { ["Account"] = $"100{index}" } })
                .ToArray(),
            RowCount = 45
        };

        var pdf = (byte[])write!.Invoke(null, [report, presentation])!;
        var content = Encoding.ASCII.GetString(pdf);
        content.Should().Contain("End of report");
        content.Should().Contain("Ke toan");
        content.Should().NotContain("Giam doc");
        content.Should().Contain("/Count 2", "the footer and signature must remain visible after the report spills onto a second page");
    }
}
