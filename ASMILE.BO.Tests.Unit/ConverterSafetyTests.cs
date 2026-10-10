using System.Globalization;
using System.Windows.Data;
using ASMILE.BO.App.Converters;
using ASMILE.BO.App.Views;
using Xunit;

namespace ASMILE.BO.Tests.Unit;

public sealed class ConverterSafetyTests
{
    [Fact]
    public void One_way_room_rack_converters_do_not_throw_when_convert_back_is_requested()
    {
        IValueConverter[] converters =
        [
            new OccupancyToColorConverter(),
            new HousekeepingToColorConverter(),
            new IntToVisibilityConverter(),
            new BoolToVisibilityConverter(),
            new TabIndexToVisibilityConverter(),
            new StringToBrushConverter(),
            new BoolToThicknessConverter(),
            new StringToVisibilityConverter(),
            new RoutingToVisibilityConverter(),
            new RoutingToFixedVisConverter(),
            new ScoreToBrushConverter(),
            new WeekendToColorConverter(),
            new ActiveToColorConverter(),
            new ActiveToTextConverter(),
            new GreaterThanZeroToVisibilityConverter(),
            new StatusToColorConverter(),
            new PriorityToColorConverter(),
            new FeedbackTypeColorConverter(),
            new LevelToColorConverter(),
            new EarnRedeemColorConverter(),
            new SeverityToColorConverter(),
            new ActionToColorConverter(),
            new ProgressPercentToWidthConverter(),
            new ShiftToTimeConverter(),
            new ShiftToEndTimeConverter()
        ];

        foreach (var converter in converters)
        {
            var result = converter.ConvertBack(
                value: "unsupported",
                targetType: typeof(object),
                parameter: null!,
                culture: CultureInfo.InvariantCulture);

            Assert.Same(Binding.DoNothing, result);
        }
    }

    [Fact]
    public void Group_rooming_vip_converter_does_not_throw_when_convert_back_is_requested()
    {
        var result = new BoolToVipConverter().ConvertBack(
            value: "unsupported",
            targetType: typeof(object),
            parameter: null!,
            culture: CultureInfo.InvariantCulture);

        Assert.Same(Binding.DoNothing, result);
    }

    [Fact]
    public void Additional_display_converters_do_not_throw_when_convert_back_is_requested()
    {
        IValueConverter[] converters =
        [
            new StatusDisplayConverter(),
            new QuoteTabBgConverter(),
            new QuoteTabFgConverter(),
            new QuoteTabBrConverter(),
            new QuoteTabLabelConverter(),
            new OneBasedIndexConverter(),
            new SupplierCreatedDateConverter(),
            new VirtualExplorerNodeIconConverter()
        ];

        foreach (var converter in converters)
        {
            var result = converter.ConvertBack(
                value: "unsupported",
                targetType: typeof(object),
                parameter: null!,
                culture: CultureInfo.InvariantCulture);

            Assert.Same(Binding.DoNothing, result);
        }
    }

    [Fact]
    public void Additional_multi_value_converters_do_not_throw_when_convert_back_is_requested()
    {
        IMultiValueConverter[] converters =
        [
            new StayNightsConverter(),
            new IsAlreadyAddedConverter(),
            new StringEqualityConverter()
        ];

        foreach (var converter in converters)
        {
            var result = converter.ConvertBack(
                value: "unsupported",
                targetTypes: [typeof(object), typeof(object)],
                parameter: null!,
                culture: CultureInfo.InvariantCulture);

            Assert.Single(result);
            Assert.Same(Binding.DoNothing, result[0]);
        }
    }
}
