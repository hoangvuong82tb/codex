using System.ComponentModel;
using System.Runtime.InteropServices;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Reads the local Windows Service Control Manager without inventing service
/// rows or heartbeat timestamps. Restart remains intentionally fail-closed
/// because it is a privileged, potentially disruptive operation.
/// </summary>
public sealed class WindowsServiceStatusService : IServiceStatusService
{
    private const uint ScManagerEnumerateService = 0x0004;
    private const uint ServiceWin32 = 0x00000030;
    private const uint ServiceStateAll = 0x00000003;
    private const int ScEnumProcessInfoLevel = 0;
    private const int ErrorMoreData = 122;
    private const uint ScManagerConnect = 0x0001;
    private const uint ServiceQueryStatus = 0x0004;
    private const uint ServiceStart = 0x0010;
    private const uint ServiceStop = 0x0020;
    private const uint ServiceControlStop = 0x00000001;
    private const uint ServiceStopped = 1;
    private const uint ServiceRunning = 4;
    private const uint ServiceStopPending = 3;
    private const uint ServiceStartPending = 2;

    private readonly ServiceSupervisorOptions _options;

    public WindowsServiceStatusService(IOptions<ServiceSupervisorOptions>? options = null)
    {
        _options = options?.Value ?? new ServiceSupervisorOptions();
    }

    public Task<IReadOnlyList<ServiceStatusDto>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(Array.Empty<ServiceStatusDto>());
        }

        try
        {
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(EnumerateServices(cancellationToken));
        }
        catch (DllNotFoundException)
        {
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(Array.Empty<ServiceStatusDto>());
        }
        catch (EntryPointNotFoundException)
        {
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(Array.Empty<ServiceStatusDto>());
        }
        catch (Win32Exception)
        {
            return Task.FromResult<IReadOnlyList<ServiceStatusDto>>(Array.Empty<ServiceStatusDto>());
        }
    }

    public Task RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_options.AllowRestart)
        {
            throw new InvalidOperationException(
                "Restart service đang bị khóa. Bật ServiceSupervisor:AllowRestart và allow-list service cụ thể trước khi sử dụng.");
        }

        if (string.IsNullOrWhiteSpace(serviceName)
            || !_options.AllowedServiceNames.Contains(serviceName, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Service không nằm trong allow-list restart.");
        }

        return RestartServiceCoreAsync(serviceName, cancellationToken);
    }

    private async Task RestartServiceCoreAsync(string serviceName, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Windows Service Control Manager chỉ có trên Windows.");
        }

        var manager = OpenSCManager(null, null, ScManagerConnect);
        if (manager == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            var service = OpenService(manager, serviceName, ServiceQueryStatus | ServiceStart | ServiceStop);
            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Không mở được service '{serviceName}'.");
            }

            try
            {
                var state = QueryState(service);
                if (state is not ServiceStopped)
                {
                    if (!ControlService(service, ServiceControlStop, out _)
                        && Marshal.GetLastWin32Error() != 1062)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Không dừng được service '{serviceName}'.");
                    }

                    await WaitForStateAsync(service, ServiceStopped, cancellationToken);
                }

                if (!StartService(service, 0, IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Không khởi động được service '{serviceName}'.");
                }

                await WaitForStateAsync(service, ServiceRunning, cancellationToken);
            }
            finally
            {
                CloseServiceHandle(service);
            }
        }
        finally
        {
            CloseServiceHandle(manager);
        }
    }

    private async Task WaitForStateAsync(IntPtr service, uint expectedState, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Clamp(_options.RestartTimeoutSeconds, 1, 300));
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = QueryState(service);
            if (state == expectedState)
            {
                return;
            }

            if (state is not ServiceStopPending and not ServiceStartPending)
            {
                throw new InvalidOperationException($"Service chuyển sang trạng thái không mong đợi: {MapStatus(state)}.");
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"Service không đạt trạng thái {MapStatus(expectedState)} trong thời gian cho phép.");
    }

    private static uint QueryState(IntPtr service)
    {
        var size = Marshal.SizeOf<ServiceStatusProcess>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!QueryServiceStatusEx(service, ScEnumProcessInfoLevel, buffer, (uint)size, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return Marshal.PtrToStructure<ServiceStatusProcess>(buffer).CurrentState;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IReadOnlyList<ServiceStatusDto> EnumerateServices(CancellationToken cancellationToken)
    {
        var manager = OpenSCManager(null, null, ScManagerEnumerateService);
        if (manager == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            uint bytesNeeded;
            uint serviceCount;
            uint resumeHandle = 0;
            EnumServicesStatusEx(
                manager,
                ScEnumProcessInfoLevel,
                ServiceWin32,
                ServiceStateAll,
                IntPtr.Zero,
                0,
                out bytesNeeded,
                out serviceCount,
                ref resumeHandle,
                null);

            var error = Marshal.GetLastWin32Error();
            if (error != ErrorMoreData && bytesNeeded == 0)
            {
                if (error == 0)
                {
                    return Array.Empty<ServiceStatusDto>();
                }

                throw new Win32Exception(error);
            }

            if (bytesNeeded == 0)
            {
                return Array.Empty<ServiceStatusDto>();
            }

            var buffer = Marshal.AllocHGlobal(checked((int)bytesNeeded));
            try
            {
                if (!EnumServicesStatusEx(
                        manager,
                        ScEnumProcessInfoLevel,
                        ServiceWin32,
                        ServiceStateAll,
                        buffer,
                        bytesNeeded,
                        out _,
                        out serviceCount,
                        ref resumeHandle,
                        null))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var itemSize = Marshal.SizeOf<EnumServiceStatusProcess>();
                var result = new List<ServiceStatusDto>((int)serviceCount);
                for (var index = 0; index < serviceCount; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = Marshal.PtrToStructure<EnumServiceStatusProcess>(buffer + (index * itemSize));
                    var serviceName = ReadString(item.ServiceName);
                    if (string.IsNullOrWhiteSpace(serviceName))
                    {
                        continue;
                    }

                    result.Add(new ServiceStatusDto
                    {
                        Id = index + 1,
                        ServiceName = serviceName,
                        DisplayName = ReadString(item.DisplayName),
                        Status = MapStatus(item.StatusProcess.CurrentState),
                        LastHeartbeatAt = null
                    });
                }

                return result.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseServiceHandle(manager);
        }
    }

    private static string MapStatus(uint state)
        => state switch
        {
            1 => "Stopped",
            2 => "Start pending",
            3 => "Stop pending",
            4 => "Running",
            5 => "Continue pending",
            6 => "Pause pending",
            7 => "Paused",
            _ => "Unknown"
        };

    private static string ReadString(IntPtr value)
        => value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(value) ?? string.Empty;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenSCManager(
        string? machineName,
        string? databaseName,
        uint desiredAccess);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenService(
        IntPtr serviceManager,
        string serviceName,
        uint desiredAccess);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumServicesStatusEx(
        IntPtr serviceManager,
        int infoLevel,
        uint serviceType,
        uint serviceState,
        IntPtr services,
        uint bufferSize,
        out uint bytesNeeded,
        out uint servicesReturned,
        ref uint resumeHandle,
        string? groupName);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool QueryServiceStatusEx(
        IntPtr service,
        int infoLevel,
        IntPtr status,
        uint bufferSize,
        out uint bytesNeeded);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool ControlService(
        IntPtr service,
        uint control,
        out ServiceStatusProcess status);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool StartService(
        IntPtr service,
        uint argumentsCount,
        IntPtr arguments);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(IntPtr serviceHandle);

    [StructLayout(LayoutKind.Sequential)]
    private struct EnumServiceStatusProcess
    {
        public IntPtr ServiceName;
        public IntPtr DisplayName;
        public ServiceStatusProcess StatusProcess;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }
}
