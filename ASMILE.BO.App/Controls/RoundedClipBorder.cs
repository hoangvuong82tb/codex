using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Controls;

public class RoundedClipBorder : Border
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);
        ApplyClip(arranged);
        return arranged;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        ApplyClip(sizeInfo.NewSize);
    }

    private void ApplyClip(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            Clip = null;
            if (Child is UIElement invalidChild)
            {
                invalidChild.Clip = null;
            }
            return;
        }

        var radius = CornerRadius.TopLeft;
        if (CornerRadius.TopRight > radius) radius = CornerRadius.TopRight;
        if (CornerRadius.BottomRight > radius) radius = CornerRadius.BottomRight;
        if (CornerRadius.BottomLeft > radius) radius = CornerRadius.BottomLeft;

        var maxRadius = Math.Min(size.Width, size.Height) / 2d;
        radius = Math.Min(radius, maxRadius);

        Clip = null;

        if (Child is not UIElement child)
        {
            return;
        }

        var childSize = child.RenderSize;
        if (childSize.Width <= 0 || childSize.Height <= 0)
        {
            child.Clip = null;
            return;
        }

        var childRadius = Math.Min(radius, Math.Min(childSize.Width, childSize.Height) / 2d);
        child.Clip = new RectangleGeometry(new Rect(0, 0, childSize.Width, childSize.Height), childRadius, childRadius);
    }
}
