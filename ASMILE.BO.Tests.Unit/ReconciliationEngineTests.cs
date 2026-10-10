using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReconciliationEngineTests
{
    [Fact]
    public async Task Auto_match_uses_real_statement_line_and_receivable_transaction_ids()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var companyContext = new TestCompanyContext(companyId, branchId);
        await using var db = TestDbContextFactory.CreateContext(companyContext, seedAccountingCore: false);

        var statementId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var transactionDate = new DateTime(2026, 9, 8);
        db.BankStatements.Add(new BankStatement
        {
            Id = statementId,
            CompanyId = companyId,
            BranchId = branchId,
            BankAccountId = Guid.NewGuid(),
            StatementNo = "SAO-001",
            FromDate = transactionDate.AddDays(-1),
            ToDate = transactionDate.AddDays(1),
            ImportedAt = DateTime.UtcNow,
            ClosingBalance = 1_000_000,
            Lines =
            {
                new BankStatementLine
                {
                    Id = lineId,
                    CompanyId = companyId,
                    BranchId = branchId,
                    TransactionDate = transactionDate,
                    ReferenceNo = "PT-001",
                    CreditAmount = 1_000_000,
                    MatchStatus = "Unmatched"
                }
            }
        });
        db.ReceivableTransactions.Add(new ReceivableTransaction
        {
            Id = receiptId,
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = Guid.NewGuid(),
            SourceType = "PaymentReceipt",
            DocumentNo = "PT-001",
            DocumentDate = transactionDate,
            CreditAmount = 1_000_000,
            BalanceAmount = -1_000_000,
            CurrencyCode = "VND",
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        var engine = new ReconciliationEngine(db, companyContext, TestDbContextFactory.AuditService(db));
        var session = await engine.StartSessionAsync(
            "AR",
            JsonSerializer.Serialize(new { bankStatementId = statementId }));

        var matched = await engine.RunAutoMatchAsync(session.Id);

        matched.Should().Be(1);
        db.BankStatementLines.Single().MatchStatus.Should().Be("Matched");
        var reconciliation = db.ReconciliationMatches.Single();
        reconciliation.SourceType.Should().Be("BankStatementLine");
        reconciliation.SourceId.Should().Be(lineId.ToString("N"));
        reconciliation.TargetType.Should().Be("ReceivableTransaction");
        reconciliation.TargetId.Should().Be(receiptId.ToString("N"));
        db.ReconciliationSessions.Single().Status.Should().Be("Completed");

        (await engine.RunAutoMatchAsync(session.Id)).Should().Be(0);
        db.ReconciliationMatches.Should().ContainSingle();
    }

    [Fact]
    public async Task Auto_match_fails_closed_when_session_has_no_real_statement()
    {
        var companyContext = new TestCompanyContext(Guid.NewGuid(), Guid.NewGuid());
        await using var db = TestDbContextFactory.CreateContext(companyContext, seedAccountingCore: false);
        var engine = new ReconciliationEngine(db, companyContext);
        var session = await engine.StartSessionAsync("AR");

        var act = () => engine.RunAutoMatchAsync(session.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BankStatementId*");
        db.ReconciliationMatches.Should().BeEmpty();
    }

    [Fact]
    public async Task Manual_match_writes_real_ids_and_rejects_amount_mismatch()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var companyContext = new TestCompanyContext(companyId, branchId);
        await using var db = TestDbContextFactory.CreateContext(companyContext, seedAccountingCore: false);
        var statementId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var transactionDate = new DateTime(2026, 9, 8);
        db.BankStatements.Add(new BankStatement
        {
            Id = statementId,
            CompanyId = companyId,
            BranchId = branchId,
            BankAccountId = Guid.NewGuid(),
            StatementNo = "SAO-003",
            FromDate = transactionDate,
            ToDate = transactionDate,
            ImportedAt = DateTime.UtcNow,
            Lines =
            {
                new BankStatementLine
                {
                    Id = lineId,
                    CompanyId = companyId,
                    BranchId = branchId,
                    BankStatementId = statementId,
                    TransactionDate = transactionDate,
                    CreditAmount = 400_000,
                    MatchStatus = "Unmatched"
                }
            }
        });
        db.ReceivableTransactions.Add(new ReceivableTransaction
        {
            Id = receiptId,
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = Guid.NewGuid(),
            SourceType = "PaymentReceipt",
            DocumentNo = "PT-003",
            DocumentDate = transactionDate,
            CreditAmount = 400_000,
            BalanceAmount = -400_000,
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        var engine = new ReconciliationEngine(db, companyContext);
        var session = await engine.StartSessionAsync("AR", JsonSerializer.Serialize(new { bankStatementId = statementId }));

        (await engine.MatchBankStatementLineAsync(session.Id, lineId, receiptId, "tester")).Should().BeTrue();
        var match = db.ReconciliationMatches.Single();
        match.SourceId.Should().Be(lineId.ToString("N"));
        match.TargetId.Should().Be(receiptId.ToString("N"));
        match.IsFuzzy.Should().BeFalse();
        db.BankStatementLines.Single().MatchStatus.Should().Be("Matched");

        var secondLineId = Guid.NewGuid();
        db.BankStatementLines.Add(new BankStatementLine
        {
            Id = secondLineId,
            CompanyId = companyId,
            BranchId = branchId,
            BankStatementId = statementId,
            TransactionDate = transactionDate,
            CreditAmount = 999_000,
            MatchStatus = "Unmatched"
        });
        await db.SaveChangesAsync();
        var mismatchSession = await engine.StartSessionAsync("AR", JsonSerializer.Serialize(new { bankStatementId = statementId }));
        var act = () => engine.MatchBankStatementLineAsync(mismatchSession.Id, secondLineId, receiptId);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không khớp*");
        db.ReconciliationMatches.Should().ContainSingle();
    }

    [Fact]
    public async Task Detect_anomalies_reports_real_unmatched_lines_without_creating_no_data_fake()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var companyContext = new TestCompanyContext(companyId, branchId);
        await using var db = TestDbContextFactory.CreateContext(companyContext, seedAccountingCore: false);
        var statementId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var transactionDate = new DateTime(2026, 9, 8);
        db.BankStatements.Add(new BankStatement
        {
            Id = statementId,
            CompanyId = companyId,
            BranchId = branchId,
            BankAccountId = Guid.NewGuid(),
            StatementNo = "SAO-002",
            FromDate = transactionDate,
            ToDate = transactionDate,
            ImportedAt = DateTime.UtcNow,
            Lines =
            {
                new BankStatementLine
                {
                    Id = lineId,
                    CompanyId = companyId,
                    BranchId = branchId,
                    TransactionDate = transactionDate,
                    Description = "Khoản chưa xác định",
                    CreditAmount = 250_000,
                    MatchStatus = "Unmatched"
                }
            }
        });
        await db.SaveChangesAsync();

        var engine = new ReconciliationEngine(db, companyContext);
        var session = await engine.StartSessionAsync(
            "AR",
            JsonSerializer.Serialize(new { bankStatementId = statementId }));

        (await engine.DetectAnomaliesAsync(session.Id)).Should().Be(1);
        var anomaly = db.ReconciliationAnomalies.Single();
        anomaly.Category.Should().Be("UnmatchedBankStatementLine");
        anomaly.RelatedEntityId.Should().Be(lineId.ToString("N"));
        (await engine.DetectAnomaliesAsync(session.Id)).Should().Be(0);
        db.ReconciliationAnomalies.Should().ContainSingle();
    }

    private sealed class TestCompanyContext(Guid companyId, Guid? branchId) : ICurrentCompanyContext
    {
        public Guid? CompanyId { get; private set; } = companyId;
        public Guid? BranchId { get; private set; } = branchId;
        public string CompanyName => "Test";
        public string? BranchName => "Test";
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            return Task.CompletedTask;
        }

        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);

        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }
}
