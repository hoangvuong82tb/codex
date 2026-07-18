using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class TableAllocationEngine : ITableAllocationEngine
{
    private const int SoftExtraSeatPerTable = 2;

    public IReadOnlyList<TableAllocationResult> Allocate(int guestCount, int tableStandard)
    {
        if (guestCount <= 0)
        {
            throw new InvalidOperationException("Tong so khach phai lon hon 0.");
        }

        var standard = tableStandard <= 0 ? 10 : tableStandard;
        var allocations = new List<TableAllocationResult>
        {
            new()
            {
                TableNo = 0,
                DisplayName = "Ban Tong",
                GuestCount = guestCount,
                SortOrder = 0,
                IsVirtual = true,
                Note = "Tong hop thanh toan"
            }
        };

        var realTableGuests = BuildSuggestedAllocations(guestCount, standard);

        for (var index = 0; index < realTableGuests.Count; index++)
        {
            var tableNo = index + 1;
            allocations.Add(new TableAllocationResult
            {
                TableNo = tableNo,
                DisplayName = $"Ban {tableNo:00}",
                GuestCount = realTableGuests[index],
                SortOrder = tableNo,
                IsVirtual = false
            });
        }

        return allocations;
    }

    private static List<int> BuildSuggestedAllocations(int guestCount, int standard)
    {
        var fullTables = guestCount / standard;
        var remainder = guestCount % standard;

        if (fullTables <= 0)
        {
            return [guestCount];
        }

        var realTableGuests = Enumerable.Repeat(standard, fullTables).ToList();
        if (remainder == 0)
        {
            return realTableGuests;
        }

        var softCapacity = SoftExtraSeatPerTable * fullTables;
        var smallRemainderThreshold = Math.Min(softCapacity, Math.Max(2, standard / 2));
        if (remainder <= smallRemainderThreshold)
        {
            var remaining = remainder;
            var tableIndex = 0;
            while (remaining > 0)
            {
                if (realTableGuests[tableIndex] < standard + SoftExtraSeatPerTable)
                {
                    realTableGuests[tableIndex] += 1;
                    remaining--;
                }

                tableIndex++;
                if (tableIndex >= realTableGuests.Count)
                {
                    tableIndex = 0;
                }
            }

            return realTableGuests
                .OrderByDescending(x => x)
                .ToList();
        }

        realTableGuests.Add(remainder);
        return realTableGuests;
    }
}

public sealed class MenuCalculationEngine : IMenuCalculationEngine
{
    public decimal Calculate(string? pricingRule, int guestCount, int realTableCount, decimal defaultQuantity, decimal? manualQuantity = null)
    {
        var quantity = defaultQuantity <= 0 ? 1m : defaultQuantity;
        var rule = pricingRule?.Trim();

        if (string.Equals(rule, "PerPerson", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(1, guestCount) * quantity;
        }

        if (string.Equals(rule, "PerTable", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(1, realTableCount) * quantity;
        }

        if (string.Equals(rule, "PerRoom", StringComparison.OrdinalIgnoreCase))
        {
            return quantity;
        }

        var manual = manualQuantity.GetValueOrDefault(quantity);
        return manual <= 0 ? quantity : manual;
    }
}

public sealed class RoomOrderMenuPlanningEngine : IRoomOrderMenuPlanningEngine
{
    public IReadOnlyList<RoomOrderMenuPlanLine> BuildPlan(RoomOrderMenuPlanningRequest request)
    {
        var allServingTables = (request.Tables ?? Array.Empty<RoomOrderMenuTableInput>())
            .Where(x => x.TableId != Guid.Empty
                        && x.GuestCount > 0
                        && (string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(x.Status, "Đang phục vụ", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (allServingTables.Count == 0)
        {
            return Array.Empty<RoomOrderMenuPlanLine>();
        }

        var standard = Math.Max(1, request.TableStandard);
        var targetTables = FilterTargetTables(allServingTables, request.ApplyScope, standard, request.SelectedTableId);
        if (targetTables.Count == 0)
        {
            return Array.Empty<RoomOrderMenuPlanLine>();
        }

        var baseQuantity = request.BaseQuantity <= 0 ? 1m : request.BaseQuantity;
        var extraUnitQuantity = request.ExtraUnitQuantity <= 0 ? 1m : request.ExtraUnitQuantity;
        var manualQuantity = request.ManualQuantity <= 0 ? baseQuantity : request.ManualQuantity;

        return targetTables
            .Select(table =>
            {
                var extraGuests = Math.Max(0, table.GuestCount - standard);
                return BuildPlanLine(table, request.ServingRule, baseQuantity, extraUnitQuantity, manualQuantity, extraGuests);
            })
            .ToList();
    }

    private static List<RoomOrderMenuTableInput> FilterTargetTables(
        IReadOnlyList<RoomOrderMenuTableInput> allServingTables,
        string? applyScope,
        int standard,
        Guid? selectedTableId)
    {
        if (string.Equals(applyScope, "SelectedTable", StringComparison.OrdinalIgnoreCase))
        {
            return selectedTableId.HasValue
                ? allServingTables.Where(x => x.TableId == selectedTableId.Value).ToList()
                : [];
        }

        if (string.Equals(applyScope, "StandardOnly", StringComparison.OrdinalIgnoreCase))
        {
            return allServingTables.Where(x => x.GuestCount == standard).ToList();
        }

        if (string.Equals(applyScope, "OddOnly", StringComparison.OrdinalIgnoreCase))
        {
            return allServingTables.Where(x => x.GuestCount != standard).ToList();
        }

        return allServingTables.ToList();
    }

    private static RoomOrderMenuPlanLine BuildPlanLine(
        RoomOrderMenuTableInput table,
        string? servingRule,
        decimal baseQuantity,
        decimal extraUnitQuantity,
        decimal manualQuantity,
        int extraGuests)
    {
        if (string.Equals(servingRule, "PerTable", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOrderMenuPlanLine
            {
                TableId = table.TableId,
                TableName = table.TableName,
                GuestCount = table.GuestCount,
                SaveQuantity = baseQuantity,
                DisplayQuantity = baseQuantity,
                DisplayRuleText = $"{baseQuantity:N0} phần / bàn"
            };
        }

        if (string.Equals(servingRule, "PerTableWithExtraUnit", StringComparison.OrdinalIgnoreCase))
        {
            var extraQuantity = extraGuests * extraUnitQuantity;
            var note = extraQuantity > 0
                ? $"Phần bù lẻ: +{extraQuantity:N0} đơn vị cho {extraGuests} khách vượt chuẩn."
                : null;
            return new RoomOrderMenuPlanLine
            {
                TableId = table.TableId,
                TableName = table.TableName,
                GuestCount = table.GuestCount,
                SaveQuantity = baseQuantity,
                DisplayQuantity = baseQuantity + extraQuantity,
                ExtraQuantity = extraQuantity,
                DisplayRuleText = extraQuantity > 0
                    ? $"{baseQuantity:N0} phần + bù {extraQuantity:N0}"
                    : $"{baseQuantity:N0} phần chuẩn",
                KitchenNote = note
            };
        }

        if (string.Equals(servingRule, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            return new RoomOrderMenuPlanLine
            {
                TableId = table.TableId,
                TableName = table.TableName,
                GuestCount = table.GuestCount,
                SaveQuantity = manualQuantity,
                DisplayQuantity = manualQuantity,
                DisplayRuleText = $"{manualQuantity:N0} thủ công"
            };
        }

        var quantityPerGuest = Math.Max(1, table.GuestCount) * baseQuantity;
        return new RoomOrderMenuPlanLine
        {
            TableId = table.TableId,
            TableName = table.TableName,
            GuestCount = table.GuestCount,
            SaveQuantity = quantityPerGuest,
            DisplayQuantity = quantityPerGuest,
            DisplayRuleText = $"{table.GuestCount} khách x {baseQuantity:N0}"
        };
    }
}
