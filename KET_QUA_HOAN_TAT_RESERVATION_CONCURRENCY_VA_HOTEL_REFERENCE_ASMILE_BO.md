# KET QUA HOAN TAT RESERVATION CONCURRENCY VA HOTEL REFERENCE ASMILE-BO

## 1. Executive Summary

Da dua Reservation optimistic concurrency va Hotel Master Data reference validation vao vertical slice. Stale user mutation khong con bi reload/retry; no rollback va tra `RESERVATION_CONCURRENCY_CONFLICT`.

Trang thai: **CHUA HOAN THANH** theo acceptance gate day du, vi chua co bo test matrix rieng bao phu tat ca ma RES-CON-U, RES-REF-U, RES-PROP-U va tat ca stale action variants. Cac correctness gate trung tam da co LocalDB evidence va regression suite pass.

## 2. Scope

Chi sua Reservation concurrency/reference boundary, persistence behavior, tests va report. Khong lam UI, RatePlan, pricing, stay workflow, Property aggregate hay migration moi.

## 3. Existing Code Review

Reservation da co RowVersion mapping/DTO, nhung mutation contracts chua co expected token, handlers dung `DateTime.UtcNow`, va service chua dung Hotel reference boundary. Repository projection cung duoc sua de filter entity truoc khi project DTO, tranh LINQ translation failure.

## 4. SaveChanges Retry Analysis

`AppDbContext.SaveChangesAsync` truoc day reload va retry `DbUpdateConcurrencyException` toi 2 lan. Dieu nay khong phu hop user-driven optimistic concurrency. Da bo reload/retry: exception duoc clear tracking va wrap thanh `AppConcurrencyException`; persistence session rollback transaction, clear staged events/tracker va tra concurrency result.

## 5. RowVersion Design

Reservation van dung SQL Server `rowversion`, private setter, DTO `byte[]? RowVersion`. Update dat expected token vao EF `OriginalValue`, khong set client token thanh current value va khong last-write-wins.

## 6. Command Contract Changes

Chin mutation commands da nhan `byte[] ExpectedRowVersion`: update details, change stay, change guest count, assign/unassign, tentative, confirm, cancel, no-show. Create khong can token. Update details co them RoomTypeId nullable de validation change room type.

## 7. DTO Token Behavior

Get by id, get by number, search va mutation read-after-write deu tra RowVersion. Mutation thanh cong tra token moi tu SQL Server.

## 8. Repository Concurrency

`IReservationRepository.ApplyExpectedRowVersion` va `EfReservationRepository` dat `Entry(...).Property(RowVersion).OriginalValue`. Entity luon duoc load property-scoped va tracked boi cung DbContext.

## 9. Error Mapping

Missing/empty token: `RESERVATION_ROWVERSION_REQUIRED`. Token do dai khac SQL Server rowversion 8 bytes: `RESERVATION_ROWVERSION_INVALID`. Save conflict: `RESERVATION_CONCURRENCY_CONFLICT`.

## 10. Property Isolation

Service chi chap nhan PropertyId trung trusted execution context va repository query theo PropertyId. Cross-property reservation tra NotFound/property mismatch; room/type reference reader tra null cho Property khac.

## 11. HotelRoomType Validation

Create, update details va confirm validate RoomType qua `IHotelReservationReferenceReader`: phai ton tai trong Property hien tai va active. Cross-property duoc an nhu not found.

## 12. HotelRoom Validation

Assign load Room reference property-scoped truoc conflict checker. Missing/wrong-property room tra `RESERVATION_ROOM_NOT_FOUND`; room inactive/non-operational tra `RESERVATION_ROOM_NOT_ASSIGNABLE`.

## 13. Room Assignability

Reference safe facts gom Room active, operational status va RoomType active. Chi active + Available + active RoomType moi assignable.

## 14. RoomType Compatibility

Assign yeu cau Room.RoomTypeId trung Reservation.RoomTypeId. Update RoomType khi da assigned room yeu cau current room match type moi; neu khong tra `RESERVATION_ASSIGNED_ROOM_TYPE_MISMATCH`.

## 15. Room Conflict

Overlap checker chi chay sau khi Room da hop le. SQL overlap giu nguyen: existing arrival < requested departure va existing departure > requested arrival; Tentative/Confirmed moi block.

## 16. Guest Limitation

Khong tao Guest validator moi. Domain rule Guest required khi confirm duoc giu nguyen; chua co CRM guest reference boundary chinh thuc.

## 17. RatePlan Limitation

RatePlan nullable giu nguyen va khong duoc validate, dung theo scope.

## 18. Transaction Atomicity

Load -> validate token -> apply OriginalValue -> validate references/conflict -> mutate -> history -> stage audit/events/outbox -> mot SaveChanges -> mot commit. Concurrency failure rollback va clear tracking.

## 19. History Behavior

Thanh cong ghi action dung: DetailsUpdated, StayChanged, GuestCountChanged, RoomAssigned, RoomUnassigned, MarkedTentative, Confirmed, Cancelled, MarkedNoShow. Stale/reference failure khong append history.

## 20. Audit Behavior

Audit stage trong persistence session voi ReservationId, PropertyId, number, action, from/to status, actor va correlation. Khong log phone, email, notes, raw command, raw token hay SQL exception.

## 21. Outbox Behavior

Domain events duoc stage cung transaction. Stale/reference failure khong tao outbox; khong co direct publish.

## 22. Unit Test Results

`dotnet test ASMILE.BO.Tests.Unit --no-build`: **230 passed, 0 failed**.

## 23. Concurrency Integration Tests

LocalDB test `RES_CON_and_REF_localdb_stale_mutation_is_atomic_and_hotel_references_are_property_scoped` xac nhan create token, winner update, stale update conflict, state winner duoc giu, stale khong them history/outbox/audit success.

## 24. Property Isolation Tests

Hotel master integration xac nhan Property A/B isolation. Reservation acceptance test xac nhan Room Property B bi an nhu not found khi assign tu Property A.

## 25. Hotel Reference Tests

LocalDB xac nhan valid active Available room assign thanh cong; wrong-property room bi an; OutOfOrder room bi reject. Room/RoomType mismatch va inactive RoomType co implementation validation, nhung chua co test case rieng day du.

## 26. DI Validation

DI da co scoped `IHotelReservationReferenceReader`, `EfReservationRepository`, `ReservationApplicationService`, number generator, conflict checker va 10 command/4 query handlers. Khong co singleton giu DbContext.

## 27. Architecture Tests

Build va existing unit suite pass. Khong them architecture-test harness rieng; code giu Application khong tham chieu DbContext, Domain khong tham chieu Infrastructure/WPF/Restaurant/Banquet, va handlers khong dung `DateTime.Now` hay retry concurrency.

## 28. Migration Regression

Khong tao migration moi. Clean LocalDB integration replay chain qua Reservation va Hotel Master Data migrations. `dotnet ef migrations has-pending-model-changes` bao `No changes have been made to the model since the last migration.`

## 29. EF Tooling Results

`dotnet ef dbcontext info` pass voi AppDbContext/SqlServer/LocalDB. `dotnet ef migrations list` co `20260718011133_AddFrontOfficeReservation` va `20260718014449_AddFrontOfficeHotelRoomMasterData`; design database dang pending, khong phai production database.

## 30. Build Results

`dotnet build ASMILE.BO.sln --no-restore`: **0 warning, 0 error**; XAML encoding check pass.

## 31. Files Created

- `KET_QUA_HOAN_TAT_RESERVATION_CONCURRENCY_VA_HOTEL_REFERENCE_ASMILE_BO.md`

## 32. Files Modified

- `ASMILE.BO.Application/FrontOffice/Reservation/ReservationContracts.cs`
- `ASMILE.BO.Application/FrontOffice/Reservation/ReservationCqrsHandlers.cs`
- `ASMILE.BO.Application/FrontOffice/Reservation/ReservationHandlers.cs`
- `ASMILE.BO.Application/FrontOffice/HotelMasterData/HotelRoomMasterDataContracts.cs`
- `ASMILE.BO.Core/FrontOffice/Reservation/Reservation.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/Reservation/EfReservationRepository.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/HotelMasterData/EfHotelRoomMasterDataRepository.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/Foundation/EfFrontOfficePersistenceSession.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs`

## 33. Package Changes

Khong them package moi.

## 34. Deviations

Khong thay doi Property schema va khong tao composite RoomType FK. Same-property duoc enforce boi trusted context va property-scoped reference lookup.

## 35. Existing Issues

EF tooling van canh bao global filter RestaurantQuote/QuoteAuditLog va QuoteChangeRequest/Detail. Canh bao nay co san, ngoai scope Reservation va khong lam fail build/test.

## 36. Known Limitations

Chua co CRM Guest reference boundary, RatePlan model/validation, permission policy wiring, va bo test acceptance chi tiet theo tung ma yeu cau. Day la ly do khong tuyen bo HOAN THANH.

## 37. Recommended Next Sprint

Bo sung unit tests cho token/reference/property va LocalDB tests rieng cho stale Confirm/Cancel/Assign, RoomType mismatch/inactive, adjacent/cancelled/tentative/confirmed conflict cases. Sau do wiring CRM Guest boundary va permission policy truoc UI.

## 38. Final Status

**CHUA HOAN THANH** theo acceptance gate day du. Core RowVersion end-to-end, stale atomicity, Property isolation va Hotel reference validation da duoc implement va co evidence; test matrix day du van con thieu.
