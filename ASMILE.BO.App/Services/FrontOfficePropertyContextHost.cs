using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.App.Services;

public sealed record FrontOfficePropertyOption(Guid PropertyId, string Code, string Name, string DisplayName, bool IsActive);

public sealed class FrontOfficePropertyContextChangedEventArgs(
    FrontOfficePropertyOption? previous,
    FrontOfficePropertyOption? current,
    int version,
    string? errorCode = null) : EventArgs
{
    public FrontOfficePropertyOption? Previous { get; } = previous;
    public FrontOfficePropertyOption? Current { get; } = current;
    public int Version { get; } = version;
    public string? ErrorCode { get; } = errorCode;
}

public interface IFrontOfficePropertyContextHost : IDisposable
{
    FrontOfficePropertyOption? CurrentProperty { get; }
    IReadOnlyList<FrontOfficePropertyOption> AvailableProperties { get; }
    bool HasCurrentProperty { get; }
    int Version { get; }
    string? ErrorCode { get; }
    event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged;
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default);
}

// Branch is the existing trusted operational unit. Front Office maps one active branch to one Property.
public sealed class FrontOfficePropertyContextHost(
    ICurrentCompanyContext companyContext,
    ICurrentUserService currentUser,
    IFrontOfficeExecutionContextAccessor executionContext,
    IClock clock) : IFrontOfficePropertyContextHost
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<FrontOfficePropertyOption> _availableProperties = [];
    private IDisposable? _executionScope;
    private bool _disposed;

    public FrontOfficePropertyOption? CurrentProperty { get; private set; }
    public IReadOnlyList<FrontOfficePropertyOption> AvailableProperties => _availableProperties;
    public bool HasCurrentProperty => CurrentProperty is not null && executionContext.Current is not null;
    public int Version { get; private set; }
    public string? ErrorCode { get; private set; }
    public event EventHandler<FrontOfficePropertyContextChangedEventArgs>? PropertyChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            await companyContext.EnsureInitializedAsync(cancellationToken);
            _availableProperties = (await companyContext.GetBranchesAsync(cancellationToken))
                .Where(x => x.Id != Guid.Empty)
                .Select(x => new FrontOfficePropertyOption(x.Id, x.Code, x.Name, $"{x.Code} - {x.Name}", true))
                .ToArray();

            var selected = companyContext.BranchId is { } branchId
                ? _availableProperties.FirstOrDefault(x => x.PropertyId == branchId)
                : null;
            if (selected is not null)
            {
                Activate(selected);
                return;
            }

            if (_availableProperties.Count == 1)
            {
                selected = _availableProperties[0];
                await companyContext.SetBranchAsync(selected.PropertyId, cancellationToken);
                Activate(selected);
                return;
            }

            ClearCore(_availableProperties.Count == 0 ? "FRONT_OFFICE_PROPERTY_NOT_FOUND" : "FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            ClearCore("FRONT_OFFICE_PROPERTY_LOAD_FAILED");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetCurrentPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            if (_availableProperties.Count == 0)
            {
                await companyContext.EnsureInitializedAsync(cancellationToken);
                _availableProperties = (await companyContext.GetBranchesAsync(cancellationToken))
                    .Where(x => x.Id != Guid.Empty)
                    .Select(x => new FrontOfficePropertyOption(x.Id, x.Code, x.Name, $"{x.Code} - {x.Name}", true))
                    .ToArray();
            }

            var selected = _availableProperties.FirstOrDefault(x => x.PropertyId == propertyId);
            if (selected is null)
            {
                ClearCore("FRONT_OFFICE_PROPERTY_NOT_AUTHORIZED");
                return;
            }

            await companyContext.SetBranchAsync(selected.PropertyId, cancellationToken);
            Activate(selected);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            ClearCore("FRONT_OFFICE_PROPERTY_LOAD_FAILED");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearCurrentPropertyAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_disposed) return;
            await companyContext.SetBranchAsync(null, cancellationToken);
            ClearCore("FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");
        }
        finally
        {
            _gate.Release();
        }
    }

    private void Activate(FrontOfficePropertyOption selected)
    {
        if (CurrentProperty?.PropertyId == selected.PropertyId && executionContext.Current is not null)
        {
            ErrorCode = null;
            return;
        }

        if (!currentUser.IsAuthenticated || currentUser.CurrentUser is null || currentUser.CurrentSessionId is null)
        {
            ClearCore("FRONT_OFFICE_PROPERTY_CONTEXT_REQUIRED");
            return;
        }

        var previous = CurrentProperty;
        _executionScope?.Dispose();
        _executionScope = null;
        var propertyId = new PropertyId(selected.PropertyId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow, TimeZoneInfo.Local);
        var user = currentUser.CurrentUser;
        var context = new FrontOfficeExecutionContext(
            new ActorContext(new ActorId(user.Id), user.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase), currentUser.CurrentSessionId.Value.ToString("N"), Environment.MachineName, false),
            new PropertyContext(propertyId, TimeZoneInfo.Local.Id, null),
            new BusinessDateContext(propertyId, DateOnly.FromDateTime(localNow), "host-property-context"),
            new CorrelationContext(new CorrelationId(Guid.NewGuid()), null, null, null));
        _executionScope = executionContext.BeginScope(context);
        CurrentProperty = selected;
        ErrorCode = null;
        Version++;
        PropertyChanged?.Invoke(this, new FrontOfficePropertyContextChangedEventArgs(previous, selected, Version));
    }

    private void ClearCore(string errorCode)
    {
        var previous = CurrentProperty;
        _executionScope?.Dispose();
        _executionScope = null;
        CurrentProperty = null;
        ErrorCode = errorCode;
        Version++;
        if (previous is not null || executionContext.Current is not null)
        {
            PropertyChanged?.Invoke(this, new FrontOfficePropertyContextChangedEventArgs(previous, null, Version, errorCode));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _executionScope?.Dispose();
        _executionScope = null;
        CurrentProperty = null;
        _gate.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FrontOfficePropertyContextHost));
    }
}
