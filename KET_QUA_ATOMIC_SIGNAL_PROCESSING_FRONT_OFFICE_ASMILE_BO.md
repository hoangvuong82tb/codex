# Kết quả Atomic Signal Processing — Front Office Asmile-BO

Ngày thực hiện: 2026-07-17. Không tạo workflow nghiệp vụ, UI, Reservation hoặc migration mới; không chạy production migration/connection và không commit/push/PR.

## 1. Executive Summary

Đã tạo `ProcessSignalHandler` làm boundary duy nhất cho process-state transition qua pipeline atomic. Handler reserve signal, load/validate/transition process, append history, tạo timeout/manual action khi transition yêu cầu, persist outbox, đánh dấu signal processed và gọi đúng một `SaveChangesAsync` trong một SQL transaction. Kết quả **HOÀN THÀNH CÓ GIỚI HẠN** vì chỉ một phần crash matrix REC-001..009 có LocalDB evidence.

## 2. Transaction Flow

`Signal received → reserve → load process → typed transition → history → timeout/manual action (nếu requested) → outbox → signal processed → SaveChanges một lần → commit → logical ack`.

Signal status `Processed` được staged trong cùng commit. Đây là durable acknowledgement; checkpoint sau commit mô phỏng việc caller chết trước khi nhận response/ack, retry cùng hash trở thành no-op mà không cần Save lần hai.

## 3. Atomic Boundary

Handler mở `IDbContextTransaction`; không gọi SaveChanges trước stages. Process, signal, history, timeout/manual action và outbox được EF track trong cùng DbContext, rồi SaveChanges một lần và commit một lần. Exception trước commit rollback toàn bộ. Outbox luôn pending sau commit và không publish trong transaction.

## 4. Crash Matrix

Checkpoint test-only hỗ trợ after reserve/load/transition, before history/outbox/save, after save before commit và after commit before ack. LocalDB đã kiểm chứng rollback ở `BeforeSaveChanges` và recovery ở `AfterCommitBeforeAck`. REC-001..003, REC-005..009 chưa có case test riêng hoàn chỉnh.

## 5. Recovery Matrix

Crash trước commit: database không có signal/history/outbox mới và process giữ state cũ. Crash sau commit trước ack: signal đã Processed; retry same hash nhận durable duplicate no-op; không có outbox/history thứ hai.

## 6. Duplicate Signal

Handler kiểm tra `(ProcessId, SignalId)` trước mutation. Same hash trả `PM_SIGNAL_DUPLICATE_NOOP`; hash khác trả `PM_SIGNAL_PAYLOAD_CONFLICT`. Integration test xác nhận không duplicate state/history/outbox.

## 7. Outbox

Handler tạo `FrontOffice.Process.Transitioned` outbox message chỉ sau transition và trước SaveChanges. Message không chứa PII/raw payload; chỉ process/state/step/signal/timestamp. Publish không nằm trong handler transaction.

## 8. Tests

Thêm LocalDB TX-001/003/004/005/006/007 coverage: atomic commit, rollback no partial state, duplicate same/different hash, outbox exactly once, crash-before-commit và ack recovery. Existing retry/manual/compensation primitives vẫn chạy trong unit suite.

## 9. Build

`dotnet build .\ASMILE.BO.sln --no-restore` pass, 0 warning/error; XAML encoding check pass.

## 10. Integration

16/16 integration tests pass. Process signal tests chạy SQL Server LocalDB thật, gồm transaction, rollback, duplicate, ack recovery và prior two-DbContext timeout claim race.

## 11. Files Modified

Tạo `ProcessSignalHandler.cs`, Core `AssemblyInfo.cs`, cập nhật `ProcessFaultInjection.cs`, `ProcessTimeoutDispatcher.cs`, DI và `FrontOfficeSqlServerIntegrationTests.cs`; `ProcessInstance.Transition` đổi sang `internal` để process state không bị mutate trực tiếp ngoài Application transition engine/handler path.

## 12. Known Limitations

Chưa có LocalDB test riêng cho toàn bộ REC-001..009, compensation/manual-action fault case, timeout publish lifecycle, retry/compensation creation do transition request, và một registry production cho typed process definitions. Vì vậy không tuyên bố full crash matrix pass.

## 13. Final Status

**HOÀN THÀNH CÓ GIỚI HẠN.** Atomic boundary, no-partial rollback, duplicate no-op, ack recovery và outbox-before-commit đã được chứng minh; crash matrix đầy đủ chưa hoàn tất.
