using ASMILE.BO.Infrastructure.SettingsEngine;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace ASMILE.BO.Infrastructure.Persistence;

/// <summary>Applies the live Settings Engine timeout to EF Core commands.</summary>
public sealed class DatabaseCommandTimeoutInterceptor : DbCommandInterceptor
{
    private readonly RuntimeDatabaseCommandTimeout _timeout;

    public DatabaseCommandTimeoutInterceptor(RuntimeDatabaseCommandTimeout timeout)
    {
        _timeout = timeout;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplyTimeout(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTimeout(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ApplyTimeout(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTimeout(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTimeout(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTimeout(command);
        return ValueTask.FromResult(result);
    }

    private void ApplyTimeout(DbCommand command)
        => command.CommandTimeout = _timeout.Seconds;
}
