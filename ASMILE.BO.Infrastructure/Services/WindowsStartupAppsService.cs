using Microsoft.Win32;
using System.Security;
using System.Runtime.Versioning;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Read-only view of the current Windows startup sources.
/// It deliberately exposes no registry or filesystem mutation API.
/// </summary>
public sealed class WindowsStartupAppsService : IWindowsStartupAppsService
{
    private const int MaxEntries = 500;
    private const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public Task<WindowsStartupAppsSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(new WindowsStartupAppsSnapshotDto
            {
                IsAvailable = false,
                ProviderStatus = "Windows startup provider chỉ khả dụng trên Windows."
            });
        }

        var entries = new List<WindowsStartupAppEntryDto>();
        var diagnostics = new List<string>();
        ReadRunKey(RegistryHive.CurrentUser, RegistryView.Default, "HKCU Run", entries, diagnostics, cancellationToken);
        ReadRunKey(RegistryHive.LocalMachine, RegistryView.Registry64, "HKLM Run 64-bit", entries, diagnostics, cancellationToken);
        ReadRunKey(RegistryHive.LocalMachine, RegistryView.Registry32, "HKLM Run 32-bit", entries, diagnostics, cancellationToken);
        ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "User Startup folder", entries, diagnostics, cancellationToken);
        ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Common Startup folder", entries, diagnostics, cancellationToken);

        var distinctEntries = entries
            .GroupBy(x => $"{x.Source}\u001F{x.Name}\u001F{x.Command}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(MaxEntries)
            .ToList();
        var status = diagnostics.Count == 0
            ? $"Đã đọc {distinctEntries.Count} startup entry từ Windows Registry/Startup folder."
            : $"Đã đọc {distinctEntries.Count} startup entry; một số nguồn không đọc được: {string.Join(", ", diagnostics.Distinct(StringComparer.OrdinalIgnoreCase))}.";

        return Task.FromResult(new WindowsStartupAppsSnapshotDto
        {
            IsAvailable = true,
            ProviderStatus = status,
            Entries = distinctEntries
        });
    }

    [SupportedOSPlatform("windows")]
    private static void ReadRunKey(
        RegistryHive hive,
        RegistryView view,
        string source,
        ICollection<WindowsStartupAppEntryDto> entries,
        ICollection<string> diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var runKey = baseKey.OpenSubKey(RunSubKey, writable: false);
            if (runKey is null)
            {
                return;
            }

            foreach (var valueName in runKey.GetValueNames())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var command = runKey.GetValue(valueName, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(valueName) || string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }

                entries.Add(new WindowsStartupAppEntryDto
                {
                    Name = valueName,
                    Command = command,
                    Source = source
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            diagnostics.Add(source);
        }
        catch (IOException)
        {
            diagnostics.Add(source);
        }
        catch (SecurityException)
        {
            diagnostics.Add(source);
        }
        catch (ArgumentException)
        {
            diagnostics.Add(source);
        }
        catch (NotSupportedException)
        {
            diagnostics.Add(source);
        }
    }

    private static void ReadStartupFolder(
        string folder,
        string source,
        ICollection<WindowsStartupAppEntryDto> entries,
        ICollection<string> diagnostics,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        try
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                entries.Add(new WindowsStartupAppEntryDto
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Command = file,
                    Source = source
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            diagnostics.Add(source);
        }
        catch (IOException)
        {
            diagnostics.Add(source);
        }
        catch (SecurityException)
        {
            diagnostics.Add(source);
        }
    }
}
