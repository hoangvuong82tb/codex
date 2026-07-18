namespace ASMILE.BO.Application.FrontOffice.ProcessManager;

public enum ProcessFaultPoint { AfterSignalReserved, BeforeProcessLoad, AfterProcessLoaded, AfterTransitionApplied, BeforeHistoryPersist, BeforeOutboxPersist, BeforeSaveChanges, AfterSaveChangesBeforeCommit, AfterCommitBeforeAck, AfterOutboxPersistBeforePublish, AfterTimeoutClaim, BeforeSignalReserve, BeforeTimeoutCompletion, AfterTimeoutCompletion, BeforeCompensationPersist, AfterCompensationPersist, BeforeManualActionComplete }
public interface IProcessFaultInjector { Task CheckpointAsync(ProcessFaultPoint point, CancellationToken cancellationToken = default); }
public sealed class NoOpProcessFaultInjector : IProcessFaultInjector { public Task CheckpointAsync(ProcessFaultPoint point, CancellationToken cancellationToken = default) => Task.CompletedTask; }
