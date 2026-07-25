# KET QUA FRONT OFFICE STAY MANAGEMENT FOUNDATION ASMILE-BO

## 1. Executive Summary

Da hoan thanh Stay Management Foundation cho Front Office. `HotelStay` la aggregate rieng, khong bien Reservation thanh Stay va khong kich hoat Check-in, Check-out, Folio, Payment, Night Audit hay Housekeeping occupancy.

## 2. Scope

Da thuc hien domain, state machine, reservation-to-stay preparation, repository, SQL Server persistence, RowVersion, history, audit, outbox, permission catalog, migration, LocalDB tests, DI validation va Reception read-only integration.

## 3. Discovery

Giai phap su dung dung cac convention hien co: `IClock`, `IFrontOfficeExecutionContextAccessor`, `IFrontOfficePersistenceSession`, `IIntegrationEventMapper`, SQL sequence, `Uuid7`, EF RowVersion va property-scoped repository.

## 4. HotelStay Domain

Aggregate `HotelStay` co ID, PropertyId, StayNumber, reference/snapshot Reservation, guest snapshot, room/type snapshot, scheduled/actual dates, guest count, status, source, audit fields va RowVersion. Khong co entity reference truc tiep va khong co public arbitrary setter.

## 5. State Machine

Trang thai: `PendingArrival`, `InHouse`, `CheckedOut`, `Cancelled`. Aggregate chi cho phep `PendingArrival -> InHouse -> CheckedOut` va `PendingArrival -> Cancelled`. Khong co application command hoac UI de thuc thi Check-in/Check-out trong sprint nay.

## 6. Reservation Relationship

`PrepareStayFromReservationCommand` chi lay `ReservationId`; PropertyId luon lay tu trusted execution context. Reservation phai `Confirmed`, co guest snapshot/GuestId, AssignedRoom va RoomType. Reservation khong bi doi status.

## 7. Guest Boundary

Khong tao Guest fake. `GuestId` duoc giu nullable trong schema de phu hop boundary Reservation/CRM hien tai, nhung prepare tu Reservation confirmed bat buoc co GuestId va `GuestNameSnapshot`. Phone snapshot co the luu theo policy Reservation hien co, nhung khong dua vao audit/outbox.

## 8. Room Boundary

Room phai thuoc dung Property, active, `Available`, va dung RoomType cua Reservation. Snapshot `RoomNumber` va `RoomTypeCode` duoc copy vao Stay. PendingArrival khong doi room master operational status.

## 9. Property Isolation

Command va query khong nhan client PropertyId. Get by ID/number, search, arrivals, departures va in-house deu query theo Property trong context. Cross-property tra NotFound, khong lo thong tin.

## 10. Stay Number

`SqlHotelStayNumberGenerator` dung `NEXT VALUE FOR [fo].[TrinhTuSoLuuTru]`, format `STY-YYYYMM-XXXXXX`. Sequence la global, unique toan he thong, concurrent-safe; khong dung Guid substring, MAX+1 hay COUNT+1.

## 11. RowVersion

`HotelStay.RowVersion` map EF `IsRowVersion`. DTO tra token va repository co `ApplyExpectedRowVersion` de san sang cho mutation sprint sau. Khong co public setter token.

## 12. Repository

Da them `IHotelStayRepository` va `EfHotelStayRepository`: add/history, get by ID/number, search, current in-house, expected arrivals/departures, duplicate active reservation check va concurrency original value.

## 13. Commands

Da active duy nhat `PrepareStayFromReservationCommand`. Khong co CheckInStayCommand, CheckOut command, Folio command hoac UI mutation.

## 14. Queries

Da them GetStayById, GetStayByNumber, SearchStays, GetCurrentInHouseStays, GetTodayExpectedArrivals va GetTodayExpectedDepartures. Business date den tu execution context, khong dung `DateTime.Now` trong handler.

## 15. DTOs

`HotelStayDto` tra stay number, reservation snapshot/reference, guest name, room/type snapshot, schedule/actual timestamps, counts, status, source va RowVersion; khong tra notes hay phone.

## 16. Error Codes

Da dung: `STAY_PERMISSION_DENIED`, `STAY_NOT_FOUND`, `STAY_RESERVATION_NOT_FOUND`, `STAY_RESERVATION_NOT_ELIGIBLE`, `STAY_ALREADY_EXISTS_FOR_RESERVATION`, `STAY_ROOM_REQUIRED`, `STAY_ROOM_NOT_FOUND`, `STAY_ROOM_NOT_ASSIGNABLE`, `STAY_ROOM_TYPE_MISMATCH`, `STAY_GUEST_REQUIRED`, `STAY_INVALID_DATE_RANGE`, `STAY_INVALID_GUEST_COUNT`, `STAY_CONCURRENCY_CONFLICT`.

## 17. Transaction

Prepare theo thu tu: load/validate Reservation, duplicate, room/type, generate number, create aggregate, append history, stage event/audit, mot `SaveChanges`, commit, read DTO. Failure truoc commit khong stage success audit/outbox hay persist Stay/history.

## 18. History

`HotelStayHistory` luu `StayPrepared`, PropertyId, status transition, actor, UTC time, correlation va safe metadata. Khong luu raw command JSON, phone, notes hoac PII.

## 19. Audit

Audit `StayPrepared` luu safe metadata: StayId, PropertyId, StayNumber, ReservationId, actor, correlation va result. Khong luu RowVersion raw, guest phone, note, SQL exception hay stack trace.

## 20. Outbox

Domain event `HotelStayPrepared` co payload safe: StayId, PropertyId, StayNumber, ReservationId, RoomId, scheduled dates, occurred time. Event duoc stage vao canonical outbox trong cung persistence transaction; khong publish truc tiep.

## 21. Permissions

Da them catalog idempotent: `FrontOffice.Stay.View`, `Prepare`, `CheckIn`, `CheckOut`, `Cancel`, `Update`. Sprint nay chi handler `View` va `Prepare` duoc dung. Khong hardcode Administrator allow-all.

## 22. EF Mapping

Da map tables `fo.LuuTruKhachSan` va `fo.LichSuLuuTruKhachSan`, FK Reservation/HotelRoom/HotelRoomType restrictive, string lengths, enum conversion, RowVersion va indexes can thiet.

## 23. Migration

Migration moi: `20260718092912_AddFrontOfficeHotelStayFoundation`.

## 24. Database Objects

Tao `fo.LuuTruKhachSan`, `fo.LichSuLuuTruKhachSan`, `fo.TrinhTuSoLuuTru`; unique StayNumber, filtered unique active ReservationId (`PendingArrival`, `InHouse`), Property/status/date/room indexes, updated timestamp index va history Stay/time index.

## 25. Domain Tests

`HotelStayFoundationTests` bao phu STAY-DOM-U-001..010: creation, dates, counts, guest/room required, initial status, invalid transition, immutable PropertyId va non-public RowVersion setter.

## 26. Application Tests

`HotelStayFoundationTests` bao phu STAY-APP-U-001..015: happy path, missing/wrong property Reservation, ineligible status, room states/type, duplicate, permission, no side effects khi failure, trusted context va BusinessDate.

## 27. Integration Tests

`STAY_I_001_to_017_localdb_migration_prepare_atomicity_scope_and_queries_are_durable` pass tren LocalDB: clean migration, sequence, prepare persistence, uniqueness/concurrency number, RowVersion, property scope, duplicate, history/outbox/audit, arrivals/departures/in-house va migration chain Reservation/HotelRoom.

## 28. Architecture Tests

Full unit suite pass. Stay domain khong reference EF/WPF; application khong reference AppDbContext; handler dung context trusted/IClock; no direct publishing, GUID substring numbering, MAX+1, COUNT+1, Restaurant/Banquet room hay ViewModel SQL.

## 29. DI Validation

`STAY_DI_U_001` pass voi full composition root: repository, SQL number generator, application service, prepare handler, query handler va persistence session deu resolve scoped.

## 30. Reception Integration

`Ho so luu tru` va `Tinh trang khach hien tai` da la read-only cards dung `SearchStaysQuery` va `GetCurrentInHouseStaysQuery`. Empty state no ro Check-in chua nam trong sprint. Arrivals/departures hien tai van giu Reservation source of truth.

## 31. Debug Runtime

Debug executable khoi dong va con song sau 8 giay smoke. Khong tu dong dang nhap hoac thao tac UI.

## 32. Release Runtime

Release executable khoi dong va con song sau 8 giay smoke. Khong tu dong dang nhap hoac thao tac UI.

## 33. Full Build

`dotnet build ASMILE.BO.sln --no-restore`: PASS, 0 warning, 0 error. Release build: PASS, 0 warning, 0 error. XAML encoding check: PASS.

## 34. Full Unit Results

PASS 312/312.

## 35. Full Integration Results

PASS 21/21 tren disposable LocalDB databases.

## 36. EF Results

`dbcontext info`: SQL Server AppDbContext. `migrations has-pending-model-changes`: no model changes since migration. Migration list co `AddFrontOfficeHotelStayFoundation`. Lenh list hien tat ca migration Pending tren `ASMILE_BO_DesignTime`, khong phai CSDL production va khong co migration nao duoc apply vao production.

## 37. Files Created

- `ASMILE.BO.Core/FrontOffice/Stay/HotelStay.cs`
- `ASMILE.BO.Application/FrontOffice/Stay/HotelStayContracts.cs`
- `ASMILE.BO.Application/FrontOffice/Stay/HotelStayHandlers.cs`
- `ASMILE.BO.Application/FrontOffice/Stay/HotelStayCqrsHandlers.cs`
- `ASMILE.BO.Application/FrontOffice/Stay/HotelStayPermissions.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/Stay/EfHotelStayRepository.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/Stay/SqlHotelStayNumberGenerator.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260718092912_AddFrontOfficeHotelStayFoundation.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260718092912_AddFrontOfficeHotelStayFoundation.Designer.cs`
- `ASMILE.BO.Tests.Unit/HotelStayFoundationTests.cs`
- `ASMILE.BO.Tests.Unit/HotelStayDependencyInjectionTests.cs`

## 38. Files Modified

AppDbContext, Infrastructure DI, permission seed/catalog, model snapshot, LocalDB integration test, Reception contracts/service/view models va Reception Hub regression test.

## 39. Package Changes

Khong them package.

## 40. Database Changes

Chi tao migration source. Chi disposable LocalDB test da migrate va duoc xoa sau test. Khong cham vao production database.

## 41. Deviations

Khong tao Check-in/Check-out application handler hay UI. Reception integration chi doc. Filtered unique ReservationId chi chan Stay active/prepared, cho phep lich su CheckedOut/Cancelled phuc vu reopen/reinstate sau nay.

## 42. Limitations

- CRM Guest existence boundary chua co contract chinh thuc; foundation dung GuestId/snapshot cua Reservation confirmed.
- Check-in UI va workflow chua implement.
- Folio chua implement.
- Housekeeping occupancy integration chua implement.

## 43. Recommended Next Sprint

Xac dinh CRM Guest reference contract, sau do thiet ke Check-in vertical slice voi expected RowVersion, room occupancy integration va guardrails truoc khi lam Folio/Checkout.

## 44. Final Status

HOAN THANH CO GIOI HAN
