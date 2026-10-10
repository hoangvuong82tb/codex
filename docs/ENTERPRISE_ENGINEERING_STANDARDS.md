# ASMILE ERP — ENTERPRISE ENGINEERING STANDARDS

## Engineering Handbook · Toàn bộ lập trình viên phải tuân thủ

**Phiên bản:** 1.0 · **Ngày:** 2026-07-20
**Tham khảo:** Microsoft Engineering Playbook · Google Engineering Practices · Amazon Builders Library

---

# PHẦN 1: CODING STANDARDS

## 1.1. C# Coding Convention

```yaml
NAMING:
  Classes, Records, Structs:  PascalCase  (ReservationService)
  Interfaces:                 IPascalCase (IReservationRepository)
  Methods:                    PascalCase  (CheckInAsync)
  Properties:                 PascalCase  (GuestName)
  Fields (private):           _camelCase (_repository)
  Parameters:                 camelCase   (hotelStayId)
  Local variables:            camelCase   (availableRooms)
  Constants:                  PascalCase  (MaxRetryCount)

BRACES:
  Allman style (new line):
  if (condition)
  {
      // code
  }

  NEVER: K&R style (same line):
  if (condition) {  ← NOT ALLOWED

NAMESPACES:
  ASMILE.BO.{Layer}.{Module}
  Examples:
    ASMILE.BO.Core.FrontOffice.Stay
    ASMILE.BO.Application.FrontOffice.Folio
    ASMILE.BO.Infrastructure.FrontOffice.Payment

FILE ORGANIZATION:
  1. Using statements (System.* first, then third-party, then ASMILE.*)
  2. Namespace declaration
  3. Class/record/struct declaration
  4. Constants and static fields
  5. Private fields
  6. Constructor(s)
  7. Public properties
  8. Public methods
  9. Private methods

ASYNC NAMING:
  - Async methods MUST end with "Async": CheckInAsync(), LoadAsync()
  - CancellationToken as LAST parameter (optional): ct = default
  - Never async void EXCEPT for event handlers
```

## 1.2. C# Code Quality Rules

```yaml
METHOD LENGTH:
  - Maximum 50 lines per method
  - If longer → extract private methods
  - Exception: switch expressions with many cases

CLASS LENGTH:
  - Maximum 500 lines per class
  - If larger → split into partial classes or extract collaborators

PARAMETER COUNT:
  - Maximum 5 parameters per method
  - If more → use record/class as parameter object

CYCLOMATIC COMPLEXITY:
  - Maximum 10 per method
  - Reduce by: extracting methods, using switch expressions, early returns

NULL HANDLING:
  - Prefer nullable reference types (NRT) enabled
  - Use ?? (null coalescing) and ?. (null conditional)
  - NEVER return null from public methods (throw or return Result<T>.Failure)

EXCEPTION HANDLING:
  - Catch SPECIFIC exceptions, never catch(Exception) except at top-level
  - Always include context in exception message
  - Use PlatformException hierarchy for domain errors
  - NEVER swallow exceptions silently (at minimum, log)
```

## 1.3. Comment Rules

```yaml
WHEN TO COMMENT:
  - WHY the code exists (business rationale)
  - NON-OBVIOUS behavior (workaround, optimization trick)
  - PUBLIC API documentation (XML doc comments)

WHEN NOT TO COMMENT:
  - WHAT the code does (should be obvious from naming)
  - HOW the code does it (should be obvious from structure)
  - REMOVED code (use git history)
  - TODO with no owner/date

XML DOC COMMENTS (REQUIRED FOR):
  - All public interfaces
  - All public classes
  - All public methods with non-obvious parameters
  - Example:
    /// <summary>
    /// Checks in a guest from PendingArrival status to InHouse.
    /// Validates room availability, guest identity, and RowVersion concurrency.
    /// </summary>
    /// <param name="command">Check-in command with stay ID and RowVersion.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated HotelStayDto on success, or error details on failure.</returns>
```

---

# PHẦN 2: PROJECT STRUCTURE

## 2.1. Solution Architecture

```yaml
SOLUTION LAYOUT:
  ASMILE.BO.sln
  ├── src/
  │   ├── Core/           ← Domain entities, enums, repository interfaces
  │   ├── Application/    ← DTOs, interfaces, application services, CQRS
  │   ├── Infrastructure/ ← EF Core, repository impl, external services
  │   ├── App/            ← WPF Views, ViewModels, Themes, Converters
  │   └── Shared/         ← Shared utilities, base exceptions
  ├── tests/
  │   ├── Tests.Unit/     ← Unit tests (xUnit + Moq + FluentAssertions)
  │   └── Tests.Integration/ ← Integration tests (LocalDB)
  └── docs/               ← Architecture docs, sprint results

DEPENDENCY RULES (STRICT):
  Core → NO dependencies
  Application → Core, Shared
  Infrastructure → Application, Core, Shared
  App → Application, Core, Shared (NEVER Infrastructure directly)
  Tests → Everything

  VIOLATIONS WILL BE CAUGHT BY ARCHITECTURE TESTS:
  [Fact]
  public void Core_Should_Not_Reference_Infrastructure() { ... }
  [Fact]
  public void Application_Should_Not_Reference_App() { ... }

PROJECT REFERENCES:
  - Use ProjectReference, not direct DLL reference
  - Use PackageReference with EXACT versions (no floating versions)
  - Central package management via Directory.Packages.props (future)
```

## 2.2. File Naming Convention

```yaml
C# FILES:
  Entity:              {EntityName}.cs              (Reservation.cs)
  Interface:           I{ServiceName}.cs            (IReservationRepository.cs)
  Service:             {ServiceName}.cs             (ReservationApplicationService.cs)
  Handler:             {Entity}{Action}Handler.cs   (CheckInHotelStayCommandHandler.cs)
  DTO:                 {Entity}Dto.cs               (HotelStayDto.cs)
  Command:             {Action}{Entity}Command.cs   (CheckInHotelStayCommand.cs)
  Query:               {Action}{Entity}Query.cs     (SearchStaysQuery.cs)
  ViewModel:           {Module}ViewModel.cs         (RoomRackViewModel.cs)
  View:                {Module}View.xaml            (RoomRackView.xaml)
  Converter:           {Purpose}Converter.cs        (StatusToColorConverter.cs)

XAML FILES:
  Views:               Views/{Module}View.xaml
  Themes:              Themes/{Purpose}.xaml        (Buttons.xaml, Colors.xaml)
  Controls:            Controls/{Control}.xaml      (EnterpriseButton.xaml)
  DataTemplates:       Themes/DataTemplates.xaml    (ALL VM→View mappings)

TEST FILES:
  Unit test:           {Class}Tests.cs              (ReservationTests.cs)
  Integration test:    {Module}IntegrationTests.cs  (FrontOfficeIntegrationTests.cs)
  Test naming:         {Method}_Should_{Behavior}_When_{Condition}
```

---

# PHẦN 3: GIT WORKFLOW

## 3.1. Branch Strategy

```yaml
BRANCHES:
  main          ← Production-ready code. ALWAYS deployable.
  develop       ← Integration branch. Feature branches merge here.
  feature/*     ← Feature branches. Created from develop.
  bugfix/*      ← Bug fix branches. Created from develop.
  hotfix/*      ← Emergency production fixes. Created from main.
  release/*     ← Release preparation. Created from develop.

LIFETIME:
  main:          Permanent
  develop:       Permanent
  feature/*:     Deleted after merge (max 5 days old)
  bugfix/*:      Deleted after merge (max 2 days old)
  hotfix/*:      Deleted after merge to main + develop
  release/*:     Deleted after release

WORKFLOW (FEATURE):
  1. git checkout develop && git pull
  2. git checkout -b feature/fo-xxx-description
  3. [work, commit frequently]
  4. git push -u origin feature/fo-xxx-description
  5. Create Pull Request → develop
  6. Code review → approve → squash merge
  7. Delete feature branch

WORKFLOW (HOTFIX):
  1. git checkout main && git pull
  2. git checkout -b hotfix/xxx-description
  3. [fix, commit, test]
  4. Create PR → main → approve → merge
  5. Create PR → develop → approve → merge (cherry-pick)
  6. Delete hotfix branch
  7. Tag release on main
```

## 3.2. Commit Convention

```yaml
FORMAT:
  <type>(<scope>): <description>

TYPES:
  feat:     New feature
  fix:      Bug fix
  refactor: Code restructuring (no behavior change)
  test:     Adding or updating tests
  docs:     Documentation changes
  style:    Formatting, whitespace (no code change)
  perf:     Performance improvement
  chore:    Build, CI/CD, dependencies
  revert:   Revert previous commit

SCOPE:
  fo:       Front Office
  hk:       Housekeeping
  folio:    Folio
  payment:  Payment
  rate:     Rate Engine
  ui:       UI / XAML
  db:       Database / Migration
  platform: Platform Core
  ai:       AI Platform
  integration: Integration Hub

EXAMPLES:
  feat(fo): implement check-out application handler
  fix(folio): correct running balance calculation on reversal
  refactor(payment): extract payment allocation to separate service
  test(fo): add 7 unit tests for check-out modal
  docs(platform): add coding standards section
  chore(db): add index on HotelStay.RoomId for occupancy queries
  perf(ui): enable virtualization on room rack grid

COMMIT SIZE:
  - One logical change per commit
  - Max 20 files changed per commit
  - If more → split into multiple commits
```

## 3.3. Pull Request Rules

```yaml
PR SIZE:
  - Maximum 400 lines changed
  - If larger → reviewer can reject and ask to split

PR TEMPLATE:
  ## Summary
  (1-3 bullet points explaining what this PR does)

  ## Test Plan
  - [ ] Unit tests added/updated
  - [ ] Integration tests pass
  - [ ] Manual smoke test performed

  ## Screenshots (UI changes only)

  ## Checklist
  - [ ] Build passes (0 errors, 0 warnings)
  - [ ] All tests pass
  - [ ] No hardcoded values
  - [ ] No commented-out code
  - [ ] XML doc comments on public API

APPROVAL REQUIREMENTS:
  - Minimum 1 reviewer approval
  - All comments resolved
  - CI pipeline green
  - No merge conflicts
```

---

# PHẦN 4: CODE REVIEW CHECKLIST

```yaml
CORRECTNESS:
  - [ ] Does the code do what it claims to do?
  - [ ] Are edge cases handled? (null, empty, zero, negative, max)
  - [ ] Are business rules enforced?
  - [ ] Is concurrency handled? (RowVersion)

ARCHITECTURE:
  - [ ] Is the change in the right layer? (Domain vs Application vs Infrastructure)
  - [ ] Are dependencies going the right direction?
  - [ ] Are new interfaces in the right project? (.Abstractions if shared)
  - [ ] Is there duplicated logic that should be extracted?

DESIGN:
  - [ ] Is the class/method well-named?
  - [ ] Is the method short enough? (< 50 lines)
  - [ ] Are there too many parameters? (< 5)
  - [ ] Can this be simpler?

TESTING:
  - [ ] Are there unit tests for the new code?
  - [ ] Do tests cover happy path + edge cases + error cases?
  - [ ] Are test names descriptive?
  - [ ] Do existing tests still pass?

PERFORMANCE:
  - [ ] Are database queries efficient? (no N+1)
  - [ ] Is the UI thread blocked? (no sync-over-async)
  - [ ] Are large collections virtualized?
  - [ ] Are there unnecessary allocations in hot paths?

SECURITY:
  - [ ] Are permissions checked?
  - [ ] Is user input validated?
  - [ ] Is PII protected? (not logged, masked in audit)
  - [ ] Are SQL injection vectors closed? (use parameterized queries)

MAINTAINABILITY:
  - [ ] Are magic numbers extracted to named constants?
  - [ ] Are strings extracted to resources? (for UI text)
  - [ ] Is there dead code or commented-out code?
  - [ ] Are TODO comments actionable? (owner + date + ticket)
```

---

# PHẦN 5: TESTING STANDARD

## 5.1. Test Pyramid

```yaml
UNIT TESTS (70%):
  - Test: Domain logic, value objects, specifications, state machines
  - Mock: All external dependencies (repositories, services)
  - Database: InMemory / none
  - Speed: < 100ms per test
  - Count: 300+ and growing

INTEGRATION TESTS (20%):
  - Test: Repository implementations, transaction behavior, concurrency
  - Database: LocalDB (SQL Server Express)
  - Speed: < 5s per test
  - Count: 25+

END-TO-END TESTS (10% — future):
  - Test: Complete business workflows (check-in, night audit)
  - Database: Test instance
  - Speed: < 30s per test
  - Count: Planned

MANUAL TESTS (supplementary):
  - UI smoke test (login, navigate, check-in, check-out)
  - Performance smoke test (500+ rooms in Room Rack)
```

## 5.2. Unit Test Rules

```csharp
// TEST NAMING: Method_Should_Behavior_When_Condition
[Fact]
public async Task CheckIn_Should_TransitionToInHouse_When_StayIsPendingArrival()
{ }

[Fact]
public async Task CheckIn_Should_ReturnError_When_StayIsAlreadyCheckedIn()
{ }

[Fact]
public async Task CheckIn_Should_Throw_When_RowVersionMismatch()
{ }

// TEST STRUCTURE: AAA (Arrange, Act, Assert)
[Fact]
public async Task CalculatePrice_Should_ApplyWeekendSurcharge_OnFriday()
{
    // Arrange
    var plan = RatePlan.Create(..., baseRate: 1_000_000);
    plan.AddRule(RateRuleType.WeekendSurcharge, 20, ...);
    var friday = new DateOnly(2026, 7, 24); // Friday

    // Act
    var (price, _) = plan.CalculatePrice(friday, adults: 2, nights: 1, ...);

    // Assert
    price.Should().Be(1_200_000); // 1,000,000 + 20%
}

// USE FLUENT ASSERTIONS:
result.IsSuccess.Should().BeTrue();
result.Value.Status.Should().Be(HotelStayStatus.InHouse);
rooms.Should().HaveCount(12);
error.Code.Should().Be("STAY_NOT_PENDING_ARRIVAL");

// NEVER:
Assert.True(result.IsSuccess);                ← Don't use Assert.*
Assert.Equal(12, rooms.Count);               ← Don't use Assert.*
if (result.Value.Status != HotelStayStatus.InHouse)
    throw new Exception("Test failed");       ← NEVER write custom assertions
```

## 5.3. Test Coverage Requirements

```yaml
DOMAIN LAYER:        80%+ line coverage
  - All aggregate methods
  - All value object factory methods
  - All state machine transitions
  - All specification evaluations

APPLICATION LAYER:   70%+ line coverage
  - All command handlers (happy path + validation errors)
  - All query handlers
  - All application service methods

INFRASTRUCTURE:      50%+ line coverage
  - Repository methods (integration tests)
  - Number generators
  - Persistence session

WPF UI:              Tested via ViewModel unit tests
  - ViewModels: 60%+ (commands, computed properties, error mapping)
  - Converters: 90%+
  - XAML: manual smoke test
```

---

# PHẦN 6: PERFORMANCE STANDARD

## 6.1. Performance Budget

```yaml
APPLICATION STARTUP:
  - Cold start: < 3 seconds
  - Warm start: < 1 second
  - Module first open: < 1 second

UI RESPONSIVENESS:
  - UI thread never blocked > 16ms (60fps)
  - Room Rack: 500 rooms render < 500ms
  - DataGrid: 10,000 rows virtualized, scroll 60fps
  - Dialog open animation: < 250ms
  - Search response: < 300ms (with 50ms debounce)

DATABASE:
  - Query p95: < 200ms
  - SaveChanges p95: < 100ms
  - Migration apply: < 5 seconds per migration
  - Connection pool: max 20 connections

MEMORY:
  - Idle: < 400MB
  - Active (5 modules open): < 600MB
  - Peak (Night Audit): < 800MB
  - GC pause: < 50ms (gen 2)

NETWORK (future):
  - API response p95: < 500ms
  - OTA sync: < 30 seconds
  - Payment gateway: < 5 seconds
```

## 6.2. Performance Rules

```yaml
DO:
  - Use async/await for ALL I/O (DB, file, network)
  - Use AsNoTracking() for read-only queries
  - Use virtualization for lists > 100 items
  - Use pagination for lists > 1000 items
  - Use compiled queries for hot paths (> 10 calls/second)
  - Cache lookup data (room types, rate plans, countries)
  - Use CancellationToken for all long-running operations

DO NOT:
  - Query in a loop (N+1 problem)
  - Load entire table into memory (use filtering, pagination)
  - Use .Result or .Wait() on async methods (deadlock risk)
  - Create large object graphs (> 1000 objects in memory)
  - Use reflection in hot paths
  - Ignore EF Core warnings (MultipleCollectionInclude, etc.)
```

---

# PHẦN 7: DATABASE GUIDELINE

## 7.1. Migration Rules

```yaml
BEFORE CREATING MIGRATION:
  1. Verify you're on the correct database (check connection string)
  2. Pull latest from develop (migrations may have changed)
  3. Build solution: 0 errors, 0 warnings
  4. Run all tests: ALL PASS

CREATING MIGRATION:
  1. dotnet ef migrations add DescriptiveName --project Infrastructure --startup-project App
  2. Migration name: {DateIfNeeded}_{Action}_{Entity}
     Examples: AddFrontOfficeHotelFolio, AddHousekeepingTaskPriority
  3. Review generated migration: verify Up() and Down() are correct
  4. Verify no DDL outside intended scope
  5. Add migration file to git

APPLYING MIGRATION (DEVELOPMENT):
  1. Backup database
  2. dotnet ef database update --project Infrastructure --startup-project App
  3. Verify tables/columns exist
  4. Run integration tests

APPLYING MIGRATION (PRODUCTION):
  1. Full backup with CHECKSUM + RESTORE VERIFYONLY
  2. Maintenance window (low traffic)
  3. Apply migration
  4. Verify schema + data integrity
  5. Smoke test critical workflows

NEVER:
  - Modify an applied migration (create a NEW migration to fix)
  - Delete a migration file that has been applied to any database
  - Manually edit __EFMigrationsHistory
  - Run Update-Database on production without approval
  - Drop a column without checking it's unused
```

## 7.2. SQL Convention

```yaml
TABLE NAMES:
  - Schema: fo (Front Office), dbo (Core), analytics (Warehouse)
  - PascalCase, tiếng Việt không dấu
  - Examples: DatPhong, LuuTruKhachSan, SoFolio

COLUMN NAMES:
  - PascalCase
  - Suffix for foreign keys: Id
  - Examples: ReservationId, HotelStayId, PropertyId

INDEX NAMES:
  - Format: {Type}_{Schema}_{Table}_{Purpose}
  - Type: PK, FK, IX, UQ
  - Examples: PK_fo_DatPhong, UQ_fo_SoFolio_SoFolio, IX_fo_LuuTruKhachSan_CoSo_TrangThai

SEQUENCE NAMES:
  - Format: TrinhTuSo{Purpose}
  - Examples: TrinhTuSoDatPhong, TrinhTuSoLuuTru, TrinhTuSoFolio

TYPES:
  - Primary keys: uniqueidentifier (UUIDv7)
  - Strings: nvarchar(length) — ALWAYS use Unicode
  - Money: decimal(18,2) — NEVER float or double
  - Percentage: decimal(9,4)
  - Dates: date (no time) or datetime2 (with time) — NEVER datetime
  - RowVersion: rowversion (auto-managed by SQL Server)
```

---

# PHẦN 8: API GUIDELINE (Future)

```yaml
REST API DESIGN:
  - URL: /api/v{version}/{resource}
  - Plural nouns: /api/v1/reservations (NOT /api/v1/reservation)
  - HTTP methods: GET (read), POST (create), PUT (update), DELETE (delete)
  - Use query parameters for filtering: ?status=confirmed&date=2026-07-20

RESPONSE FORMAT:
  Success:
  {
    "data": { ... },
    "requestId": "guid",
    "timestamp": "2026-07-20T08:30:00Z"
  }

  Error:
  {
    "error": {
      "code": "STAY_NOT_FOUND",
      "message": "Không tìm thấy thông tin lưu trú.",
      "details": [...]
    },
    "requestId": "guid"
  }

PAGINATION:
  - Query: ?page=1&pageSize=50
  - Response header: X-Total-Count: 150
  - Response body includes: items[], totalCount, page, pageSize, hasNextPage

VERSIONING:
  - URL-based: /api/v1/reservations, /api/v2/reservations
  - Backward compatible: v1 clients continue to work
  - Deprecation: v1 returns Sunset header 90 days before removal
```

---

# PHẦN 9: CI/CD GUIDELINE (Future)

```yaml
CI PIPELINE (on every push and PR):
  1. Restore packages
  2. Build solution (Debug + Release)
  3. Run unit tests
  4. Run integration tests
  5. Code analysis (Roslyn analyzers)
  6. Architecture tests (dependency rules)
  7. XAML encoding check (mojibake detection)
  8. EF migration validation (no pending model changes)

CD PIPELINE (on merge to main):
  1. All CI steps
  2. Version bump
  3. Build Release
  4. Create MSI installer
  5. Publish to artifact store
  6. Deploy to staging
  7. Smoke tests on staging
  8. Manual approval gate
  9. Deploy to production

PIPELINE MUST:
  - Fail on: build error, test failure, architecture violation
  - Warn on: code analysis warning, performance regression > 10%
  - Notify: Slack/Teams on failure
  - Dashboard: build status, test coverage trend, deployment history
```

---

# PHẦN 10: DOCUMENTATION STANDARD

```yaml
WHAT TO DOCUMENT:
  - Architecture Decision Records (ADRs) for significant decisions
  - Module README for each bounded context
  - API documentation (XML doc comments → Swagger)
  - Sprint results (docs/ketqua.md)
  - Release notes (CHANGELOG.md)

WHAT NOT TO DOCUMENT:
  - Code that explains itself (good naming > comments)
  - Implementation details that change frequently
  - Historical context available in git history

ADR FORMAT:
  # ADR-{NNN}: {Title}
  **Date:** YYYY-MM-DD
  **Status:** Proposed | Accepted | Deprecated | Superseded
  **Context:** (why we need to make this decision)
  **Decision:** (what we decided)
  **Consequences:** (what happens because of this decision)
  **Alternatives Considered:** (what else we considered and why rejected)

DOCUMENTATION LOCATION:
  - Architecture: /docs/architecture/
  - ADRs: /docs/adr/
  - Module docs: /docs/modules/{module}/
  - Sprint results: /docs/ketqua.md (APPEND ONLY, never delete)
  - API docs: auto-generated from XML comments
```

---

# PHẦN 11: AI CODING GUIDELINE

```yaml
AI-GENERATED CODE MUST:
  1. Be reviewed by a human before merge
  2. Follow all coding standards in this document
  3. Include unit tests
  4. Not introduce new dependencies without approval
  5. Not contain hardcoded secrets or connection strings
  6. Use existing abstractions (not create parallel implementations)

AI USAGE:
  - AI is a tool to accelerate development, not replace engineering judgment
  - AI-generated architecture decisions must be reviewed by a senior engineer
  - AI-generated SQL must be reviewed for performance and security
  - AI-generated XAML must pass encoding check (mojibake guard)
  - AI-generated domain logic must be validated against business rules

DO NOT USE AI FOR:
  - Production database migrations (review every line)
  - Financial calculations (must be manually verified)
  - Security-critical code (authentication, authorization)
  - PCI-DSS compliance code (payment processing)
```

---

# PHẦN 12: DEFINITION OF DONE

```yaml
A FEATURE IS DONE WHEN:
  - [ ] Code implements the feature completely
  - [ ] Code follows all coding standards
  - [ ] Unit tests cover happy path + edge cases + error cases
  - [ ] Integration tests pass (if database changes)
  - [ ] Build passes: 0 errors, 0 warnings
  - [ ] Code review approved by at least 1 reviewer
  - [ ] All review comments resolved
  - [ ] No hardcoded values, magic numbers, or dead code
  - [ ] Public API documented (XML doc comments)
  - [ ] Database migration created and tested (if schema change)
  - [ ] UI smoke test performed (if UI change)
  - [ ] Sprint result documented in docs/ketqua.md
  - [ ] Feature flag added (if large feature needing gradual rollout)
  - [ ] Technical debt documented (if any trade-offs made)

A SPRINT IS DONE WHEN:
  - [ ] All features in sprint meet Definition of Done
  - [ ] Full test suite passes (0 failures)
  - [ ] No regression in existing functionality
  - [ ] Sprint report filed
  - [ ] Demo prepared for stakeholder review
```

---

# PHẦN 13: TEAM WORKFLOW

```yaml
DAILY:
  - Standup: 15 min, what did I do, what will I do, any blockers
  - Commit at least once
  - Pull develop at start of day

SPRINT (2 WEEKS):
  - Sprint Planning: Monday morning, 1 hour
  - Daily standups: 15 min
  - Code review: continuous (within 4 hours of PR)
  - Sprint Review: Friday afternoon, 30 min
  - Sprint Retrospective: Friday afternoon, 30 min

ROLES:
  - Senior Engineer: Architecture decisions, code review, mentoring
  - Engineer: Feature development, testing, documentation
  - QA: Manual smoke testing, regression testing, UAT coordination
  - PM: Sprint planning, stakeholder communication, roadmap

COMMUNICATION:
  - Technical discussions: GitHub issues / PR comments
  - Quick questions: internal chat
  - Architecture decisions: ADR in repository
  - Stakeholder updates: Sprint review demo
```

---

# PHẦN 14: TECHNICAL DEBT MANAGEMENT

```yaml
REGISTERING TECHNICAL DEBT:
  - Create GitHub issue with label "technical-debt"
  - Title: [TECH-DEBT] {Description}
  - Body:
    - What: (brief description)
    - Where: (file/class/module)
    - Why: (why it was done this way)
    - Impact: (what it affects)
    - Proposed Fix: (how to fix it)
    - Effort: (S/M/L/XL)

TECHNICAL DEBT BUDGET:
  - Maximum 10% of sprint capacity for tech debt
  - P0 (blocking): fix immediately
  - P1 (high impact): fix within 2 sprints
  - P2 (medium): fix within 4 sprints
  - P3 (low): fix when touching that code

REFACTORING RULES:
  - Refactor as part of feature work (boy scout rule)
  - Large refactors: separate PR, no behavior change
  - Before refactoring: ensure tests exist
  - After refactoring: all tests must still pass
  - NEVER refactor AND add features in the same PR
```

---

# PHẦN 15: TOOLS & ENVIRONMENT

```yaml
REQUIRED TOOLS:
  - .NET SDK 8.0+
  - Visual Studio 2022 / JetBrains Rider 2024+
  - SQL Server 2019+ (Developer Edition)
  - Git for Windows
  - Windows Terminal

RECOMMENDED EXTENSIONS:
  - Roslynator (code analysis)
  - FineCodeCoverage (test coverage visualization)
  - GitLens (git history)
  - XAML Styler (XAML formatting)

ENVIRONMENT SETUP:
  1. Clone repository
  2. Run dotnet restore
  3. Update connection string in appsettings.json (if needed)
  4. Run dotnet build
  5. Run dotnet test
  6. Run dotnet ef database update
  7. Launch application

TROUBLESHOOTING:
  - Build fails: dotnet clean && dotnet restore && dotnet build
  - Migration fails: check connection string, verify SQL Server running
  - Test fails: run test in isolation, check LocalDB instance
  - UI crash: check logs/ folder, verify all resource dictionaries load
```

---

# PHẦN 16: ENGINEERING METRICS

## 16.1. DORA Metrics

```yaml
DEPLOYMENT FREQUENCY:
  Target: Daily (on demand for WPF, weekly for API)
  Elite: Multiple per day
  High: Once per week
  Medium: Once per month
  Low: Once per 6 months

LEAD TIME FOR CHANGES:
  Target: < 24 hours (PR open → merged to main)
  Elite: < 1 hour
  High: < 1 day
  Medium: < 1 week
  Measurement: PR created_at → merged_at (excluding review waiting time)

MEAN TIME TO RECOVER (MTTR):
  Target: < 1 hour (from incident detection to fix deployed)
  For WPF: time from bug report → hotfix released
  Elite: < 1 hour
  High: < 1 day
  Measurement: incident_opened_at → fix_deployed_at

CHANGE FAILURE RATE:
  Target: < 15% (percentage of deployments causing incidents)
  Elite: < 5%
  High: < 15%
  Measurement: deployments_with_incident / total_deployments × 100
```

## 16.2. Team Metrics

```yaml
CODE REVIEW SLA:
  - PR review within 4 business hours
  - Urgent PR (hotfix): within 1 hour
  - Re-review after changes: within 2 hours

BUG ESCAPE RATE:
  - Bugs found in production / total bugs × 100
  - Target: < 10%
  - Track per sprint, per module

TEST COVERAGE TREND:
  - Must not decrease sprint-over-sprint
  - New code: 80%+ line coverage
  - Legacy code: gradual improvement (1-2% per sprint)

CYCLE TIME:
  - Task assigned → PR merged
  - Target: < 3 working days
  - Breakdown: coding (60%), review (20%), testing (20%)
```

---

# PHẦN 17: QUALITY GATES

## 17.1. Build Quality Gates

```yaml
GATE 0: LOCAL BUILD (Developer Machine)
  - dotnet build: 0 errors, 0 warnings
  - dotnet test: ALL PASS
  - XAML encoding check: PASS

GATE 1: CI BUILD (Automated on Push)
  Must PASS ALL:
  - Build (Debug + Release): 0 errors, 0 warnings
  - Unit tests: ALL PASS (threshold: 100%)
  - Integration tests: ALL PASS (threshold: 100%)
  - Architecture tests: ALL PASS
  - XAML mojibake check: PASS

  Must NOT EXCEED:
  - New warnings: 0 (treat warnings as errors)
  - Code duplication: > 3% (SonarQube)
  - Cyclomatic complexity per method: > 10
  - Cognitive complexity per method: > 15

GATE 2: PULL REQUEST
  Must PASS:
  - All CI checks (Gate 1)
  - Minimum 1 reviewer approval
  - All review comments resolved
  - No merge conflicts with target branch

  Must NOT:
  - Decrease test coverage (delta < 0%)
  - Introduce new SonarQube issues (Blocker or Critical)
  - Contain TODO without owner + date

GATE 3: RELEASE
  - Full regression test suite PASS
  - Performance benchmark within budget
  - Security scan: 0 Critical/High vulnerabilities
  - Database migration validated on staging
  - Release notes prepared
  - Rollback plan documented
```

## 17.2. Build Break Policy

```yaml
IF MAIN BRANCH BREAKS:
  1. Breakage detected immediately (CI notification)
  2. Last committer responsible for fix (or revert)
  3. Fix within 30 minutes
  4. If not fixed → revert merge commit
  5. Post-mortem: why was it not caught in PR?

IF DEVELOP BRANCH BREAKS:
  1. Breakage detected immediately
  2. Author of breaking PR responsible
  3. Fix within 2 hours
  4. All other PRs blocked until fixed
  5. Team notified in chat

PREVENTION:
  - All PRs must be up-to-date with target branch before merge
  - "Require branches to be up-to-date before merging" enabled
  - CI must pass on the MERGE COMMIT (not just the feature branch)
```

## 17.3. Warning as Error Policy

```yaml
WARNINGS TREATED AS ERRORS:
  - CS8600: Converting null literal to non-nullable type
  - CS8602: Dereference of possibly null reference
  - CS8603: Possible null reference return
  - CS8625: Cannot convert null literal to non-nullable reference type
  - CS8618: Non-nullable property must contain non-null value

WARNINGS NOT TREATED AS ERRORS (logged only):
  - CS1591: Missing XML comment (tracked, not blocking)
  - CS0618: Obsolete member usage (tracked, not blocking — for migration)
  - CA1416: Platform compatibility (tracked, platform-specific)

.csproj CONFIGURATION:
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsNotAsErrors>CS1591;CS0618;CA1416</WarningsNotAsErrors>
```

---

# PHẦN 18: STATIC ANALYSIS

## 18.1. Analyzer Configuration

```yaml
ROSLYN ANALYZERS (Built-in + NuGet):
  - Microsoft.CodeAnalysis.NetAnalyzers (latest)
  - Microsoft.VisualStudio.Threading.Analyzers
  - Meziantou.Analyzer
  - AsyncFixer
  - Configured via .editorconfig and .globalconfig

RULESET (STRICT):
  - Security rules: Error (block build)
  - Reliability rules: Error
  - Performance rules: Warning
  - Design rules: Warning
  - Naming rules: Error
  - Maintainability rules: Warning

SONARQUBE GATES:
  Quality Gate: PASS required
  - New Bugs: 0
  - New Vulnerabilities: 0
  - New Security Hotspots: 0
  - New Code Smells: < 10
  - Coverage on New Code: > 80%
  - Duplicated Lines on New Code: < 3%

CODEQL (GitHub Advanced Security):
  - Scan on every PR
  - Custom query pack for ASMILE-specific patterns:
    - Direct AppDbContext usage in ViewModel (violation)
    - Missing RowVersion check on financial operations
    - PII in log statements
    - Hardcoded connection strings

OWASP DEPENDENCY CHECK:
  - Run weekly (Monday morning)
  - Alert on: Critical (fix within 24h), High (fix within sprint)
  - Automated PR from Dependabot for patch versions
  - Manual review for major version upgrades

NDepend (quarterly):
  - Dependency graph validation
  - Dead code detection
  - Abstractness vs Instability matrix
  - Technical debt estimation (man-days)
```

## 18.2. Suppression Policy

```yaml
SUPPRESSING A WARNING:
  1. Must have justification in code comment
  2. Must reference issue/ticket number
  3. Must be reviewed by senior engineer
  4. Format:
     #pragma warning disable CA1234 // Justification: See ADR-001. Ticket: ASMILE-1234
     // code
     #pragma warning restore CA1234

  NEVER: Global suppression in .editorconfig without team discussion
  NEVER: Blanket disable all warnings in a file
```

---

# PHẦN 19: SECURE SDLC

## 19.1. Security Activities per Phase

```yaml
DESIGN PHASE:
  - Threat Modeling: STRIDE per module (every 6 months or major change)
  - Secure Design Review: required for:
    - New external integrations
    - Payment processing changes
    - Authentication/Authorization changes
    - PII data flow changes

DEVELOPMENT PHASE:
  - SAST (Static Analysis): every PR (Roslyn + SonarQube)
  - Secret Scan: pre-commit hook + CI pipeline
    Tools: git-secrets, Gitleaks, GitHub Secret Scanning
  - Dependency Scan: weekly (OWASP Dependency Check)
  - CodeQL: every PR

TESTING PHASE:
  - Security unit tests: OWASP Top 10 scenarios
  - DAST (Dynamic Analysis): quarterly on staging (OWASP ZAP)
  - Penetration Testing: annually (external firm)

RELEASE PHASE:
  - SBOM (Software Bill of Materials): auto-generated per release
  - License Compliance: all dependencies must be approved
  - CVE Management: 0 Critical/High at release time
  - Sign-off: Security Review Board approval

OPERATIONS PHASE:
  - Continuous CVE monitoring (Dependabot)
  - Secret rotation: API keys every 90 days, certificates annually
  - Security incident response: documented in runbook
```

## 19.2. CVE Management

```yaml
CVE SEVERITY & SLA:
  Critical (CVSS 9.0-10.0):
    - Acknowledge: 4 hours
    - Fix: 24 hours (or hotfix within 1 business day)
  High (CVSS 7.0-8.9):
    - Acknowledge: 1 business day
    - Fix: within current sprint
  Medium (CVSS 4.0-6.9):
    - Acknowledge: 1 week
    - Fix: within 2 sprints
  Low (CVSS 0.1-3.9):
    - Acknowledge: 1 month
    - Fix: within next quarter

SBOM FORMAT:
  - SPDX 2.3 (ISO/IEC 5962)
  - Generated by: Microsoft.Sbom.Targets (NuGet)
  - Includes: all NuGet packages, versions, licenses, hashes
  - Stored: alongside release artifacts
```

---

# PHẦN 20: RELEASE GOVERNANCE

## 20.1. Release Process

```yaml
RELEASE CANDIDATE (RC):
  1. Create release/x.y.z branch from develop
  2. Version bump (AssemblyInfo + CHANGELOG)
  3. Full regression test suite
  4. Performance benchmark
  5. Security scan
  6. If PASS → promote to Release
  7. If FAIL → fix on release branch → RC2 → repeat

DEPLOYMENT RINGS (for WPF):
  Ring 0: Internal (development team, 5 users)
    - Deploy immediately after release
    - Monitor: 24 hours
  Ring 1: Pilot (1 hotel, power users, 20 users)
    - Deploy after Ring 0 success
    - Monitor: 3 days
  Ring 2: Staged (25% of hotels)
    - Deploy after Ring 1 success
    - Monitor: 1 week
  Ring 3: Broad (all hotels)
    - Deploy after Ring 2 success

ROLLBACK STRATEGY:
  WPF Application:
    1. Revert to previous MSI version
    2. Database: use backup (if migration rollback not possible)
    3. Feature flags: disable new feature (instant)
    4. Client cache: clear AppData

  Database:
    1. If migration has Down(): ef database update <previous>
    2. If no Down(): restore from pre-deployment backup
    3. NEVER manually edit production database

FEATURE FLAGS FOR SAFE RELEASE:
  - All new major features behind feature flag
  - Flag OFF on release day → gradually enable
  - Monitor error rate, performance, user feedback
  - After 2 weeks with no issues → flag removed (graduated)
```

## 20.2. Versioning

```yaml
SEMANTIC VERSIONING (SemVer):
  MAJOR.MINOR.PATCH  (2.1.0)
  MAJOR: Breaking changes (API, database schema)
  MINOR: New features (backward compatible)
  PATCH: Bug fixes

VERSION EXAMPLES:
  2.0.0  ← Initial V2 release
  2.1.0  ← Added Night Audit
  2.1.1  ← Fixed occupancy calculation bug
  3.0.0  ← Breaking: changed API authentication

CHANGELOG FORMAT (Keep a Changelog):
  ## [2.1.0] - 2026-08-01
  ### Added
  - Night Audit module with Business Date roll
  - Auto Room Assignment algorithm
  ### Fixed
  - Occupancy calculation excluding OOO rooms
  ### Changed
  - Rate Engine: priority-based rule execution
```

---

# PHẦN 21: DEPENDENCY GOVERNANCE

## 21.1. NuGet Version Policy

```yaml
VERSION PINNING:
  - All NuGet packages use EXACT versions (no ^, ~, *, >=)
  - Example: <PackageReference Include="Serilog" Version="4.0.0" />
  - NOT: Version="4.*" or Version=">=4.0.0"

UPGRADE POLICY:
  PATCH (4.0.0 → 4.0.1):
    - Auto-approved (Dependabot auto-merge)
    - Must pass CI + tests
  MINOR (4.0.0 → 4.1.0):
    - Review changelog for breaking changes
    - Test in development for 1 sprint before merge
  MAJOR (4.0.0 → 5.0.0):
    - Full evaluation required
    - Proof-of-concept branch
    - Architecture Review Board approval
    - Migration plan documented
    - Scheduled during low-activity period

BREAKING CHANGE DETECTION:
  - Microsoft.CodeAnalysis.PublicApiAnalyzers for ASMILE's own public API
  - NuGet package: PackageExplorer for diff between versions
  - Manual review of changelog for all major upgrades

DEPRECATION POLICY:
  - Public API deprecation: [Obsolete] attribute + message
  - Must explain migration path: "Use CalculatePrice(date, adults, nights) instead"
  - Deprecated API remains for 2 major versions
  - Example: Deprecated in 2.0 → removed in 4.0

DEPENDENCY AUDIT (quarterly):
  - List all dependencies with versions
  - Identify outdated packages
  - Identify packages with known CVEs
  - Identify unused packages (remove them)
  - Consolidate versions (same package, different versions)
```

---

# PHẦN 22: ADR GOVERNANCE

## 22.1. ADR Lifecycle

```yaml
STATES:
  Proposed    ← Anyone can propose. Created as PR to docs/adr/
  In Review   ← Architecture Review Board discussing
  Accepted    ← Approved, merged to main, effective immediately
  Deprecated  ← No longer applicable (but kept for history)
  Superseded  ← Replaced by newer ADR (links to replacement)

WORKFLOW:
  1. Anyone identifies a significant architectural decision
  2. Author writes ADR using template
  3. Author submits PR to docs/adr/ with label "adr"
  4. Architecture Review Board reviews within 1 week
  5. If approved → merge → status = Accepted
  6. If needs changes → author revises
  7. If rejected → PR closed with explanation

ARCHITECTURE REVIEW BOARD (ARB):
  Members: Lead Architect + 2 Senior Engineers (rotating)
  Meets: Weekly (or as needed)
  Quorum: 2 of 3 members
  Decisions: Majority vote, Lead Architect breaks ties

DECISION CRITERIA:
  - Does this align with platform vision?
  - Is this the simplest solution?
  - What are the long-term consequences?
  - Are there proven alternatives?
  - Is this reversible?

RFC (Request for Comments) — for MAJOR decisions:
  - Decisions affecting 3+ modules
  - Decisions with > 1 week implementation impact
  - Decisions changing core platform behavior
  - RFC document published for team review
  - Comment period: 1 week minimum
  - ARB makes final decision after comment period
```

---

# PHẦN 23: OBSERVABILITY STANDARDS

## 23.1. Logging Convention

```yaml
LOG LEVELS:
  Trace:   Detailed diagnostic info (development only)
  Debug:   Development debugging info
  Info:    Normal operations (check-in, payment, sync)
  Warning: Unexpected but handled (retry, fallback, degraded)
  Error:   Operation failed (exception caught, user gets error message)
  Fatal:   Application cannot continue (crash, data corruption)

STRUCTURED LOGGING REQUIRED:
  # GOOD (structured):
  logger.Info("Guest {GuestId} checked into Room {RoomNumber}",
      guestId, roomNumber);
  # BAD (string interpolation):
  logger.Info($"Guest {guestId} checked into Room {roomNumber}");

CORRELATION ID:
  - Every operation gets a CorrelationId (Guid)
  - Passed through: UI → Application → Domain → Infrastructure
  - Included in all log entries for that operation
  - Enables end-to-end tracing of a single user action
  - Format in logs: "{CorrelationId}" property

PII PROTECTION IN LOGS:
  NEVER log:
    - Full guest names (use GuestId)
    - Phone numbers
    - Email addresses
    - Passport/ID numbers
    - Credit card numbers (even masked)
    - Passwords or tokens

  ALWAYS mask:
    - Connection strings (replace password with ***)
    - API keys (show last 4 chars only)
```

## 23.2. OpenTelemetry (Future)

```yaml
TRACING:
  - Every external API call creates a span
  - Every database query creates a span (> 50ms)
  - Every background job creates a trace
  - Span attributes: module, operation, entity_type, entity_id
  - Exporter: OTLP → Jaeger / Azure Monitor

METRICS:
  - Counter: total_checkins, total_payments, total_errors
  - Histogram: checkin_duration_ms, query_duration_ms
  - Gauge: active_sessions, memory_usage_mb, db_connections
  - Exporter: Prometheus / Azure Monitor

SLO / SLA / SLI:
  SLI (Service Level Indicator):
    - API availability: successful_requests / total_requests
    - API latency: p95 response time
    - Error rate: failed_requests / total_requests

  SLO (Service Level Objective):
    - Availability: 99.5% monthly (WPF is local, high bar)
    - API latency: p95 < 500ms
    - Error rate: < 1%

  SLA (Service Level Agreement):
    - External commitment to customers
    - Breach triggers credit/penalty
    - ASMILE is WPF → SLA primarily for API services (future)

ALERT RULES:
  - Error rate > 5% for 5 minutes → Page on-call
  - Latency p95 > 1s for 10 minutes → Warning notification
  - Database connection failures > 3 in 1 minute → Page on-call
  - Memory > 80% for 10 minutes → Warning notification
  - Night Audit failure → Page on-call immediately
  - Payment gateway down → Warning notification + fallback
```

---

# PHẦN 24: SECURE CODING CHECKLIST

## 24.1. OWASP Top 10 Mitigation

```yaml
A01: BROKEN ACCESS CONTROL
  - [ ] Every command handler checks permissions
  - [ ] Every query filters by PropertyId (data isolation)
  - [ ] UI hides/disables unauthorized actions
  - [ ] Direct object references validated (user owns the resource)

A02: CRYPTOGRAPHIC FAILURES
  - [ ] Passwords: PBKDF2 (not MD5, SHA1, or bcrypt alone)
  - [ ] Secrets: DPAPI on Windows (not plain text)
  - [ ] HTTPS for all external communication
  - [ ] TLS 1.2+ minimum (disable SSL, TLS 1.0, 1.1)

A03: INJECTION
  - [ ] SQL: ALWAYS parameterized queries (EF Core does this)
  - [ ] SQL: NEVER string concatenation for queries
  - [ ] XSS: WPF is safe (no HTML rendering), but validate for API
  - [ ] Command injection: NEVER Process.Start with user input
  - [ ] LDAP/XML injection: validate all user input

A04: INSECURE DESIGN
  - [ ] Threat modeling before major features
  - [ ] Security review for financial operations
  - [ ] AI: never auto-execute financial transactions

A05: SECURITY MISCONFIGURATION
  - [ ] Debug mode disabled in production
  - [ ] Detailed errors disabled in production
  - [ ] Default credentials changed
  - [ ] Unnecessary features disabled

A06: VULNERABLE COMPONENTS
  - [ ] Dependency scan weekly
  - [ ] Auto-update patch versions
  - [ ] CVE database monitoring

A07: AUTHENTICATION FAILURES
  - [ ] Password complexity enforced
  - [ ] Account lockout after 5 failures
  - [ ] Session timeout: 8 hours
  - [ ] JWT expiry: 8 hours with refresh token

A08: SOFTWARE AND DATA INTEGRITY FAILURES
  - [ ] NuGet packages from trusted sources only
  - [ ] MSI signed with code signing certificate
  - [ ] Database backups verified (RESTORE VERIFYONLY)

A09: LOGGING AND MONITORING FAILURES
  - [ ] All authentication events logged
  - [ ] All authorization failures logged
  - [ ] All financial transactions logged
  - [ ] Logs protected from tampering (immutable)

A10: SSRF (Server-Side Request Forgery)
  - [ ] API: validate all URLs before fetching
  - [ ] Use allowlist for external domains
  - [ ] Block internal IPs (169.254, 10.x, 192.168.x, 127.x)
```

## 24.2. Secrets Management

```yaml
DEVELOPMENT:
  - appsettings.json: NEVER committed to git
  - Use appsettings.example.json with placeholder values
  - Secrets in user secrets: dotnet user-secrets set "Key" "Value"
  - Local DB: allowed to use simple passwords (localhost only)

PRODUCTION:
  - Windows DPAPI: encrypt connection strings
  - Certificate Store: for SSL certificates
  - Future: Azure Key Vault / HashiCorp Vault

SECRETS NEVER IN:
  - Source code (hardcoded)
  - Git history (even if removed, git keeps history)
  - Log files
  - Error messages
  - Configuration files in repository
  - Screenshots or documentation

SECRETS ROTATION:
  - API keys: every 90 days
  - Database passwords: every 180 days
  - SSL certificates: annually
  - JWT signing keys: every 30 days
```

## 24.3. Encryption Standard

```yaml
DATA AT REST:
  - Database: SQL Server TDE (Transparent Data Encryption) — future
  - File System: BitLocker (Windows)
  - Backups: encrypted with AES-256

DATA IN TRANSIT:
  - External: TLS 1.2+ with strong cipher suites
  - Internal (localhost): trusted connection (Windows Auth)
  - Future API: HTTPS only, HSTS enabled

HASHING:
  - Passwords: PBKDF2 (100,000 iterations, SHA-256)
  - Data integrity: SHA-256
  - File integrity: SHA-256 (backup verification)

PII ENCRYPTION (future):
  - Guest passport/ID numbers: AES-256-GCM
  - Credit card numbers: NEVER stored (tokenize via payment gateway)
  - Encryption keys: stored in DPAPI / Key Vault
```

---

*Enterprise Engineering Standards complete. 24 sections. Every ASMILE engineer must know and follow these standards. No exceptions.*

