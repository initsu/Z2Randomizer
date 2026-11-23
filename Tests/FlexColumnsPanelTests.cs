using Avalonia;
using Avalonia.Controls;
using CrossPlatformUI.Views;

namespace Z2Randomizer.Tests;

[TestClass]
public class FlexColumnsPanelTests
{
    [TestMethod]
    public void SwitchingOptionsKeepsColumnsAndOnlyUsesVisibleHeightForPlacement()
    {
        var shortOption = new FlexColumnOption
        {
            IsVisible = false,
            Children = { new Border { Height = 5 } },
        };
        var tallOption = new FlexColumnOption
        {
            Children = { new Border { Height = 90 } },
        };
        var following = new Border { Height = 20 };
        var nextColumn = new Border { Height = 70 };
        var section = new StackPanel
        {
            Children = { new Border { Height = 20 }, shortOption, tallOption },
        };
        var panel = new FlexColumnsPanel
        {
            Children =
            {
                new Border { Height = 20 },
                section,
                following,
                nextColumn,
                new Border { Height = 80 },
            },
        };

        Layout();
        Assert.AreEqual(0d, following.Bounds.X);
        Assert.AreEqual(130d, following.Bounds.Y);
        Assert.IsGreaterThan(0d, nextColumn.Bounds.X);
        double columnX = nextColumn.Bounds.X;

        tallOption.IsVisible = false;
        shortOption.IsVisible = true;
        Layout();
        Assert.AreEqual(0d, following.Bounds.X);
        Assert.AreEqual(45d, following.Bounds.Y);
        Assert.AreEqual(columnX, nextColumn.Bounds.X);

        shortOption.IsVisible = false;
        Layout();
        Assert.AreEqual(0d, following.Bounds.X);
        Assert.AreEqual(40d, following.Bounds.Y);
        Assert.AreEqual(columnX, nextColumn.Bounds.X);

        void Layout()
        {
            // This test drives layout directly, without a window's layout manager.
            section.InvalidateMeasure();
            panel.InvalidateMeasure();
            var available = new Size(640, 400);
            panel.Measure(available);
            panel.Arrange(new Rect(available));
        }
    }
}
