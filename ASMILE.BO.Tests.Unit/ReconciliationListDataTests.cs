using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReconciliationListDataTests
{
    [Fact]
    public void BuildRealRows_maps_only_persisted_receivable_summaries()
    {
        var lastTransaction = new DateTime(2026, 9, 24);
        var summaries = new[]
        {
            new ReceivableSummaryDto
            {
                CustomerId = "C-001",
                CustomerName = "Khách thật",
                ClosingBalance = 1_250_000,
                LastTransactionDate = lastTransaction
            }
        };

        var rows = ReconciliationListDialog.BuildRealRows(summaries);

        rows.Should().ContainSingle();
        var row = rows[0];
        row.CustomerName.Should().Be("Khách thật");
        row.DebtA.Should().Be(1_250_000);
        row.DebtB.Should().BeNull();
        row.Diff.Should().BeNull();
        row.IsMatched.Should().BeFalse();
        row.Status.Should().Be("Chưa đối chiếu");
        row.Reviewer.Should().Be("Chưa có xác nhận khách hàng");
        row.ReconcileDate.Should().Be(lastTransaction);
        row.Period.Should().Be("Lũy kế đến 09/2026");
    }

    [Fact]
    public void BuildRealRows_selected_customer_filters_without_inventing_history()
    {
        var selected = new ReceivableSummaryDto
        {
            CustomerId = "C-002",
            CustomerName = "Được chọn"
        };
        var summaries = new[]
        {
            selected,
            new ReceivableSummaryDto { CustomerId = "C-003", CustomerName = "Không chọn" }
        };

        var rows = ReconciliationListDialog.BuildRealRows(summaries, selected);

        rows.Should().ContainSingle();
        rows[0].CustomerName.Should().Be("Được chọn");
        rows[0].Period.Should().Be("Theo dữ liệu hiện có");
    }
}
