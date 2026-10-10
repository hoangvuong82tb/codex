# ASMILE ERP — ENTERPRISE DOMAIN PLATFORM

## Kiến trúc Domain-Driven Design cho toàn bộ hệ sinh thái ASMILE

**Phiên bản:** 1.0
**Ngày:** 2026-07-20
**Loại:** Domain Platform Architecture
**Mục tiêu chất lượng:** Eric Evans DDD · Martin Fowler Patterns · Udi Dahan CQRS · Greg Young Event Sourcing
**Tương đương:** Microsoft Dynamics 365 · SAP S/4HANA · Oracle Fusion · Salesforce Platform

---

# PHẦN 1: DDD ARCHITECTURE OVERVIEW

## 1.1. Domain Layer Positioning

```
┌─────────────────────────────────────────────────────────────┐
│                   PRESENTATION LAYER                         │
│  WPF Views │ ViewModels │ Converters │ Behaviors             │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   APPLICATION LAYER                          │
│  Application Services │ CQRS Handlers │ DTOs │ Validators   │
│  Workflow Orchestration │ Process Managers │ Sagas           │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   DOMAIN LAYER ← THIS DOCUMENT               │
│  Aggregates │ Entities │ Value Objects │ Domain Services    │
│  Domain Events │ Specifications │ Repositories (contracts)  │
│  State Machines │ Business Rules │ Invariants              │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   INFRASTRUCTURE LAYER                       │
│  EF Core │ SQL Server │ Repository Impl │ Outbox │ Cache    │
└─────────────────────────────────────────────────────────────┘
```

## 1.2. Bounded Context Map

```
┌─────────────────────────────────────────────────────────────┐
│                  ASMILE ERP — BOUNDED CONTEXTS               │
│                                                              │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐    │
│  │  Reservation │   │  Hotel Stay  │   │    Folio     │    │
│  │   Context    │──▶│   Context    │──▶│   Context    │    │
│  └──────────────┘   └──────────────┘   └──────────────┘    │
│         │                  │                  │              │
│         ▼                  ▼                  ▼              │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐    │
│  │Guest Profile │   │ Housekeeping │   │   Payment    │    │
│  │   Context    │   │   Context    │   │   Context    │    │
│  └──────────────┘   └──────────────┘   └──────────────┘    │
│                                                              │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐    │
│  │  Rate Plan   │   │Night Audit   │   │  Cashier     │    │
│  │   Context    │   │   Context    │   │   Context    │    │
│  └──────────────┘   └──────────────┘   └──────────────┘    │
│                                                              │
│  ┌──────────────┐   ┌──────────────┐                        │
│  │  Inventory   │   │  Accounting  │   ← Back Office        │
│  │   Context    │   │   Context    │                        │
│  └──────────────┘   └──────────────┘                        │
└─────────────────────────────────────────────────────────────┘

Context Relationship Types:
  ──▶  Customer/Supplier (upstream/downstream)
  ---  Shared Kernel (shared model)
  ═══  Anti-Corruption Layer (translation between contexts)
```

## 1.3. Context Relationship Matrix

| Upstream | Downstream | Relationship | Integration |
|---|---|---|---|
| Reservation | HotelStay | Customer/Supplier | ReservationId → Stay |
| HotelStay | Folio | Customer/Supplier | StayId → Folio |
| HotelStay | Housekeeping | Customer/Supplier | Stay → Room status |
| Folio | Payment | Customer/Supplier | FolioId → Payment |
| GuestProfile | Reservation | Shared Kernel | GuestId shared |
| RatePlan | Reservation | Customer/Supplier | Rate → Reservation |
| HotelStay | NightAudit | Customer/Supplier | Stay data consumed |
| Inventory | Folio | Anti-Corruption Layer | Recipe consumption |

---

# PHẦN 2: AGGREGATE DESIGN PRINCIPLES

## 2.1. The Four Rules of Aggregates

```
RULE 1: Protect Business Invariants
  ─────────────────────────────────
  The Aggregate Root is the gatekeeper.
  ALL modifications to entities within the aggregate
  MUST go through the Aggregate Root.
  NO external code may modify child entities directly.

RULE 2: Design Small Aggregates
  ──────────────────────────────
  Preference: single-entity aggregates.
  Maximum: 3-5 entities per aggregate.
  If an aggregate grows beyond 5 entities, split it.
  Large aggregates cause concurrency conflicts.

RULE 3: Reference Other Aggregates by ID ONLY
  ────────────────────────────────────────────
  HotelStay references RoomId (Guid), NOT Room object.
  Payment references FolioId (Guid), NOT Folio object.
  NEVER hold a reference to another aggregate root.

RULE 4: Eventual Consistency Across Aggregates
  ─────────────────────────────────────────────
  When CheckIn succeeds → publish HotelStayCheckedIn event.
  Housekeeping listens → updates room status.
  Folio listens → creates folio.
  ALL of these happen AFTER the CheckIn transaction commits.
  If Housekeeping update fails → retry, don't rollback CheckIn.
```

## 2.2. Aggregate Design — Front Office Examples

### Aggregate: Reservation

```
Reservation (Aggregate Root)
  │
  ├── Invariant 1: DepartureDate > ArrivalDate
  ├── Invariant 2: AssignedRoom must be compatible with RoomType
  ├── Invariant 3: Confirmed reservations cannot have empty GuestName
  ├── Invariant 4: Cancelled/NoShow are terminal states
  │
  ├── Methods:
  │   ├── Create()
  │   ├── UpdateDetails()
  │   ├── ChangeStayDates()
  │   ├── ChangeGuestCount()
  │   ├── AssignRoom()
  │   ├── UnassignRoom()
  │   ├── Confirm()
  │   ├── Cancel()
  │   └── MarkNoShow()
  │
  └── Events:
      ├── ReservationCreated
      ├── ReservationConfirmed
      ├── ReservationCancelled
      └── ReservationNoShow
```

### Aggregate: HotelStay

```
HotelStay (Aggregate Root)
  │
  ├── Invariant 1: Status must follow state machine
  ├── Invariant 2: One InHouse stay per Room (enforced by DB unique index)
  ├── Invariant 3: CheckIn requires PendingArrival status
  ├── Invariant 4: CheckOut requires InHouse status
  ├── Invariant 5: GuestName cannot be empty
  │
  ├── Methods:
  │   ├── Prepare()
  │   ├── CheckIn(actualCheckInTime)
  │   ├── MarkCheckedOut(actualCheckOutTime)
  │   └── CancelPreparation()
  │
  └── Events:
      ├── HotelStayPrepared
      ├── HotelStayCheckedIn
      └── HotelStayCheckedOut
```

### Aggregate: HotelFolio

```
HotelFolio (Aggregate Root)
  │
  ├── HotelFolioEntry (Child Entity — immutable)
  │     ├── EntryType: Charge, Payment, Deposit, Refund, Adjustment
  │     ├── Amount: decimal (positive = charge, negative = credit)
  │     └── INVARIANT: entries are IMMUTABLE — no edit, no delete
  │
  ├── Invariant 1: RunningBalance = SUM(Entries.Amount) — COMPUTED, not stored
  ├── Invariant 2: Folio cannot be modified when Closed
  ├── Invariant 3: Payment cannot exceed OutstandingBalance
  │
  ├── Methods:
  │   ├── AddCharge()
  │   ├── AddPayment()
  │   ├── AddDeposit()
  │   ├── ReverseEntry()     ← Creates compensating entry, never deletes
  │   ├── BeginSettlement()
  │   └── Close()
  │
  └── Events:
      ├── FolioCreated
      ├── FolioEntryAdded
      ├── FolioSettled
      └── FolioClosed
```

## 2.3. Aggregate Design Rules — Checklist

```yaml
When designing a new aggregate, verify:
  - [ ] Root entity clearly identified
  - [ ] All invariants documented
  - [ ] All methods named as business actions (not CRUD)
  - [ ] All child entities modified ONLY through root
  - [ ] References to other aggregates are by ID only
  - [ ] Maximum 5 entities in the aggregate
  - [ ] Domain events raised for significant state changes
  - [ ] RowVersion for optimistic concurrency
  - [ ] Constructor is private, creation via static factory method
  - [ ] No public setters on domain properties
```

---

# PHẦN 3: VALUE OBJECTS

## 3.1. Value Object Design

```csharp
// ===== VALUE OBJECT: StayPeriod =====
public readonly record struct StayPeriod
{
    public DateOnly ArrivalDate { get; }
    public DateOnly DepartureDate { get; }
    public int Nights => DepartureDate.DayNumber - ArrivalDate.DayNumber;

    private StayPeriod(DateOnly arrival, DateOnly departure)
    {
        ArrivalDate = arrival;
        DepartureDate = departure;
    }

    public static Result<StayPeriod> Create(DateOnly arrival, DateOnly departure)
    {
        if (departure <= arrival)
            return Result<StayPeriod>.Failure("STAY_INVALID_DATES",
                "Departure must be after arrival.");

        return Result<StayPeriod>.Success(new StayPeriod(arrival, departure));
    }

    // Value Object equality: structural, not referential
    public bool Overlaps(StayPeriod other) =>
        ArrivalDate < other.DepartureDate && DepartureDate > other.ArrivalDate;

    // Immutable transformation: returns NEW instance
    public StayPeriod Extend(int days) =>
        new(ArrivalDate, DepartureDate.AddDays(days));
}

// ===== VALUE OBJECT: Money =====
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "VND")
    {
        if (amount < 0)
            return Result<Money>.Failure("MONEY_NEGATIVE",
                "Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            return Result<Money>.Failure("MONEY_NO_CURRENCY",
                "Currency is required.");

        return Result<Money>.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    public static Money Zero(string currency = "VND") => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract different currencies.");
        return new Money(a.Amount - b.Amount, a.Currency);
    }
}

// ===== VALUE OBJECT: GuestCount =====
public readonly record struct GuestCount
{
    public int Adults { get; }
    public int Children { get; }

    private GuestCount(int adults, int children)
    {
        Adults = adults;
        Children = children;
    }

    public static Result<GuestCount> Create(int adults, int children)
    {
        if (adults < 1)
            return Result<GuestCount>.Failure("GUEST_COUNT_INVALID",
                "At least 1 adult is required.");

        if (children < 0)
            return Result<GuestCount>.Failure("GUEST_COUNT_INVALID",
                "Children count cannot be negative.");

        return Result<GuestCount>.Success(new GuestCount(adults, children));
    }

    public int TotalGuests => Adults + Children;
}

// ===== VALUE OBJECT: RoomNumber =====
public readonly record struct RoomNumber
{
    public string Value { get; }

    private RoomNumber(string value) => Value = value;

    public static Result<RoomNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<RoomNumber>.Failure("ROOM_NUMBER_REQUIRED");

        if (value.Length > 10)
            return Result<RoomNumber>.Failure("ROOM_NUMBER_TOO_LONG");

        return Result<RoomNumber>.Success(new RoomNumber(value.Trim()));
    }

    public override string ToString() => Value;
}
```

## 3.2. Value Object Characteristics

```yaml
ALL Value Objects MUST:
  - Be immutable (readonly struct or record struct)
  - Have structural equality (not referential)
  - Be self-validating (factory method, not public constructor)
  - Have no identity (no Id field)
  - Be replaceable as a whole (no partial update)
  - Be serializable to/from database (EF Owned Types)

Benefits:
  - Type safety: StayPeriod vs (DateOnly, DateOnly)
  - Validation at creation: cannot create invalid value
  - Domain logic encapsulation: StayPeriod.Nights, StayPeriod.Overlaps()
  - No primitive obsession
```

---

# PHẦN 4: REPOSITORY PATTERN

## 4.1. Repository Interface Design

```csharp
/// <summary>
/// Repository ONLY for Aggregate Roots.
/// Child entities are accessed through the root.
/// Queries return DTOs, not entities (CQRS).
/// Commands work with domain entities.
/// </summary>
public interface IRepository<TAggregate, TId> where TAggregate : IAggregateRoot
{
    // ===== COMMANDS =====
    Task AddAsync(TAggregate aggregate, CancellationToken ct = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken ct = default);
    Task DeleteAsync(TAggregate aggregate, CancellationToken ct = default); // Soft delete

    // ===== QUERIES (return domain entity — for commands) =====
    Task<TAggregate> GetByIdAsync(TId id, CancellationToken ct = default);

    // ===== CONCURRENCY =====
    void ApplyExpectedRowVersion(TAggregate aggregate, byte[] expectedRowVersion);
}

/// <summary>
/// Separate read-only repository for queries (CQRS).
/// Returns DTOs, not domain entities.
/// </summary>
public interface IQueryRepository<TDto, TQuery>
{
    Task<TDto> GetAsync(TQuery query, CancellationToken ct = default);
    Task<PageResult<TDto>> SearchAsync(TQuery query, CancellationToken ct = default);
}

// ============ CONCRETE EXAMPLES ============

// Reservation Repository
public interface IReservationRepository : IRepository<Reservation, Guid>
{
    // Additional domain-specific queries
    Task<Reservation> GetByNumberAsync(Guid propertyId, string number,
        CancellationToken ct = default);

    Task<bool> HasOverlappingReservationAsync(Guid propertyId, Guid roomId,
        StayPeriod period, CancellationToken ct = default);
}

// HotelStay Repository
public interface IHotelStayRepository : IRepository<HotelStay, Guid>
{
    Task<bool> ExistsInHouseForRoomAsync(Guid propertyId, Guid roomId,
        Guid? excludingStayId = null, CancellationToken ct = default);

    Task<bool> ExistsActiveForReservationAsync(Guid propertyId,
        Guid reservationId, CancellationToken ct = default);

    // CQRS: read-only queries return DTOs
    Task<HotelStayDto> GetDtoAsync(Guid propertyId, Guid stayId,
        CancellationToken ct = default);

    Task<IReadOnlyList<HotelStayDto>> GetCurrentInHouseAsync(Guid propertyId,
        CancellationToken ct = default);
}
```

## 4.2. Repository Implementation

```csharp
public class EfReservationRepository : IReservationRepository
{
    private readonly AppDbContext _db;

    public EfReservationRepository(AppDbContext db) => _db = db;

    // ===== COMMANDS =====
    public async Task AddAsync(Reservation reservation, CancellationToken ct)
    {
        await _db.Set<Reservation>().AddAsync(reservation, ct);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken ct)
    {
        // EF Core tracks changes automatically — no explicit Update needed
        // Entity is already tracked if loaded from DB
        await Task.CompletedTask;
    }

    // ===== QUERIES (entity) =====
    public async Task<Reservation> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<Reservation>()
            .AsTracking()  // IMPORTANT: tracking for change detection
            .FirstOrDefaultAsync(r => r.ReservationId == id, ct);
    }

    public async Task<Reservation> GetByNumberAsync(Guid propertyId,
        string number, CancellationToken ct)
    {
        return await _db.Set<Reservation>()
            .AsTracking()
            .FirstOrDefaultAsync(r => r.PropertyId == propertyId
                && r.ReservationNumber == number, ct);
    }

    // ===== DOMAIN QUERY =====
    public async Task<bool> HasOverlappingReservationAsync(Guid propertyId,
        Guid roomId, StayPeriod period, CancellationToken ct)
    {
        return await _db.Set<Reservation>()
            .AnyAsync(r =>
                r.PropertyId == propertyId
                && r.AssignedRoomId == roomId
                && r.Status == ReservationStatus.Confirmed
                && r.ArrivalDate < period.DepartureDate
                && r.DepartureDate > period.ArrivalDate, ct);
    }

    // ===== CONCURRENCY =====
    public void ApplyExpectedRowVersion(Reservation aggregate,
        byte[] expectedRowVersion)
    {
        _db.Entry(aggregate)
            .Property(nameof(Reservation.RowVersion))
            .OriginalValue = expectedRowVersion;
    }
}
```

## 4.3. Repository Rules

```yaml
DO:
  - Repository ONLY for Aggregate Roots
  - One repository per aggregate
  - Use AsTracking() for command operations
  - Use AsNoTracking() for query operations
  - Return domain entities from command repository methods
  - Return DTOs from query repository methods

DO NOT:
  - Create repositories for child entities
  - Return IQueryable (leaky abstraction)
  - Mix command and query responsibilities in same method
  - Use SaveChanges() inside repository (leave to Unit of Work)

EF Core Integration:
  - AppDbContext = Unit of Work
  - DbSet = Repository (simplified)
  - SaveChangesAsync() = Commit
  - ChangeTracker = Identity Map
```

---

# PHẦN 5: SPECIFICATION PATTERN

## 5.1. Specification Interface

```csharp
/// <summary>
/// Specification pattern — encapsulates business rules
/// that can be combined, reused, and tested independently.
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
    string ErrorMessage { get; }
    string ErrorCode { get; }
}

// Logical operators
public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left,
        ISpecification<T> right) => new AndSpecification<T>(left, right);

    public static ISpecification<T> Or<T>(this ISpecification<T> left,
        ISpecification<T> right) => new OrSpecification<T>(left, right);

    public static ISpecification<T> Not<T>(this ISpecification<T> spec)
        => new NotSpecification<T>(spec);
}
```

## 5.2. Specification Examples

```csharp
// ===== CHECK-IN SPECIFICATIONS =====

public class RoomMustBeAvailableForCheckIn : ISpecification<HotelStay>
{
    public bool IsSatisfiedBy(HotelStay stay)
    {
        return stay.Status == HotelStayStatus.PendingArrival;
    }

    public string ErrorMessage =>
        "Chỉ có thể check-in khách đang ở trạng thái PendingArrival.";
    public string ErrorCode => "STAY_NOT_PENDING_ARRIVAL";
}

public class GuestMustExist : ISpecification<HotelStay>
{
    public bool IsSatisfiedBy(HotelStay stay)
    {
        return stay.GuestId.HasValue
            && !string.IsNullOrWhiteSpace(stay.GuestNameSnapshot);
    }

    public string ErrorMessage =>
        "Khách phải có thông tin đầy đủ trước khi check-in.";
    public string ErrorCode => "STAY_GUEST_REQUIRED";
}

public class RoomMustBeOperational : ISpecification<HotelStay>
{
    private readonly IHotelRoomRepository _rooms;

    public RoomMustBeOperational(IHotelRoomRepository rooms) => _rooms = rooms;

    public bool IsSatisfiedBy(HotelStay stay)
    {
        var room = _rooms.GetByIdAsync(stay.RoomId).Result;
        return room != null
            && room.IsActive
            && room.OperationalStatus == HotelRoomOperationalStatus.Available;
    }

    public string ErrorMessage =>
        "Phòng không ở trạng thái sẵn sàng để nhận khách.";
    public string ErrorCode => "STAY_ROOM_NOT_ASSIGNABLE";
}

// ===== COMBINED SPECIFICATION =====
public class CheckInSpecification
{
    public static ISpecification<HotelStay> Create(IHotelRoomRepository rooms)
    {
        return new RoomMustBeAvailableForCheckIn()
            .And(new GuestMustExist())
            .And(new RoomMustBeOperational(rooms));
    }
}
```

## 5.3. Usage in Application Service

```csharp
public async Task<Result<HotelStayDto>> CheckInAsync(
    CheckInHotelStayCommand command, CancellationToken ct)
{
    // Load aggregate
    var stay = await _stayRepository.GetByIdAsync(command.HotelStayId, ct);
    if (stay is null)
        return Result<HotelStayDto>.Failure("STAY_NOT_FOUND");

    // Validate business rules
    var specification = CheckInSpecification.Create(_roomRepository);
    if (!specification.IsSatisfiedBy(stay))
        return Result<HotelStayDto>.Failure(
            specification.ErrorCode, specification.ErrorMessage);

    // Execute domain logic
    stay.CheckIn(command.ActualCheckInAtUtc ?? _clock.UtcNow);

    // Persist
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<HotelStayDto>.Success(MapToDto(stay));
}
```

---

# PHẦN 6: CQRS (Command Query Responsibility Segregation)

## 6.1. CQRS Architecture

```
                  ┌──────────────────────┐
                  │      CLIENT          │
                  │  (WPF ViewModel)     │
                  └──────┬───────┬───────┘
                         │       │
                    COMMAND    QUERY
                         │       │
              ┌──────────▼──┐ ┌──▼──────────┐
              │ COMMAND BUS  │ │  QUERY BUS  │
              └──────────┬──┘ └──┬──────────┘
                         │       │
              ┌──────────▼──┐ ┌──▼──────────┐
              │   HANDLER   │ │   HANDLER   │
              └──────────┬──┘ └──┬──────────┘
                         │       │
              ┌──────────▼──┐ ┌──▼──────────┐
              │   DOMAIN    │ │    READ     │
              │   MODEL     │ │   MODEL     │
              │  (Write DB) │ │  (Read DB)  │
              └─────────────┘ └─────────────┘

  COMMAND: Changes state — returns Result
  QUERY:   Reads state — returns DTO
  NEVER:   Command returns data, Query changes state
```

## 6.2. Command Definition

```csharp
// ===== BASE INTERFACES =====
public interface ICommand<TResult> { }
public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct);
}

// ===== CONCRETE COMMANDS =====

// Check-in
public record CheckInHotelStayCommand(
    Guid HotelStayId,
    byte[] ExpectedRowVersion,
    DateTime? ActualCheckInAtUtc = null
) : ICommand<HotelStayDto>;

// Create Reservation
public record CreateReservationCommand(
    Guid PropertyId,
    string GuestName,
    string? GuestPhone,
    string? GuestEmail,
    DateOnly ArrivalDate,
    DateOnly DepartureDate,
    int Adults,
    int Children,
    Guid? RoomTypeId,
    Guid? AssignedRoomId,
    string Source
) : ICommand<ReservationDto>;

// Receive Payment
public record ReceivePaymentCommand(
    Guid HotelFolioId,
    byte[] FolioExpectedRowVersion,
    PaymentMethod Method,
    decimal Amount,
    string? ExternalReference = null,
    string? Notes = null
) : ICommand<PaymentDto>;

// ===== CONCRETE HANDLERS =====
public class CheckInHotelStayCommandHandler : ICommandHandler<CheckInHotelStayCommand, HotelStayDto>
{
    private readonly HotelStayApplicationService _service;

    public CheckInHotelStayCommandHandler(HotelStayApplicationService service)
        => _service = service;

    public Task<Result<HotelStayDto>> HandleAsync(
        CheckInHotelStayCommand command, CancellationToken ct)
        => _service.CheckInAsync(command, ct);
}
```

## 6.3. Query Definition

```csharp
// ===== BASE INTERFACES =====
public interface IQuery<TResult> { }
public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct);
}

// ===== CONCRETE QUERIES =====

// Get Stay by ID
public record GetStayByIdQuery(Guid HotelStayId) : IQuery<HotelStayDto>;

// Search Stays
public record SearchStaysQuery(
    string? Keyword,
    HotelStayStatus? Status,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PageRequest Page,
    string? Sort
) : IQuery<PageResult<HotelStayDto>>;

// Get Room Rack
public record GetRoomRackQuery(
    RoomRackFilterDto? Filter,
    string? Search,
    PageRequest Page
) : IQuery<PageResult<RoomRackDto>>;

// ===== CONCRETE HANDLERS =====
public class SearchStaysQueryHandler : IQueryHandler<SearchStaysQuery, PageResult<HotelStayDto>>
{
    private readonly IHotelStayRepository _repository;
    private readonly IFrontOfficeExecutionContextAccessor _context;
    private readonly IAuthorizationService _auth;

    public SearchStaysQueryHandler(
        IHotelStayRepository repository,
        IFrontOfficeExecutionContextAccessor context,
        IAuthorizationService auth)
    {
        _repository = repository;
        _context = context;
        _auth = auth;
    }

    public async Task<Result<PageResult<HotelStayDto>>> HandleAsync(
        SearchStaysQuery query, CancellationToken ct)
    {
        // Authorization
        if (!_auth.HasPermission("FrontOffice.Stay.View"))
            return Result<PageResult<HotelStayDto>>.Failure(
                "STAY_PERMISSION_DENIED");

        // Context
        var context = _context.Current;
        if (context is null)
            return Result<PageResult<HotelStayDto>>.Failure(
                "FRONT_OFFICE_CONTEXT_REQUIRED");

        // Query
        var result = await _repository.SearchAsync(
            context.Property.PropertyId.Value,
            query.Keyword,
            query.Status,
            query.DateFrom,
            query.DateTo,
            query.Page,
            query.Sort,
            ct);

        return Result<PageResult<HotelStayDto>>.Success(result);
    }
}
```

## 6.4. CQRS Pipeline (Mediator)

```csharp
public interface ICommandBus
{
    Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command,
        CancellationToken ct = default);
}

public interface IQueryBus
{
    Task<Result<TResult>> SendAsync<TResult>(IQuery<TResult> query,
        CancellationToken ct = default);
}

// ===== PIPELINE BEHAVIORS (Cross-Cutting Concerns) =====
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request,
        Func<Task<TResponse>> next, CancellationToken ct);
}

// Behaviors execute in order:
// 1. ValidationBehavior       ← Validate command/query
// 2. AuthorizationBehavior    ← Check permissions
// 3. LoggingBehavior          ← Log request/response
// 4. AuditBehavior            ← Audit command execution
// 5. PerformanceBehavior      ← Measure execution time
// 6. RetryBehavior            ← Retry on transient failure
// 7. TransactionBehavior      ← Begin/commit transaction (commands only)
// 8. Actual Handler           ← Execute business logic

public class ValidationBehavior<TCommand, TResult>
    : IPipelineBehavior<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>
{
    private readonly IValidationEngine _validator;

    public async Task<Result<TResult>> HandleAsync(TCommand request,
        Func<Task<Result<TResult>>> next, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return Result<TResult>.Failure(
                validationResult.Errors.First().Code,
                validationResult.Errors.First().Message);

        return await next();
    }
}

public class TransactionBehavior<TCommand, TResult>
    : IPipelineBehavior<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>
{
    private readonly IUnitOfWork _uow;

    public async Task<Result<TResult>> HandleAsync(TCommand request,
        Func<Task<Result<TResult>>> next, CancellationToken ct)
    {
        // Begin transaction
        await _uow.BeginTransactionAsync(ct);

        try
        {
            var result = await next();

            if (result.IsSuccess)
            {
                await _uow.CommitTransactionAsync(ct);
                // AFTER commit: dispatch domain events (outbox)
                await DispatchDomainEventsAsync(ct);
            }
            else
            {
                await _uow.RollbackTransactionAsync(ct);
            }

            return result;
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
```

---

# PHẦN 7: DOMAIN EVENTS

## 7.1. Domain Event Definition

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    string EventName { get; }
}

// ===== CONCRETE DOMAIN EVENTS =====

public record HotelStayCheckedInEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid HotelStayId,
    Guid PropertyId,
    string StayNumber,
    Guid? ReservationId,
    Guid RoomId,
    DateTime ActualCheckInAtUtc,
    DateOnly ScheduledDepartureDate
) : IDomainEvent
{
    public string EventName => "HotelStayCheckedIn";
}

public record PaymentReceivedEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid PaymentId,
    string PaymentNumber,
    Guid HotelFolioId,
    Guid HotelStayId,
    decimal Amount,
    string PaymentMethod,
    DateTime ReceivedAtUtc
) : IDomainEvent
{
    public string EventName => "PaymentReceived";
}
```

## 7.2. Raising Domain Events from Aggregates

```csharp
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _events = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void RaiseEvent(IDomainEvent @event) => _events.Add(@event);

    public void ClearEvents() => _events.Clear();
}

// Usage in aggregate
public class HotelStay : AggregateRoot
{
    public void CheckIn(DateTime actualCheckInAtUtc)
    {
        Require(HotelStayStatus.PendingArrival);

        Status = HotelStayStatus.InHouse;
        ActualCheckInAtUtc = actualCheckInAtUtc;

        // Raise domain event
        RaiseEvent(new HotelStayCheckedInEvent(
            EventId.New(),
            actualCheckInAtUtc,
            HotelStayId,
            PropertyId,
            StayNumber,
            ReservationId,
            RoomId,
            ActualCheckInAtUtc.Value,
            ScheduledDepartureDate
        ));
    }
}
```

## 7.3. Domain Event Dispatch (Outbox Pattern)

```csharp
// Domain events are dispatched AFTER the transaction commits
// NEVER during the transaction — this is the Outbox pattern

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private readonly List<IDomainEvent> _pendingEvents = new();

    public void StageDomainEvent(IDomainEvent @event)
        => _pendingEvents.Add(@event);

    public async Task CommitAsync(CancellationToken ct)
    {
        // 1. Save all changes to DB (aggregates + outbox messages)
        foreach (var @event in _pendingEvents)
        {
            var outboxMessage = OutboxMessage.FromDomainEvent(@event);
            await _db.Set<OutboxMessage>().AddAsync(outboxMessage, ct);
        }

        await _db.SaveChangesAsync(ct);

        // 2. AFTER commit: dispatch events to handlers
        //    (in same process or via message queue)
        foreach (var @event in _pendingEvents)
        {
            await _eventBus.PublishAsync(@event, ct);
        }

        _pendingEvents.Clear();
    }
}
```

---

# PHẦN 8: STATE MACHINE

## 8.1. Reservation State Machine

```
                    ┌─────────┐
                    │  Draft  │
                    └────┬────┘
                         │ Confirm()
                         ▼
              ┌─────────────────────┐
              │    Confirmed        │
              └──┬──────┬──────┬────┘
                 │      │      │
      Cancel()   │      │      │ NoShow()
                 ▼      │      ▼
         ┌──────────┐   │   ┌──────────┐
         │ Cancelled│   │   │  NoShow  │
         └──────────┘   │   └──────────┘
                        │
                   CheckIn() (creates HotelStay)
                        │
                        ▼
              ┌─────────────────────┐
              │    Checked In       │──→ HotelStay takes over
              └─────────────────────┘

  Terminal States: Cancelled, NoShow
  Transition Rules:
    Draft → Tentative (optional): OK
    Draft → Confirmed: OK
    Draft → Cancelled: OK
    Tentative → Confirmed: OK
    Tentative → Cancelled: OK
    Confirmed → Cancelled: OK (requires reason)
    Confirmed → NoShow: OK (after departure date)
    Confirmed → Draft: NOT ALLOWED (forward-only)
    Cancelled → Confirmed: NOT ALLOWED (terminal)
    NoShow → Confirmed: NOT ALLOWED (terminal)
```

## 8.2. State Machine Implementation

```csharp
public class ReservationStateMachine
{
    private static readonly Dictionary<ReservationStatus, HashSet<ReservationStatus>> _transitions = new()
    {
        [ReservationStatus.Draft] = new()
        {
            ReservationStatus.Tentative,
            ReservationStatus.Confirmed,
            ReservationStatus.Cancelled
        },
        [ReservationStatus.Tentative] = new()
        {
            ReservationStatus.Confirmed,
            ReservationStatus.Cancelled
        },
        [ReservationStatus.Confirmed] = new()
        {
            ReservationStatus.Cancelled,
            ReservationStatus.NoShow
        },
        [ReservationStatus.Cancelled] = new(),     // Terminal
        [ReservationStatus.NoShow] = new()          // Terminal
    };

    public static bool CanTransition(ReservationStatus from, ReservationStatus to)
    {
        return _transitions.TryGetValue(from, out var allowed)
            && allowed.Contains(to);
    }

    public static void ValidateTransition(ReservationStatus from, ReservationStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(
                $"Cannot transition from {from} to {to}. " +
                $"Valid transitions: {string.Join(", ", _transitions[from])}");
        }
    }
}

// Usage in aggregate
public class Reservation : AggregateRoot
{
    public void Confirm()
    {
        ReservationStateMachine.ValidateTransition(Status, ReservationStatus.Confirmed);
        Status = ReservationStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        RaiseEvent(new ReservationConfirmedEvent(...));
    }

    public void Cancel(string reason)
    {
        ReservationStateMachine.ValidateTransition(Status, ReservationStatus.Cancelled);
        Status = ReservationStatus.Cancelled;
        CancellationReason = reason;
        CancelledAtUtc = DateTime.UtcNow;
        RaiseEvent(new ReservationCancelledEvent(...));
    }
}
```

## 8.3. Complete State Machine Catalog

```yaml
HotelStay:
  PendingArrival → InHouse (CheckIn)
  InHouse → CheckedOut (MarkCheckedOut)
  PendingArrival → Cancelled (CancelPreparation)

HotelFolio:
  Open → Settling (BeginSettlement)
  Settling → Closed (Close)
  Closed: TERMINAL

Payment:
  Completed → Refunded (Refund)
  Completed → Voided (Void)
  Refunded: TERMINAL
  Voided: TERMINAL

HousekeepingTask:
  Pending → Assigned (Assign)
  Assigned → Accepted (Accept)
  Accepted → Cleaning (StartCleaning)
  Cleaning → WaitingInspection (CompleteCleaning)
  WaitingInspection → Completed (Approve)
  WaitingInspection → Pending (Reject — back for re-cleaning)
  Any → Cancelled (Cancel)

NightAudit:
  Running → Completed (Complete)
  Running → Failed (Fail)
  Running → Cancelled (Cancel)
  Completed: TERMINAL
  Failed: TERMINAL
  Cancelled: TERMINAL

BusinessDate:
  Open → Closing (BeginClosing)
  Closing → Closed (MarkClosed)
  Closed: TERMINAL
```

---

# PHẦN 9: WORKFLOW ENGINE

## 9.1. Workflow Definition

```csharp
public interface IWorkflowDefinition<TState, TStep, TSignal>
    where TState : Enum
    where TStep : Enum
    where TSignal : Enum
{
    string WorkflowName { get; }
    int Version { get; }

    // Transition rules
    bool CanTransition(TState from, TState to);
    IReadOnlyList<TSignal> GetAllowedSignals(TState state);

    // Step rules
    TStep GetInitialStep();
    bool IsTerminal(TState state);
    bool IsTerminal(TStep step);

    // Timeout rules
    TimeSpan? GetStepTimeout(TStep step);

    // Retry rules
    RetryPolicy GetRetryPolicy(TStep step);

    // Compensation rules
    ICompensationHandler GetCompensationHandler(TStep step);
}

// Example: Night Audit Workflow
public class NightAuditWorkflow : IWorkflowDefinition<NightAuditStatus, NightAuditStep, NightAuditSignal>
{
    public string WorkflowName => "NightAudit";
    public int Version => 1;

    public bool CanTransition(NightAuditStatus from, NightAuditStatus to)
        => (from, to) switch
        {
            (NightAuditStatus.Running, NightAuditStatus.Completed) => true,
            (NightAuditStatus.Running, NightAuditStatus.Failed) => true,
            (NightAuditStatus.Running, NightAuditStatus.Cancelled) => true,
            _ => false
        };

    public IReadOnlyList<NightAuditSignal> GetAllowedSignals(NightAuditStatus state)
        => state switch
        {
            NightAuditStatus.Running => new[] {
                NightAuditSignal.StepCompleted,
                NightAuditSignal.Error,
                NightAuditSignal.Cancel },
            _ => Array.Empty<NightAuditSignal>()
        };

    public NightAuditStep GetInitialStep() => NightAuditStep.LockProperty;

    public bool IsTerminal(NightAuditStatus state)
        => state is NightAuditStatus.Completed
            or NightAuditStatus.Failed
            or NightAuditStatus.Cancelled;

    public TimeSpan? GetStepTimeout(NightAuditStep step) => null; // No timeout

    public RetryPolicy GetRetryPolicy(NightAuditStep step)
        => step == NightAuditStep.GenerateRevenueSnapshot
            ? new RetryPolicy(3, TimeSpan.FromMinutes(1)) // Retry snapshot 3 times
            : RetryPolicy.NoRetry;
}
```

## 9.2. Workflow Instance

```csharp
public class WorkflowInstance
{
    public Guid WorkflowId { get; private set; }
    public string WorkflowName { get; private set; }
    public int Version { get; private set; }
    public string CurrentState { get; private set; }
    public string CurrentStep { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string ErrorMessage { get; private set; }
    public Dictionary<string, string> Context { get; private set; }
    public List<WorkflowStepExecution> StepHistory { get; private set; }
}

public class WorkflowStepExecution
{
    public string Step { get; set; }
    public WorkflowStepStatus Status { get; set; }  // Pending, Completed, Skipped, Failed
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
```

---

# PHẦN 10: RULE ENGINE

## 10.1. Business Rule Definition

```csharp
public interface IBusinessRule
{
    string RuleId { get; }
    string Name { get; }
    string Description { get; }
    int Priority { get; }
    bool IsActive { get; }

    Task<RuleEvaluationResult> EvaluateAsync(object context);
}

public class RuleEvaluationResult
{
    public bool Passed { get; set; }
    public string Message { get; set; }
    public RuleSeverity Severity { get; set; }
    public Dictionary<string, object> Details { get; set; }
}

public enum RuleSeverity
{
    Error,       // Block operation
    Warning,     // Allow but warn
    Info         // Informational
}
```

## 10.2. Built-in Rules Catalog

```csharp
// ===== RESERVATION RULES =====

public class ReservationDateRule : IBusinessRule
{
    public string RuleId => "RES-DATE-001";
    public string Name => "Valid Stay Dates";
    public int Priority => 1;

    public Task<RuleEvaluationResult> EvaluateAsync(object context)
    {
        var reservation = (Reservation)context;
        if (reservation.DepartureDate <= reservation.ArrivalDate)
            return Task.FromResult(new RuleEvaluationResult
            {
                Passed = false,
                Message = "Ngày đi phải sau ngày đến.",
                Severity = RuleSeverity.Error
            });
        return Task.FromResult(new RuleEvaluationResult { Passed = true });
    }
}

// ===== PAYMENT RULES =====

public class PaymentAmountPositiveRule : IBusinessRule
{
    public string RuleId => "PAY-001";
    public string Name => "Payment Amount Positive";
    public int Priority => 1;

    public Task<RuleEvaluationResult> EvaluateAsync(object context)
    {
        var payment = (Payment)context;
        if (payment.Amount <= 0)
            return Task.FromResult(new RuleEvaluationResult
            {
                Passed = false,
                Message = "Số tiền thanh toán phải lớn hơn 0.",
                Severity = RuleSeverity.Error
            });
        return Task.FromResult(new RuleEvaluationResult { Passed = true });
    }
}

// ===== NIGHT AUDIT RULES =====

public class NoOpenSettlementsRule : IBusinessRule
{
    public string RuleId => "NA-001";
    public string Name => "No Open Settlements";

    public async Task<RuleEvaluationResult> EvaluateAsync(object context)
    {
        var propertyId = (Guid)context;
        var openSettlements = await _folioRepo.CountOpenSettlementsAsync(propertyId);

        if (openSettlements > 0)
            return new RuleEvaluationResult
            {
                Passed = false,
                Message = $"Còn {openSettlements} folio đang mở. Vui lòng quyết toán trước khi Night Audit.",
                Severity = RuleSeverity.Error
            };

        return new RuleEvaluationResult { Passed = true };
    }
}
```

## 10.3. Rule Engine Execution

```csharp
public class RuleEngine
{
    private readonly List<IBusinessRule> _rules = new();

    public void Register(IBusinessRule rule) => _rules.Add(rule);

    public async Task<RuleEngineResult> ExecuteAsync(object context)
    {
        var results = new List<RuleEvaluationResult>();

        foreach (var rule in _rules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority))
        {
            var result = await rule.EvaluateAsync(context);
            results.Add(result);

            // Stop on first error (fail-fast)
            if (result.Severity == RuleSeverity.Error && !result.Passed)
                break;
        }

        return new RuleEngineResult
        {
            Passed = results.All(r => r.Passed || r.Severity != RuleSeverity.Error),
            Results = results
        };
    }
}

public class RuleEngineResult
{
    public bool Passed { get; set; }
    public List<RuleEvaluationResult> Results { get; set; }
    public List<RuleEvaluationResult> Errors => Results
        .Where(r => !r.Passed && r.Severity == RuleSeverity.Error).ToList();
    public List<RuleEvaluationResult> Warnings => Results
        .Where(r => !r.Passed && r.Severity == RuleSeverity.Warning).ToList();
}
```

---

# PHẦN 11: SAGA / PROCESS MANAGER

## 11.1. Saga Pattern for Long-Running Transactions

```csharp
/// <summary>
/// Saga coordinates a long-running business process
/// across multiple aggregates.
/// Each step can be compensated if a later step fails.
/// </summary>
public interface ISaga<TData> where TData : SagaData
{
    Task StartAsync(TData data);
    Task ResumeAsync(Guid sagaId);  // Resume after crash
}

public abstract class SagaData
{
    public Guid SagaId { get; set; }
    public string CurrentStep { get; set; }
    public SagaStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public string ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, string> Context { get; set; }
}

public enum SagaStatus
{
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated
}

// ===== EXAMPLE: GROUP CHECK-IN SAGA =====
public class GroupCheckInSagaData : SagaData
{
    public Guid GroupReservationId { get; set; }
    public List<Guid> StayIds { get; set; } = new();
    public int TotalRooms { get; set; }
    public int CheckedInRooms { get; set; }
}

public class GroupCheckInSaga : ISaga<GroupCheckInSagaData>
{
    private readonly ICommandBus _commands;

    public async Task StartAsync(GroupCheckInSagaData data)
    {
        // Step 1: Create stays for all rooms
        foreach (var assignment in data.StayIds)
        {
            var result = await _commands.SendAsync(
                new PrepareStayFromReservationCommand(assignment));
            if (result.IsFailure)
            {
                await CompensateAsync(data); // Rollback created stays
                return;
            }
        }

        // Step 2: Check in all stays
        foreach (var stayId in data.StayIds)
        {
            var result = await _commands.SendAsync(
                new CheckInHotelStayCommand(stayId, GetRowVersion(stayId)));
            if (result.IsFailure)
            {
                await CompensateAsync(data); // Rollback checked-in stays
                return;
            }
            data.CheckedInRooms++;
        }

        data.Status = SagaStatus.Completed;
    }

    private async Task CompensateAsync(GroupCheckInSagaData data)
    {
        data.Status = SagaStatus.Compensating;

        // Reverse: cancel checked-in stays
        foreach (var stayId in data.StayIds)
        {
            await _commands.SendAsync(new CancelStayCommand(stayId));
        }

        data.Status = SagaStatus.Compensated;
    }
}
```

---

# PHẦN 12: OUTBOX PATTERN

## 12.1. Outbox Architecture

```
Transaction Boundary
┌──────────────────────────────────────────┐
│  1. Save aggregate (HotelStay)           │
│  2. Save domain events as outbox messages│ ← Same transaction!
│  3. Commit transaction                   │
└──────────────────────────────────────────┘
                    │
                    ▼ (AFTER commit)
┌──────────────────────────────────────────┐
│  Outbox Dispatcher (background)          │
│  4. Read pending outbox messages         │
│  5. Publish to EventBus                 │
│  6. Mark as published                   │
│  ─────────────────────────────────────── │
│  If publish fails → retry (with backoff) │
│  If crash → next startup picks up        │
└──────────────────────────────────────────┘
```

## 12.2. Outbox Implementation

```csharp
public class OutboxMessage
{
    public Guid MessageId { get; set; }
    public string EventName { get; set; }
    public string EventPayload { get; set; }    // JSON
    public string PayloadHash { get; set; }    // SHA256
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public static OutboxMessage FromDomainEvent(IDomainEvent @event)
    {
        var json = JsonSerializer.Serialize(@event, @event.GetType());
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(json)));

        return new OutboxMessage
        {
            MessageId = @event.EventId,
            EventName = @event.EventName,
            EventPayload = json,
            PayloadHash = hash,
            OccurredAtUtc = @event.OccurredAtUtc
        };
    }
}

public class OutboxDispatcher : IOutboxDispatcher
{
    private readonly AppDbContext _db;
    private readonly IEventBus _eventBus;

    public async Task DispatchPendingAsync(CancellationToken ct)
    {
        var pending = await _db.Set<OutboxMessage>()
            .Where(m => m.PublishedAtUtc == null
                && (m.NextRetryAt == null || m.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(m => m.OccurredAtUtc)
            .Take(100)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            try
            {
                var @event = JsonSerializer.Deserialize(
                    message.EventPayload,
                    Type.GetType(message.EventName));

                await _eventBus.PublishAsync((IDomainEvent)@event, ct);

                message.PublishedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.ErrorMessage = ex.Message;
                message.NextRetryAt = DateTime.UtcNow.AddMinutes(
                    Math.Pow(2, message.RetryCount)); // Exponential backoff

                if (message.RetryCount >= 10)
                {
                    // Move to dead letter after 10 retries
                    message.NextRetryAt = null; // Stop retrying
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

---

# PHẦN 13: OPTIMISTIC CONCURRENCY

## 13.1. Concurrency Strategy

```csharp
// ===== OPTIMISTIC CONCURRENCY — THE DEFAULT =====
// All aggregates use RowVersion (SQL Server rowversion/timestamp)
// On save, EF Core compares RowVersion
// If changed → DbUpdateConcurrencyException → map to domain error

public async Task<Result<HotelStayDto>> CheckInAsync(CheckInHotelStayCommand cmd, CancellationToken ct)
{
    // 1. Load aggregate
    var stay = await _repository.GetByIdAsync(cmd.HotelStayId, ct);

    // 2. Apply expected version (optimistic lock)
    _repository.ApplyExpectedRowVersion(stay, cmd.ExpectedRowVersion);

    // 3. Execute business logic
    stay.CheckIn(cmd.ActualCheckInAtUtc ?? _clock.UtcNow);

    // 4. Save
    try
    {
        await _uow.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        // RowVersion mismatch → someone else changed this stay
        // Clear tracking, return conflict error
        _uow.ClearTracker();
        return Result<HotelStayDto>.Failure(
            Error.Concurrency("STAY_CONCURRENCY_CONFLICT",
                "Stay has been modified by another user. Please reload."));
    }

    return Result<HotelStayDto>.Success(MapToDto(stay));
}

// ===== PESSIMISTIC CONCURRENCY — RARE, FOR HIGH-CONTENTION =====
// Example: Night Audit — only ONE instance can run at a time
public async Task BeginNightAuditAsync()
{
    // Use database lock to prevent concurrent night audits
    await _db.Database.ExecuteSqlRawAsync(
        "SELECT 1 FROM NightAuditLock WITH (UPDLOCK, ROWLOCK) " +
        "WHERE PropertyId = {0}", propertyId);
}
```

## 13.2. Conflict Resolution Strategies

```yaml
Strategy 1: Last Write Wins (NOT RECOMMENDED for finance)
  - Do not check RowVersion
  - Last save overwrites previous changes
  - Use case: user preferences, UI layout

Strategy 2: Optimistic Concurrency (DEFAULT)
  - Check RowVersion on save
  - If conflict → fail, ask user to reload
  - Use case: Reservation, Stay, Folio, Payment

Strategy 3: Merge (COMPLEX — rarely needed)
  - Detect which fields changed
  - Attempt to merge changes
  - If conflict on same field → fail
  - Use case: large documents with multiple editors

Strategy 4: Pessimistic Lock (PREVENT conflicts)
  - Lock the record before editing
  - Nobody else can edit until released
  - Use case: Night Audit, shift close, inventory adjustment
  - Risk: deadlocks, lock timeouts
```

---

# PHẦN 14: FOLDER STRUCTURE

```
ASMILE.BO.Domain/
├── Abstractions/
│   ├── IAggregateRoot.cs
│   ├── IEntity.cs
│   ├── IValueObject.cs
│   ├── IDomainEvent.cs
│   ├── IDomainService.cs
│   ├── ISpecification.cs
│   ├── IRepository.cs
│   └── IUnitOfWork.cs
│
├── Shared/
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   ├── StayPeriod.cs
│   │   ├── GuestCount.cs
│   │   └── RoomNumber.cs
│   ├── Specifications/
│   │   ├── AndSpecification.cs
│   │   ├── OrSpecification.cs
│   │   └── NotSpecification.cs
│   └── Entity.cs                   ← Base entity class
│
├── Reservation/
│   ├── Reservation.cs              ← Aggregate Root
│   ├── ReservationStatus.cs        ← Enum
│   ├── ReservationStateMachine.cs
│   ├── ReservationSpecifications.cs
│   ├── ReservationDomainService.cs
│   └── Events/
│       ├── ReservationCreatedEvent.cs
│       ├── ReservationConfirmedEvent.cs
│       ├── ReservationCancelledEvent.cs
│       └── ReservationNoShowEvent.cs
│
├── HotelStay/
│   ├── HotelStay.cs
│   ├── HotelStayStatus.cs
│   ├── HotelStayHistory.cs
│   └── Events/
│       ├── HotelStayPreparedEvent.cs
│       ├── HotelStayCheckedInEvent.cs
│       └── HotelStayCheckedOutEvent.cs
│
├── Folio/
│   ├── HotelFolio.cs
│   ├── HotelFolioEntry.cs
│   └── Events/
│       ├── FolioCreatedEvent.cs
│       ├── FolioEntryAddedEvent.cs
│       └── FolioClosedEvent.cs
│
├── Payment/
├── RatePlan/
├── Housekeeping/
├── NightAudit/
├── Cashier/
├── Guest/
├── ServiceRequest/
├── WakeUpCall/
├── LostFound/
├── Concierge/
├── Message/
├── Feedback/
├── Loyalty/
├── Task/
├── Notification/
└── Audit/
```

---

# PHẦN 15: CODING STANDARDS (Domain)

```yaml
Aggregate Design:
  - One aggregate per bounded context (typically)
  - Aggregate root class name matches bounded context
  - Factory method: static Create() — returns Result<T>
  - Constructor: private
  - Properties: { get; private set; }
  - Methods named as business actions (not CRUD)
  - Methods throw on invariant violation (caught by handler)
  - Methods raise domain events for significant changes

Value Objects:
  - Use 'readonly record struct' (C# 10+)
  - Private constructor + static factory method: Create()
  - Factory returns Result<T> (can fail validation)
  - Implement IEquatable<T> (auto for records)
  - Immutable — all transformations return new instance

Domain Events:
  - Use 'record' types (immutable)
  - Named in past tense: HotelStayCheckedIn, PaymentReceived
  - Contain: EventId, OccurredAtUtc, aggregate ID, relevant data
  - NO PII in events

Repositories:
  - Interface in Domain layer
  - Implementation in Infrastructure layer
  - Return domain entities (not DTOs) for commands
  - Accept domain entities for persistence
  - Use specification pattern for complex queries

Application Services:
  - Orchestrate use cases (not contain business logic)
  - Load aggregates → call methods → persist
  - Return Result<T>
  - Handle concurrency conflicts
  - Stage domain events for outbox

CQRS:
  - Commands: immutable records, return Result<T>
  - Queries: immutable records, return DTO
  - Command handlers: ONE aggregate per handler
  - Query handlers: can join multiple tables/sources
  - NEVER mix: command handler should not query, query handler should not mutate
```

---

# PHẦN 16: EVENT SOURCING READINESS

```csharp
/// <summary>
/// While NOT implementing full Event Sourcing now,
/// the domain is designed to be Event Sourcing-ready.
/// </summary>

// 1. All state changes raise domain events
// 2. Events are named in past tense
// 3. Events contain all data needed to reconstruct state
// 4. Aggregates have Version property (prepared for event versioning)

public abstract class EventSourcingReadyAggregate : AggregateRoot
{
    public int Version { get; private set; } = 1;

    // In future Event Sourcing:
    // public static TAggregate Replay(IEnumerable<IDomainEvent> events)
    // {
    //     var aggregate = new TAggregate();
    //     foreach (var @event in events)
    //         aggregate.Apply(@event);
    //     return aggregate;
    // }

    // private void Apply(IDomainEvent @event)
    // {
    //     switch (@event)
    //     {
    //         case HotelStayCheckedInEvent e: ApplyCheckedIn(e); break;
    //         case HotelStayCheckedOutEvent e: ApplyCheckedOut(e); break;
    //     }
    //     Version++;
    // }
}
```

---

# PHẦN 17: ARCHITECTURE DECISION RECORDS

### ADR-D001: One Aggregate Per Bounded Context
**Decision:** Each bounded context has exactly one primary aggregate root. Additional entities within the context are children of that root.
**Rationale:** Simplifies transaction boundaries. Reduces cross-aggregate coordination. Follows DDD best practices.

### ADR-D002: CQRS with Shared Database
**Decision:** Use CQRS pattern with command/query separation, but share the same SQL Server database (not separate read/write databases).
**Rationale:** Hotel PMS does not need the complexity of separate read stores. Query performance is adequate with proper indexes. Eventual consistency is handled via domain events.

### ADR-D003: Optimistic Concurrency as Default
**Decision:** Use RowVersion-based optimistic concurrency for all aggregates. Pessimistic locking only for Night Audit.
**Rationale:** Hotel staff rarely edit the same record simultaneously. Optimistic concurrency is simpler, doesn't cause deadlocks, and fails gracefully with user-friendly messages.

### ADR-D004: Outbox Pattern for Domain Events
**Decision:** Persist domain events in same transaction as aggregate changes. Dispatch asynchronously after commit.
**Rationale:** Guarantees at-least-once delivery. No events lost on crash. Simple to implement with existing SQL Server. No need for distributed transactions.

### ADR-D005: State Machines for Entity Lifecycle
**Decision:** Use explicit state machines (not string status fields) for all entity lifecycles.
**Rationale:** Prevents invalid state transitions. Documents valid transitions. Self-documenting code. Easy to test.

### ADR-D006: No Event Sourcing (Yet)
**Decision:** Design for Event Sourcing readiness but do NOT implement full Event Sourcing in V1.
**Rationale:** Event Sourcing adds significant complexity (event store, projections, snapshots, replay). Full audit trail achieved via audit logging. Can migrate to Event Sourcing later if needed.

---

# PHẦN 18: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| DOM-001 | Domain Abstractions (IAggregateRoot, IEntity, IValueObject, IRepository...) | 2 ngày |
| DOM-002 | Shared Value Objects (Money, StayPeriod, GuestCount, RoomNumber) | 2 ngày |
| DOM-003 | Specification pattern + built-in specifications | 2 ngày |
| DOM-004 | State machine framework + all state machines | 3 ngày |
| DOM-005 | CQRS base interfaces + Mediator implementation | 3 ngày |
| DOM-006 | CQRS pipeline (Validation, Auth, Logging, Audit, Transaction) | 3 ngày |
| DOM-007 | Domain events + Outbox pattern implementation | 3 ngày |
| DOM-008 | Workflow engine (definition, instance, step execution) | 3 ngày |
| DOM-009 | Rule engine + built-in rules catalog | 2 ngày |
| DOM-010 | Saga/Process Manager framework | 2 ngày |
| DOM-011 | Concurrency strategy + conflict resolution | 2 ngày |
| DOM-012 | Repository base implementation (EF Core) | 2 ngày |
| DOM-013 | Unit of Work + Transaction management | 2 ngày |
| DOM-014 | Migration: Reservation aggregate → new DDD patterns | 2 ngày |
| DOM-015 | Migration: HotelStay aggregate → new DDD patterns | 2 ngày |
| DOM-016 | Migration: Folio + Payment aggregates | 2 ngày |
| DOM-017 | Migration: Remaining FO aggregates | 3 ngày |
| DOM-018 | Integration testing + Regression testing | 3 ngày |

**Total: ~41 working days (8 weeks)**

---

*Enterprise Domain Platform architecture complete. 18 sections. This is the domain kernel that every ASMILE business module — Front Office, Back Office, POS, Restaurant, Banquet, Accounting, CRM, HR — will be built upon.*
