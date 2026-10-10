using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Reads installed printers from the Windows Print Spooler.
/// Printer ACL projection is intentionally not fabricated because the current
/// application contract does not expose the Windows security descriptor model.
/// </summary>
public sealed class WindowsPrinterSettingsService : IPrinterSettingsService
{
    private const uint PrinterEnumLocal = 0x00000002;
    private const uint PrinterEnumConnections = 0x00000004;
    private const uint PrinterStatusError = 0x00000002;
    private const uint PrinterStatusPaperOut = 0x00000010;
    private const uint PrinterStatusBusy = 0x00000200;
    private const uint PrinterStatusPrinting = 0x00000400;
    private const uint PrinterStatusOffline = 0x00000080;
    private const uint PrinterStatusNoToner = 0x00040000;
    private const uint PrinterStatusUserIntervention = 0x00100000;
    private const uint PrinterStatusOutputBinFull = 0x00000800;
    private const uint PrinterAccessUse = 0x00000008;
    private const uint PrinterAccessAdminister = 0x00000004;
    private const uint StandardDelete = 0x00010000;
    private const uint StandardReadControl = 0x00020000;
    private const uint StandardWriteDac = 0x00040000;
    private const uint StandardWriteOwner = 0x00080000;
    private const int ErrorInsufficientBuffer = 122;

    public Task<IReadOnlyList<PrinterDeviceDto>> GetAvailablePrintersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<PrinterDeviceDto>>(Array.Empty<PrinterDeviceDto>());
        }

        try
        {
            return Task.FromResult<IReadOnlyList<PrinterDeviceDto>>(EnumeratePrinters(cancellationToken));
        }
        catch (DllNotFoundException)
        {
            return Task.FromResult<IReadOnlyList<PrinterDeviceDto>>(Array.Empty<PrinterDeviceDto>());
        }
        catch (EntryPointNotFoundException)
        {
            return Task.FromResult<IReadOnlyList<PrinterDeviceDto>>(Array.Empty<PrinterDeviceDto>());
        }
        catch (Win32Exception)
        {
            return Task.FromResult<IReadOnlyList<PrinterDeviceDto>>(Array.Empty<PrinterDeviceDto>());
        }
    }

    public Task<IReadOnlyList<PrinterPermissionDto>> GetPrinterPermissionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<PrinterPermissionDto>>(Array.Empty<PrinterPermissionDto>());
        }

        try
        {
            return Task.FromResult<IReadOnlyList<PrinterPermissionDto>>(EnumeratePrinterPermissions(cancellationToken));
        }
        catch (DllNotFoundException)
        {
            return Task.FromResult<IReadOnlyList<PrinterPermissionDto>>(Array.Empty<PrinterPermissionDto>());
        }
        catch (EntryPointNotFoundException)
        {
            return Task.FromResult<IReadOnlyList<PrinterPermissionDto>>(Array.Empty<PrinterPermissionDto>());
        }
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<PrinterPermissionDto> EnumeratePrinterPermissions(CancellationToken cancellationToken)
    {
        var printers = EnumeratePrinters(cancellationToken);
        var result = new List<PrinterPermissionDto>();

        foreach (var printer in printers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                result.AddRange(ReadPrinterPermissions(printer.Name, cancellationToken));
            }
            catch (Win32Exception ex)
            {
                // Preserve the real printer and the OS error instead of
                // presenting an empty ACL as if it were a permission result.
                result.Add(new PrinterPermissionDto
                {
                    PrinterName = printer.Name,
                    Profile = "Unavailable",
                    ReadStatus = $"Win32:{ex.NativeErrorCode}",
                    Principal = string.Empty,
                    Rights = string.Empty,
                    AccessType = string.Empty
                });
            }
        }

        return result;
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<PrinterPermissionDto> ReadPrinterPermissions(string printerName, CancellationToken cancellationToken)
    {
        var defaults = new PrinterDefaults { DesiredAccess = StandardReadControl };
        if (!OpenPrinter(printerName, out var handle, ref defaults))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            const uint level = 3;
            GetPrinter(handle, level, IntPtr.Zero, 0, out var needed);
            var error = Marshal.GetLastWin32Error();
            if (needed == 0 && error != ErrorInsufficientBuffer)
            {
                throw new Win32Exception(error);
            }

            var buffer = Marshal.AllocHGlobal(checked((int)Math.Max(needed, 1)));
            try
            {
                if (!GetPrinter(handle, level, buffer, needed, out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var info = Marshal.PtrToStructure<PrinterInfo3>(buffer);
                if (info.SecurityDescriptor == IntPtr.Zero)
                {
                    return [new PrinterPermissionDto
                    {
                        PrinterName = printerName,
                        Profile = "NoDacl",
                        ReadStatus = "Read"
                    }];
                }

                var descriptorLength = GetSecurityDescriptorLength(info.SecurityDescriptor);
                if (descriptorLength == 0)
                {
                    return [new PrinterPermissionDto
                    {
                        PrinterName = printerName,
                        Profile = "InvalidDescriptor",
                        ReadStatus = "Read"
                    }];
                }

                var descriptorBytes = new byte[descriptorLength];
                Marshal.Copy(info.SecurityDescriptor, descriptorBytes, 0, descriptorBytes.Length);
                var descriptor = new RawSecurityDescriptor(descriptorBytes, 0);
                var acl = descriptor.DiscretionaryAcl;
                if (acl is null || acl.Count == 0)
                {
                    return [new PrinterPermissionDto
                    {
                        PrinterName = printerName,
                        Profile = "EmptyDacl",
                        ReadStatus = "Read"
                    }];
                }

                var result = new List<PrinterPermissionDto>(acl.Count);
                foreach (GenericAce ace in acl)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!TryReadAce(ace, out var principal, out var accessMask, out var accessType, out var inherited))
                    {
                        continue;
                    }

                    var rights = MapRights(accessMask);
                    result.Add(new PrinterPermissionDto
                    {
                        PrinterName = printerName,
                        Profile = accessType,
                        Principal = principal,
                        Rights = rights,
                        AllowedDocuments = rights,
                        AccessType = accessType,
                        IsInherited = inherited,
                        ReadStatus = "Read"
                    });
                }

                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            ClosePrinter(handle);
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryReadAce(GenericAce ace, out string principal, out int accessMask, out string accessType, out bool inherited)
    {
        SecurityIdentifier? sid = null;
        accessMask = 0;
        switch (ace)
        {
            case CommonAce commonAce:
                sid = commonAce.SecurityIdentifier;
                accessMask = commonAce.AccessMask;
                break;
            case ObjectAce objectAce:
                sid = objectAce.SecurityIdentifier;
                accessMask = objectAce.AccessMask;
                break;
            default:
                principal = string.Empty;
                accessType = string.Empty;
                inherited = ace.IsInherited;
                return false;
        }

        principal = TranslateSid(sid);
        accessType = ace.AceType is AceType.AccessDenied or AceType.AccessDeniedObject ? "Deny" : "Allow";
        inherited = ace.IsInherited;
        return true;
    }

    [SupportedOSPlatform("windows")]
    private static string TranslateSid(SecurityIdentifier sid)
    {
        try
        {
            return sid.Translate(typeof(NTAccount)).Value;
        }
        catch (IdentityNotMappedException)
        {
            return sid.Value;
        }
    }

    private static string MapRights(int accessMask)
    {
        var rights = new List<string>();
        var mask = unchecked((uint)accessMask);
        if ((mask & PrinterAccessUse) != 0) rights.Add("Print");
        if ((mask & PrinterAccessAdminister) != 0) rights.Add("ManagePrinter");
        if ((mask & StandardDelete) != 0) rights.Add("Delete");
        if ((mask & StandardReadControl) != 0) rights.Add("ReadControl");
        if ((mask & StandardWriteDac) != 0) rights.Add("ChangePermissions");
        if ((mask & StandardWriteOwner) != 0) rights.Add("TakeOwnership");
        return rights.Count == 0 ? $"0x{mask:X8}" : string.Join(", ", rights);
    }

    private static IReadOnlyList<PrinterDeviceDto> EnumeratePrinters(CancellationToken cancellationToken)
    {
        const uint level = 2;
        var flags = PrinterEnumLocal | PrinterEnumConnections;

        EnumPrinters(flags, null, level, IntPtr.Zero, 0, out var needed, out _);
        if (needed == 0)
        {
            return Array.Empty<PrinterDeviceDto>();
        }

        var buffer = Marshal.AllocHGlobal(checked((int)needed));
        try
        {
            if (!EnumPrinters(flags, null, level, buffer, needed, out _, out var returned))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var itemSize = Marshal.SizeOf<PrinterInfo2>();
            var defaultPrinter = GetDefaultPrinterName();
            var result = new List<PrinterDeviceDto>((int)returned);
            for (var index = 0; index < returned; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = Marshal.PtrToStructure<PrinterInfo2>(buffer + (index * itemSize));
                var name = ReadString(item.PrinterName);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var server = ReadString(item.ServerName);
                var share = ReadString(item.ShareName);
                var sharePath = !string.IsNullOrWhiteSpace(share) && !string.IsNullOrWhiteSpace(server)
                    ? $"\\\\{server.TrimStart('\\')}\\{share}"
                    : name;

                result.Add(new PrinterDeviceDto
                {
                    Name = name,
                    SharePath = sharePath,
                    Status = MapStatus(item.Status, string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase))
                });
            }

            return result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string MapStatus(uint status, bool isDefault)
    {
        if ((status & PrinterStatusOffline) != 0)
        {
            return "Offline";
        }

        if ((status & (PrinterStatusError | PrinterStatusPaperOut | PrinterStatusNoToner | PrinterStatusUserIntervention | PrinterStatusOutputBinFull)) != 0)
        {
            return "Error";
        }

        if ((status & (PrinterStatusBusy | PrinterStatusPrinting)) != 0)
        {
            return "Busy";
        }

        return isDefault ? "Online (mặc định)" : "Online";
    }

    private static string GetDefaultPrinterName()
    {
        var capacity = 256u;
        while (true)
        {
            var buffer = new StringBuilder(checked((int)capacity));
            if (GetDefaultPrinter(buffer, ref capacity))
            {
                return buffer.ToString();
            }

            if (Marshal.GetLastWin32Error() != 122 || capacity <= buffer.Capacity)
            {
                return string.Empty;
            }
        }
    }

    private static string ReadString(IntPtr value)
        => value == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(value) ?? string.Empty;

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumPrinters(
        uint flags,
        string? name,
        uint level,
        IntPtr printerEnum,
        uint bufferSize,
        out uint bytesNeeded,
        out uint printersReturned);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetDefaultPrinter(StringBuilder buffer, ref uint bufferSize);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, ref PrinterDefaults defaults);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool GetPrinter(IntPtr printerHandle, uint level, IntPtr buffer, uint bufferSize, out uint bytesNeeded);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint GetSecurityDescriptorLength(IntPtr securityDescriptor);

    [StructLayout(LayoutKind.Sequential)]
    private struct PrinterDefaults
    {
        public IntPtr Datatype;
        public IntPtr DevMode;
        public uint DesiredAccess;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PrinterInfo2
    {
        public IntPtr ServerName;
        public IntPtr PrinterName;
        public IntPtr ShareName;
        public IntPtr PortName;
        public IntPtr DriverName;
        public IntPtr Comment;
        public IntPtr Location;
        public IntPtr DevMode;
        public IntPtr SepFile;
        public IntPtr PrintProcessor;
        public IntPtr Datatype;
        public IntPtr Parameters;
        public IntPtr SecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint JobsCount;
        public uint AveragePpm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PrinterInfo3
    {
        public IntPtr SecurityDescriptor;
    }
}
