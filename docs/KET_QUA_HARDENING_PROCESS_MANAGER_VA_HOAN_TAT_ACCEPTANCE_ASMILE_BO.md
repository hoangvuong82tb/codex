# Kết quả hardening Process Manager và hoàn tất Acceptance

Ngày thực hiện: 2026-07-17. Không chạy migration production, không dùng secret/production connection, không commit/push/PR và không triển khai workflow nghiệp vụ Front Office.

## 1. Executive Summary

Đã harden đáng kể Foundation: typed definition/transition, retry/backoff, lease renewal ownership, retry/dead-letter persistence, compensation dispatcher foundation, manual-action dual-control foundation và mở rộng test LocalDB. Kết quả là **HOÀN THÀNH CÓ GIỚI HẠN** vì các test crash/race/worker acceptance catalog chưa đủ để mở Reservation Vertical Slice.

## 2. Baseline

Baseline pass: build 0 lỗi; 225 unit và 13 integration tests. Migration chain đã sạch từ Prompt 11.

## 3. Process Manager Gaps Found

Thiếu typed process definition, transition engine, retry policy, lease renewal, dual-control metadata, compensation dispatcher và test coverage acceptance chuyên biệt.

## 4. State Machine Definition

Thêm `IProcessDefinition<TState,TStep,TSignal>`, `ProcessDefinition`, typed transition/guard, timeout/retry/compensation/manual-action policy. Definition nằm trong code, không lưu workflow business vào DB.

## 5. Transition Engine

`ProcessTransitionEngine` kiểm tra process type/version, persisted-state parse, terminal state, valid transition và guard trước khi mutate. Kết quả trả về timeout/escalation/manual-permission request; transition invalid không đổi ProcessInstance.

## 6. Signal Ordering

Signal duplicate với hash giống/khác được persistence contract phân loại. Engine reject signal unexpected/out-of-order và no-op signal terminal. Chính sách hold/replay/reconciliation được biểu diễn trong typed policy nhưng chưa có dispatcher xử lý đầy đủ mọi policy.

## 7. Retry Engine

Thêm `ProcessRetryPolicy` và `ProcessRetryEngine`: max attempts, exponential backoff, max delay, injected deterministic jitter value, error-code allowlist, retry schedule hoặc escalation. Retry schedule/error/dead-letter metadata được persist trên timeout.

## 8. Lease Renewal

`RenewTimeoutLeaseAsync` dùng SQL Server conditional update theo timeout ID, owner, status và lease chưa hết hạn. Owner sai hoặc lease hết hạn không thể renew.

## 9. Poison Timeout and Dead Letter

`ProcessTimeout.DeadLetter` và retry engine hỗ trợ DeadLetter escalation, persisted `LastErrorCode`/`DeadLetteredAtUtc`. Chưa có worker policy-to-manual-action/reconciliation executor hoàn chỉnh.

## 10. Compensation Executor

Thêm `ICompensationHandler`, `CompensationExecutionResult`, `CompensationDispatcher`; dispatcher chỉ gọi handler đã đăng ký, không chứa business compensation. State requested/running/completed/manual-review được giữ trên compensation record.

## 11. Manual Action Authorization

Manual action có creator/approver metadata, claim theo actor/role, permission input, reason required và dual-control: creator không thể approve; completion yêu cầu approval khi flag bật. Chưa tích hợp authorization service/audit persistence thực tế.

## 12. Crash Recovery

Persistence của signal/timeout/history và lease expiry hỗ trợ resume. Chưa có test REC-001..007 đủ từng điểm crash transaction/publisher/compensation.

## 13. Dual-worker Concurrency

LocalDB test xác minh second worker không claim active lease, owner renew thành công, owner sai bị chặn và worker khác reclaim được sau expiry. Chưa có two-DbContext parallel race test độc lập.

## 14. Stuck Process Detection

Monitor query `GetStuckProcessesAsync` có sẵn. Chưa có worker classification/escalation và health summary đầy đủ.

## 15. Process Monitor Queries

Monitor foundation vẫn có process by id, active, manual/reconciliation, due timeout, stuck và timeline. Chưa có pagination/sort whitelist và các queues dead-letter/retry-exhausted riêng.

## 16. Logging and Metrics

Timeout dispatcher log structured count/worker/timeout lease lost. Chưa có metric sink/counters đầy đủ theo catalog và không log payload.

## 17. EF Mapping Changes

Model bổ sung runtime fields cho timeout retry/dead-letter và manual-action creator/approver. Mapping vẫn scoped `fo_process`; không có thay đổi Reservation/Stay/Folio/Payment.

## 18. Migration Changes

Tạo `20260717124748_HardenFrontOfficeProcessManagerRuntime` kèm designer/snapshot. Migration chỉ `ALTER` các table `fo_process` để thêm metadata hardening.

## 19. Unit Test Results

230/230 pass. Có tests typed valid/invalid transition, terminal guard, retry bounds/dead-letter, dual-control, architecture no public state setters và compensation dispatcher test-only handler.

## 20. SQL Server Integration Test Results

13/13 pass. Test LocalDB Process Manager xác minh migration, durable process/timeout/signal, duplicate signal, atomic claim baseline, renewal ownership và reclaim expiry.

## 21. Worker Test Results

Dispatcher compile và repository lease paths được kiểm chứng integration. Chưa có acceptance worker tests poison/retry/dead-letter/restart/cancellation riêng.

## 22. Architecture Test Results

Domain ProcessInstance không public state setter và Core không reference EF/WPF; existing Foundation architecture tests pass. Chưa có catalog architecture test đầy đủ cho presentation/repository/external-call rules.

## 23. Build Results

`dotnet build .\ASMILE.BO.sln --no-restore` pass, 0 warning/error; XAML encoding check pass.

## 24. EF Tooling Results

`dbcontext info` SQL Server pass. Migration list có `20260717124748_HardenFrontOfficeProcessManagerRuntime`. Idempotent SQL được sinh/review và giới hạn `ALTER TABLE [fo_process]` cho retry/dead-letter/dual-control metadata.

## 25. Security Review

Không thêm production connection, secret hay raw payload/PII log. Dual-control rule không cho creator approve. Authorization/Audit adapter thật còn deferred.

## 26. Files Created

`ProcessDefinition.cs`, `ProcessTransitionEngine.cs`, `ProcessRetryEngine.cs`, `ProcessOperations.cs`, `CompensationDispatcher.cs`, hardening migration/designer và báo cáo này.

## 27. Files Modified

`ProcessPersistence.cs`, `ProcessManagerContracts.cs`, `EfProcessManagerStore.cs`, `ProcessTimeoutDispatcher.cs`, Process Manager unit/integration tests, DI/model snapshot do EF cập nhật.

## 28. Package Changes

Không thay đổi package.

## 29. Migration Snapshot Changes

`AppDbContextModelSnapshot.cs` được EF cập nhật cho cột timeout/manual action hardening.

## 30. Deviations

Jitter là input deterministic tại engine để test; runtime random provider/metrics abstraction chưa thêm nhằm tránh framework không có consumer. State persistence giữ string vì schema Prompt 11 đã deploy local foundation; typed mapping kiểm soát boundary mới.

## 31. Existing Issues

Hai EF query-filter warning RestaurantQuote/QuoteAuditLog và QuoteChangeRequest/Detail vẫn tồn tại, ngoài scope Process Manager.

## 32. Known Limitations

Chưa có full out-of-order policy dispatcher, crash matrix, SQL two-DbContext parallel races, rowversion conflict mapping, worker poison/dead-letter execution, lease renewal heartbeat, manual authorization/audit adapter thật, monitor health summary/metrics.

## 33. Deferred Work

Hoàn tất PM-U-004..015, PRC-DB-005..020 và PM-W-001..008 theo Prompt 12; đặc biệt recovery transactional/outbox, dual-context concurrency và authorization audit.

## 34. Recommended Next Sprint

Không mở Reservation ngay. Thực hiện một hardening/test sprint tiếp theo tập trung atomic signal processing transaction, race harness hai DbContext, crash tests và worker escalation trước khi re-evaluate gate.

## 35. Git Diff Summary

Workspace không có `.git`, nên không thể lấy git diff/stat. Không có commit, push hay PR.

## 36. Final Status

**HOÀN THÀNH CÓ GIỚI HẠN.** Foundation đã tiến bộ nhưng chưa chứng minh được toàn bộ acceptance gate bắt buộc, đặc biệt crash/recovery và two-DbContext race.

CHƯA SẴN SÀNG TRIỂN KHAI RESERVATION VERTICAL SLICE.
