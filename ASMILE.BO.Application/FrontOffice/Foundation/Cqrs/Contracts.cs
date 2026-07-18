using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;

public interface ICommand { }
public interface ICommand<TResponse> : ICommand { }
public interface IQuery<TResponse> { }
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse> { Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken); }
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse> { Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken); }
public delegate Task<Result<TResponse>> CommandHandlerDelegate<TResponse>(CancellationToken cancellationToken);
public interface ICommandBehavior<TCommand, TResponse> where TCommand : ICommand<TResponse> { Task<Result<TResponse>> HandleAsync(TCommand command, CommandHandlerDelegate<TResponse> next, CancellationToken cancellationToken); }
public sealed record CommandEnvelope(CommandId CommandId, string CommandName, string? IdempotencyKey, DateTime ReceivedAtUtc);
public sealed record PageRequest(int Page, int PageSize) { public int Skip => Math.Max(0, Page - 1) * PageSize; }
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public static class FrontOfficePipelineOrder { public static readonly IReadOnlyList<string> Steps = ["CorrelationContext", "Validation", "Authorization", "Idempotency", "Transaction", "Handler", "AuditAndOutbox", "ResultMapping", "Logging"]; }
