using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ApprovalWorkflowService : IApprovalWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly IRuntimeTelemetryService? _telemetryService;

    public ApprovalWorkflowService(
        AppDbContext db,
        ICurrentCompanyContext companyContext,
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IAuditService? auditService = null,
        IAuditCorrelationService? correlationService = null,
        IRuntimeTelemetryService? telemetryService = null)
    {
        _db = db;
        _companyContext = companyContext;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _auditService = auditService;
        _correlationService = correlationService;
        _telemetryService = telemetryService;
    }

    public async Task<ApprovalWorkflowDto> SubmitAsync(ApprovalSubmitRequestDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsurePermissionAsync("approval.submit", cancellationToken);
        if (request.Steps.Count == 0)
        {
            throw new InvalidOperationException("Approval workflow must have at least one step.");
        }

        var totalLevels = request.Steps.Max(x => x.Level);
        var workflow = new ApprovalWorkflow
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            Module = request.Module,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Title = request.Title,
            Status = "Pending",
            CurrentLevel = request.Steps.Min(x => x.Level),
            TotalLevels = totalLevels,
            SubmittedAtUtc = DateTime.UtcNow,
            LastActivityAtUtc = DateTime.UtcNow,
            CorrelationId = request.CorrelationId ?? _correlationService?.CurrentCorrelationId,
            MetadataJson = request.MetadataJson
        };

        foreach (var stepDefinition in request.Steps.OrderBy(x => x.Level))
        {
            var step = new ApprovalStep
            {
                CompanyId = workflow.CompanyId,
                BranchId = workflow.BranchId,
                Workflow = workflow,
                Level = stepDefinition.Level,
                StepName = stepDefinition.StepName,
                Status = stepDefinition.Level == workflow.CurrentLevel ? "Pending" : "Waiting",
                StartedAtUtc = stepDefinition.Level == workflow.CurrentLevel ? DateTime.UtcNow : null,
                DueAtUtc = stepDefinition.DueAtUtc
            };

            foreach (var userId in stepDefinition.AssignedUserIds.Distinct())
            {
                step.Assignments.Add(new ApprovalAssignment
                {
                    CompanyId = workflow.CompanyId,
                    BranchId = workflow.BranchId,
                    Workflow = workflow,
                    AssignedUserId = userId,
                    Status = step.Status,
                    IsPrimary = true,
                    AssignedAtUtc = DateTime.UtcNow
                });
            }

            workflow.Steps.Add(step);
        }

        workflow.History.Add(new ApprovalHistory
        {
            CompanyId = workflow.CompanyId,
            BranchId = workflow.BranchId,
            Action = "Submitted",
            ActorUserId = _currentUserService.CurrentUser?.Id,
            OccurredAtUtc = DateTime.UtcNow,
            Comment = request.Title
        });

        _db.ApprovalWorkflows.Add(workflow);
        await PersistSnapshotAsync(workflow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await TrackAsync("approval.submit", workflow, cancellationToken);
        return ToDto(workflow);
    }

    public async Task<ApprovalWorkflowDto> ApproveAsync(Guid workflowId, string? comment = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsurePermissionAsync("approval.approve", cancellationToken);
        var workflow = await LoadWorkflowForActionAsync(workflowId, cancellationToken);
        var currentUserId = _currentUserService.CurrentUser?.Id ?? Guid.Empty;
        var currentStep = workflow.Steps.FirstOrDefault(x => x.Level == workflow.CurrentLevel);
        if (currentStep is null)
        {
            throw new InvalidOperationException("Current approval step is missing.");
        }

        var assignment = currentStep.Assignments.FirstOrDefault(x => x.AssignedUserId == currentUserId && x.Status is "Pending" or "Escalated");
        if (assignment is null)
        {
            throw new UnauthorizedAccessException("Current user is not assigned for this approval step.");
        }

        assignment.Status = "Approved";
        assignment.ActedAtUtc = DateTime.UtcNow;
        assignment.ActionComment = comment;
        currentStep.Status = "Approved";
        currentStep.CompletedAtUtc = DateTime.UtcNow;
        workflow.LastActivityAtUtc = DateTime.UtcNow;

        var nextStep = workflow.Steps
            .Where(x => x.Level > workflow.CurrentLevel)
            .OrderBy(x => x.Level)
            .FirstOrDefault();
        if (nextStep is null)
        {
            workflow.Status = "Approved";
            workflow.IsCompleted = true;
            workflow.CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            workflow.CurrentLevel = nextStep.Level;
            nextStep.Status = "Pending";
            nextStep.StartedAtUtc = DateTime.UtcNow;
            foreach (var nextAssignment in nextStep.Assignments)
            {
                nextAssignment.Status = "Pending";
            }
        }

        workflow.History.Add(new ApprovalHistory
        {
            CompanyId = workflow.CompanyId,
            BranchId = workflow.BranchId,
            WorkflowId = workflow.Id,
            StepId = currentStep.Id,
            AssignmentId = assignment.Id,
            Action = "Approved",
            ActorUserId = currentUserId,
            OccurredAtUtc = DateTime.UtcNow,
            Comment = comment
        });

        await PersistSnapshotAsync(workflow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await TrackAsync("approval.approve", workflow, cancellationToken);
        return ToDto(workflow);
    }

    public async Task<ApprovalWorkflowDto> RejectAsync(Guid workflowId, string? comment = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsurePermissionAsync("approval.reject", cancellationToken);
        var workflow = await LoadWorkflowForActionAsync(workflowId, cancellationToken);
        var currentUserId = _currentUserService.CurrentUser?.Id ?? Guid.Empty;
        var currentStep = workflow.Steps.FirstOrDefault(x => x.Level == workflow.CurrentLevel);
        if (currentStep is null)
        {
            throw new InvalidOperationException("Current approval step is missing.");
        }

        var assignment = currentStep.Assignments.FirstOrDefault(x => x.AssignedUserId == currentUserId && x.Status is "Pending" or "Escalated");
        if (assignment is null)
        {
            throw new UnauthorizedAccessException("Current user is not assigned for this approval step.");
        }

        assignment.Status = "Rejected";
        assignment.ActedAtUtc = DateTime.UtcNow;
        assignment.ActionComment = comment;
        currentStep.Status = "Rejected";
        currentStep.CompletedAtUtc = DateTime.UtcNow;
        workflow.Status = "Rejected";
        workflow.IsRejected = true;
        workflow.CompletedAtUtc = DateTime.UtcNow;
        workflow.LastActivityAtUtc = DateTime.UtcNow;

        workflow.History.Add(new ApprovalHistory
        {
            CompanyId = workflow.CompanyId,
            BranchId = workflow.BranchId,
            WorkflowId = workflow.Id,
            StepId = currentStep.Id,
            AssignmentId = assignment.Id,
            Action = "Rejected",
            ActorUserId = currentUserId,
            OccurredAtUtc = DateTime.UtcNow,
            Comment = comment
        });

        await PersistSnapshotAsync(workflow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await TrackAsync("approval.reject", workflow, cancellationToken);
        return ToDto(workflow);
    }

    public async Task DelegateAsync(Guid assignmentId, Guid toUserId, string? reason = null, DateTime? expiresAtUtc = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsurePermissionAsync("approval.delegate", cancellationToken);
        var assignment = await _db.ApprovalAssignments
            .FirstOrDefaultAsync(x => x.Id == assignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Approval assignment not found.");
        if (assignment.Status is not ("Pending" or "Escalated"))
        {
            throw new InvalidOperationException("Only pending assignment can be delegated.");
        }

        var delegation = new ApprovalDelegation
        {
            CompanyId = assignment.CompanyId,
            BranchId = assignment.BranchId,
            AssignmentId = assignment.Id,
            FromUserId = assignment.AssignedUserId,
            ToUserId = toUserId,
            DelegatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            Reason = reason,
            Status = "Active"
        };
        _db.ApprovalDelegations.Add(delegation);

        assignment.AssignedUserId = toUserId;
        assignment.AssignedAtUtc = DateTime.UtcNow;

        _db.ApprovalHistory.Add(new ApprovalHistory
        {
            CompanyId = assignment.CompanyId,
            BranchId = assignment.BranchId,
            WorkflowId = assignment.WorkflowId,
            StepId = assignment.StepId,
            AssignmentId = assignment.Id,
            Action = "Delegated",
            ActorUserId = _currentUserService.CurrentUser?.Id,
            OccurredAtUtc = DateTime.UtcNow,
            Comment = reason
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> EscalateTimeoutAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsurePermissionAsync("approval.escalate", cancellationToken);
        var dueSteps = await _db.ApprovalSteps
            .Include(x => x.Assignments)
            .Where(x => x.Status == "Pending" && x.DueAtUtc != null && x.DueAtUtc < utcNow)
            .ToListAsync(cancellationToken);
        var escalated = 0;
        foreach (var step in dueSteps)
        {
            var assignment = step.Assignments.FirstOrDefault(x => x.Status == "Pending");
            if (assignment is null)
            {
                continue;
            }

            assignment.Status = "Escalated";
            _db.ApprovalEscalations.Add(new ApprovalEscalation
            {
                CompanyId = step.CompanyId,
                BranchId = step.BranchId,
                WorkflowId = step.WorkflowId,
                StepId = step.Id,
                AssignmentId = assignment.Id,
                EscalatedToUserId = assignment.AssignedUserId,
                Reason = "Timeout",
                EscalatedAtUtc = utcNow
            });
            _db.ApprovalHistory.Add(new ApprovalHistory
            {
                CompanyId = step.CompanyId,
                BranchId = step.BranchId,
                WorkflowId = step.WorkflowId,
                StepId = step.Id,
                AssignmentId = assignment.Id,
                Action = "Escalated",
                OccurredAtUtc = utcNow,
                Comment = "Timeout"
            });
            escalated++;
        }

        if (escalated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return escalated;
    }

    public async Task<IReadOnlyList<ApprovalHistoryDto>> ReplayTrailAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _db.ApprovalHistory
            .AsNoTracking()
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new ApprovalHistoryDto
            {
                Id = x.Id,
                WorkflowId = x.WorkflowId,
                Action = x.Action,
                ActorUserId = x.ActorUserId,
                OccurredAtUtc = x.OccurredAtUtc,
                Comment = x.Comment
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ApprovalWorkflowDto> RecoverStateAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflow = await _db.ApprovalWorkflows
            .Include(x => x.Steps)
            .ThenInclude(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == workflowId, cancellationToken)
            ?? throw new KeyNotFoundException("Approval workflow not found.");

        if (!workflow.IsCompleted && !workflow.IsRejected)
        {
            var currentStep = workflow.Steps.FirstOrDefault(x => x.Level == workflow.CurrentLevel);
            if (currentStep is not null && currentStep.Status == "Waiting")
            {
                currentStep.Status = "Pending";
                foreach (var assignment in currentStep.Assignments)
                {
                    assignment.Status = "Pending";
                }
                workflow.Status = "Pending";
                workflow.LastActivityAtUtc = DateTime.UtcNow;
            }
        }

        await PersistSnapshotAsync(workflow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(workflow);
    }

    public async Task<IReadOnlyList<ApprovalWorkflowDto>> GetWorkflowsAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 500);
        return await _db.ApprovalWorkflows
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Take(normalizedTake)
            .Select(ToDtoProjection())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalStepDto>> GetStepsAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _db.ApprovalSteps
            .AsNoTracking()
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.Level)
            .Select(x => new ApprovalStepDto
            {
                Id = x.Id,
                WorkflowId = x.WorkflowId,
                Level = x.Level,
                StepName = x.StepName,
                Status = x.Status,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                DueAtUtc = x.DueAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalSnapshotDto>> GetSnapshotsAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _db.ApprovalSnapshots
            .AsNoTracking()
            .Where(x => x.WorkflowId == workflowId)
            .OrderByDescending(x => x.CapturedAtUtc)
            .Take(100)
            .Select(x => new ApprovalSnapshotDto
            {
                Id = x.Id,
                WorkflowId = x.WorkflowId,
                CapturedAtUtc = x.CapturedAtUtc,
                Status = x.Status,
                CurrentLevel = x.CurrentLevel,
                SnapshotHash = x.SnapshotHash
            })
            .ToListAsync(cancellationToken);
    }

    private static ApprovalWorkflowDto ToDto(ApprovalWorkflow workflow)
        => new()
        {
            Id = workflow.Id,
            Module = workflow.Module,
            EntityType = workflow.EntityType,
            EntityId = workflow.EntityId,
            Title = workflow.Title,
            Status = workflow.Status,
            CurrentLevel = workflow.CurrentLevel,
            TotalLevels = workflow.TotalLevels,
            SubmittedAtUtc = workflow.SubmittedAtUtc,
            CompletedAtUtc = workflow.CompletedAtUtc,
            CorrelationId = workflow.CorrelationId
        };

    private static System.Linq.Expressions.Expression<Func<ApprovalWorkflow, ApprovalWorkflowDto>> ToDtoProjection()
        => workflow => new ApprovalWorkflowDto
        {
            Id = workflow.Id,
            Module = workflow.Module,
            EntityType = workflow.EntityType,
            EntityId = workflow.EntityId,
            Title = workflow.Title,
            Status = workflow.Status,
            CurrentLevel = workflow.CurrentLevel,
            TotalLevels = workflow.TotalLevels,
            SubmittedAtUtc = workflow.SubmittedAtUtc,
            CompletedAtUtc = workflow.CompletedAtUtc,
            CorrelationId = workflow.CorrelationId
        };

    private async Task<ApprovalWorkflow> LoadWorkflowForActionAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await _db.ApprovalWorkflows
            .Include(x => x.Steps)
            .ThenInclude(x => x.Assignments)
            .Include(x => x.History)
            .FirstOrDefaultAsync(x => x.Id == workflowId, cancellationToken)
            ?? throw new KeyNotFoundException("Approval workflow not found.");
        if (workflow.IsCompleted || workflow.IsRejected)
        {
            throw new InvalidOperationException("Approval workflow already finished.");
        }

        return workflow;
    }

    private async Task EnsurePermissionAsync(string permissionCode, CancellationToken cancellationToken)
    {
        var hasPermission = await _permissionService.HasPermissionAsync(permissionCode, _companyContext.CompanyId, _companyContext.BranchId, cancellationToken);
        if (!hasPermission)
        {
            throw new UnauthorizedAccessException($"Permission denied: {permissionCode}");
        }
    }

    private async Task PersistSnapshotAsync(ApprovalWorkflow workflow, CancellationToken cancellationToken)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            workflow.Id,
            workflow.Status,
            workflow.CurrentLevel,
            workflow.TotalLevels,
            Steps = workflow.Steps.OrderBy(x => x.Level).Select(step => new
            {
                step.Id,
                step.Level,
                step.Status,
                step.StartedAtUtc,
                step.CompletedAtUtc,
                Assignments = step.Assignments.Select(a => new { a.Id, a.AssignedUserId, a.Status, a.ActedAtUtc })
            })
        }, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
        _db.ApprovalSnapshots.Add(new ApprovalSnapshot
        {
            CompanyId = workflow.CompanyId,
            BranchId = workflow.BranchId,
            WorkflowId = workflow.Id,
            CapturedAtUtc = DateTime.UtcNow,
            Status = workflow.Status,
            CurrentLevel = workflow.CurrentLevel,
            SnapshotJson = snapshot,
            SnapshotHash = hash
        });

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task TrackAsync(string action, ApprovalWorkflow workflow, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(
                "ApprovalWorkflow",
                workflow.Id.ToString("N"),
                action,
                userName: _currentUserService.CurrentUser?.UserName,
                correlationId: workflow.CorrelationId,
                cancellationToken: cancellationToken);
        }

        if (_telemetryService is not null)
        {
            await _telemetryService.TrackWorkflowMetricsAsync(
                "approval",
                workflow.Id.ToString("N"),
                durationMs: 0,
                status: workflow.Status,
                message: action,
                correlationId: workflow.CorrelationId,
                cancellationToken: cancellationToken);
        }
    }
}
