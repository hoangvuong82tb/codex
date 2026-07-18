# BREAKFAST CAFE SALES

## Mục tiêu module
Module `breakfast-cafe-sales` phục vụ bán nhanh đồ ăn sáng, cà phê, đồ uống và combo cho mô hình nhà hàng/khách sạn/café nhỏ.

## Luồng bán hàng phase 1
1. Chọn món từ menu theo danh mục.
2. Tạo order và thêm/sửa/xóa dòng món.
3. Áp dụng giảm giá order.
4. Chọn phương thức thanh toán: `Cash`, `BankTransfer`, `CardPOS`, `RoomCharge`, `CustomerDebt`.
5. Thanh toán order và chốt trạng thái.
6. Theo dõi lịch sử order trong ngày và summary.

## Tác động kế toán
- Luồng thanh toán tạo bút toán qua `IJournalService` (draft + post), không ghi tay trực tiếp vào bảng journal.
- Resolve tài khoản qua `IPostingRuleService` + `AccountRoleMapping`.
- `JournalService.PostDraftAsync` giữ nguyên workflow hiện hữu: tự sync debt/tax nếu service đã được wiring.

## Tác động tiền/quỹ-ngân hàng
- `Cash` đi qua `ICashFundService` (`CreateReceiptAsync` + `PostTransactionAsync`).
- `BankTransfer`/`CardPOS` đi qua `IBankingService` (`CreateReceiptAsync` + `PostTransactionAsync`).

## Tác động inventory/costing
- Đã thêm `IBreakfastCafeCostingService` skeleton.
- Chưa bật tự động xuất kho theo recipe trong phase 1 khi chưa đủ map recipe-kho-item ổn định.

## Giới hạn phase 1
- Không tích hợp POS printer/phần cứng.
- Không tích hợp QR payment thật.
- Không tích hợp delivery.
- Không triển khai workflow phòng/bàn phức tạp.

## Hướng mở rộng
- Bổ sung workflow room charge đầy đủ theo entity phòng/guest.
- Bổ sung recipe consumption sang `IInventoryAppService` theo policy xuất kho chuẩn.
- Bổ sung ca bán và đối soát cuối ca chi tiết.
