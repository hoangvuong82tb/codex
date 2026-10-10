using System.Runtime.InteropServices;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Read-only adapter for the Windows Firewall COM policy provider.
/// No mutation API is exposed from this service.
/// </summary>
public sealed class WindowsFirewallService : IWindowsFirewallService
{
    private const int MaxRules = 250;

    public Task<WindowsFirewallSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(new WindowsFirewallSnapshotDto
            {
                IsAvailable = false,
                ProviderStatus = "Windows Firewall provider chỉ khả dụng trên Windows."
            });
        }

        try
        {
            var policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (policyType is null)
            {
                return Task.FromResult(new WindowsFirewallSnapshotDto
                {
                    IsAvailable = false,
                    ProviderStatus = "Không tìm thấy HNetCfg.FwPolicy2."
                });
            }

            var policy = Activator.CreateInstance(policyType);
            if (policy is null)
            {
                return Task.FromResult(new WindowsFirewallSnapshotDto
                {
                    IsAvailable = false,
                    ProviderStatus = "Không khởi tạo được Windows Firewall provider."
                });
            }

            try
            {
                dynamic dynamicPolicy = policy;
                var rules = new List<WindowsFirewallRuleDto>();
                dynamic rawRules = dynamicPolicy.Rules;
                try
                {
                    foreach (var rule in rawRules)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            dynamic dynamicRule = rule;
                            rules.Add(new WindowsFirewallRuleDto
                            {
                                Name = ReadString(dynamicRule.Name),
                                Direction = ReadEnumName(dynamicRule.Direction),
                                Action = ReadEnumName(dynamicRule.Action),
                                IsEnabled = ReadBool(dynamicRule.Enabled),
                                Protocol = ReadEnumName(dynamicRule.Protocol),
                                LocalPorts = ReadString(dynamicRule.LocalPorts),
                                ApplicationName = ReadString(dynamicRule.ApplicationName),
                                ServiceName = ReadString(dynamicRule.ServiceName)
                            });
                        }
                        finally
                        {
                            if (Marshal.IsComObject(rule))
                            {
                                Marshal.FinalReleaseComObject(rule);
                            }
                        }

                        if (rules.Count >= MaxRules)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    if (Marshal.IsComObject(rawRules))
                    {
                        Marshal.FinalReleaseComObject(rawRules);
                    }
                }

                return Task.FromResult(new WindowsFirewallSnapshotDto
                {
                    IsAvailable = true,
                    ProviderStatus = "Đã đọc từ Windows Firewall policy.",
                    RuleCount = rules.Count,
                    IsTruncated = rules.Count >= MaxRules,
                    Rules = rules
                });
            }
            finally
            {
                if (Marshal.IsComObject(policy))
                {
                    Marshal.FinalReleaseComObject(policy);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Task.FromResult(new WindowsFirewallSnapshotDto
            {
                IsAvailable = false,
                ProviderStatus = $"Firewall provider lỗi {ex.GetType().Name}."
            });
        }
    }

    private static string ReadString(object? value)
        => Convert.ToString(value) ?? string.Empty;

    private static string ReadEnumName(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value.GetType().IsEnum ? value.ToString() ?? string.Empty : Convert.ToString(value) ?? string.Empty;
    }

    private static bool ReadBool(object? value)
        => value is bool boolValue ? boolValue : bool.TryParse(Convert.ToString(value), out var parsed) && parsed;
}
