# KẾT QUẢ PROCESS WORKER RUNTIME – FRONT OFFICE ASMILE-BO

## 1. Executive Summary

Đã bổ sung runtime xử lý timeout có claim SQL Server, lease heartbeat, owner + rowversion validation, retry hữu hạn, dead letter bền vững và manual review. Runtime không giữ process state, timeout queue hay lease cache lâu dài trong memory; mỗi iteration đọc/ghi SQL Server qua scope riêng.

**Trạng thái: HOÀN THÀNH CÓ GIỚI HẠN.** Các thành phần chính đã build và kiểm thử LocalDB, nhưng chưa có đủ 15 kịch bản integration độc lập theo acceptance, đặc biệt heartbeat/cancellation/graceful-shutdown end-to-end với hosted worker.

## 2. Worker Lifecycle

`Claim due timeout → renew lease trước khi làm việc → chạy timeout work → finalize signal/history/timeout trong transaction → next poll`.

Nếu lỗi: transient được đưa lại `Pending` với `NextRetryAtUtc`; permanent, business validation, unknown hoặc hết số lần thử đi đến dead letter và tạo manual action. Hosted worker dùng `PeriodicTimer`, không spin loop.

## 3. Heartbeat

`ProcessLeaseHeartbeat` chạy với cancellation token liên kết. Mỗi lần renew mở scope/repository mới, điều kiện SQL bắt buộc: timeout còn `Claimed`, đúng `ClaimedBy`, lease chưa hết hạn và (khi có) đúng `RowVersion`. Renew thất bại hủy token ngay, tăng metric `LeaseLost`, log warning và ngăn finalize/retry/dead-letter tiếp theo.

## 4. Lease Recovery

Claim SQL tiếp tục cho phép reclaim timeout có lease đã hết hạn. Hoàn tất, retry và dead-letter đều kiểm tra owner + lease expiry trong transaction trước khi mutate. Lease mất không thể tạo signal/history mới hoặc complete timeout.

## 5. Worker Restart

Worker identity là GUID mới theo process. Timeout claim không lưu trong memory; sau crash/dispose, lease hết hạn và worker khác có thể claim lại qua query SQL hiện có.

## 6. Retry

Chỉ `Transient` được retry; `BusinessValidation` không retry; `Permanent` dead-letter ngay; `Unknown` chuyển manual review/dead letter. Lịch retry bền vững trong `HenGioQuyTrinh` (`DueAtUtc`, `NextRetryAtUtc`, `AttemptCount`, `LastErrorCode`) với exponential bounded delay.

## 7. Dead Letter

Thêm bảng `fo_process.ThuDaChetQuyTrinh` qua migration `20260717143000_AddFrontOfficeProcessWorkerDeadLetters`. Bản ghi giữ `Reason`, `ErrorCode`, `RetryCount`, `OccurredAtUtc`, `ProcessId`, `SignalId`, `WorkerId`, `CorrelationId`, `BusinessDate`, timeout id; không có payload.

## 8. Manual Action

Khi dead-letter, runtime tạo một `ProcessTimeoutDeadLetter` manual action nếu chưa có action đang mở cho process. Action dùng permission `FrontOffice.Process.ManualReview`, role `FrontOfficeSupervisor`; foundation hiện hành quản lý Open/Assigned/Completed/Cancelled (tương ứng Pending/Claimed/Completed/Cancelled).

## 9. Worker Health

`GetWorkerHealthAsync` trả Health/Degraded dựa trên pending timeout, active claims và dead-letter count. Dead letter làm trạng thái Degraded để monitor ưu tiên xử lý.

## 10. Metrics

Contract `IProcessWorkerMetrics` có WorkerStarted, WorkerStopped, TimeoutClaimed, TimeoutCompleted, LeaseRenewed, LeaseLost, RetryScheduled, RetryExhausted, DeadLetter, ManualActionCreated, WorkerRestart, WorkerRecovery. DI mặc định là no-op để không phát sinh hạ tầng monitoring mới; có thể thay bằng adapter telemetry hiện hữu.

## 11. Integration Tests

- `WRK_001_to_WRK_009...`: LocalDB claim, rowversion renewal, wrong owner, persisted retry, retry exhausted, dead-letter, audit history và manual action.
- Các test LocalDB trước đó tiếp tục phủ atomic signal, duplicate/no-op, rollback, ack recovery và concurrent timeout claim.
- Full integration: **17/17 passed**.

## 12. Build

- `dotnet build .\ASMILE.BO.sln --no-restore`: passed, 0 warnings, 0 errors.
- Unit tests: **230/230 passed**.
- Integration tests: **17/17 passed**.
- Migration mới được áp dụng trong toàn bộ LocalDB integration suite; không chạm database production.

## 13. Files Modified

- `ASMILE.BO.Application/FrontOffice/ProcessManager/ProcessWorkerRuntime.cs`
- `ASMILE.BO.Application/FrontOffice/ProcessManager/ProcessManagerContracts.cs`
- `ASMILE.BO.Core/FrontOffice/ProcessManager/ProcessPersistence.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/ProcessManager/EfProcessManagerStore.cs`
- `ASMILE.BO.Infrastructure/FrontOffice/ProcessManager/ProcessTimeoutDispatcher.cs`
- `ASMILE.BO.Infrastructure/Persistence/AppDbContext.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/20260717143000_AddFrontOfficeProcessWorkerDeadLetters.cs`
- `ASMILE.BO.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `ASMILE.BO.Infrastructure/DependencyInjection.cs`
- `ASMILE.BO.Tests.Integration/FrontOfficeSqlServerIntegrationTests.cs`

## 14. Known Limitations

1. Timeout work executor hiện là durable no-op hook; typed process definition chưa được registry/dispatch vào `ProcessSignalHandler`, vì yêu cầu này không phát triển workflow nghiệp vụ.
2. Chưa có integration test độc lập WRK-010 đến WRK-015 cho heartbeat timing, cancellation, graceful shutdown, hosted-worker restart/recovery và no-busy-loop; các thuộc tính nền tảng đã có trong runtime nhưng acceptance yêu cầu kiểm thử riêng.
3. `dotnet ef migrations has-pending-model-changes` vẫn báo model-pending do quan hệ query-filter RestaurantQuote/QuoteChangeRequest đã tồn tại ngoài phạm vi worker; không phải lỗi migration worker và migration mới đã chạy qua LocalDB.
4. Adapter metrics/health vào telemetry dashboard chưa được cài; contract có sẵn để tích hợp mà không đổi runtime.

## 15. Final Status

**HOÀN THÀNH CÓ GIỚI HẠN**
