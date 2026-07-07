using Microsoft.Xna.Framework;
using MyTurn.Desktop;
using MyTurn.Presentation;

namespace MyTurn.Tests.Desktop;

[TestFixture]
public sealed class DesktopLayoutTests
{
    [TestCase(ScreenKind.MainMenu)]
    [TestCase(ScreenKind.LoadGame)]
    [TestCase(ScreenKind.Hub)]
    [TestCase(ScreenKind.World)]
    [TestCase(ScreenKind.Combat)]
    [TestCase(ScreenKind.Party)]
    [TestCase(ScreenKind.Inventory)]
    [TestCase(ScreenKind.Message)]
    public void ScreenRectangles_FitInsideVirtualCanvas(ScreenKind screenKind)
    {
        var layout = new DesktopLayout();

        foreach (var rectangle in layout.GetScreenRectangles(screenKind))
        {
            Assert.That(layout.Contains(rectangle), Is.True, $"{screenKind} rectangle {rectangle} should fit inside the virtual canvas.");
        }
    }

    [Test]
    public void WorldLayout_PanelsDoNotOverlap()
    {
        var layout = new DesktopLayout().World;

        Assert.Multiple(() =>
        {
            Assert.That(Overlaps(layout.Field, layout.StatusPanel), Is.False);
            Assert.That(Overlaps(layout.Field, layout.DialoguePanel), Is.False);
            Assert.That(Overlaps(layout.StatusPanel, layout.DialoguePanel), Is.False);
        });
    }

    [Test]
    public void CombatLayout_PanelsDoNotOverlap()
    {
        var layout = new DesktopLayout().Combat;
        var panels = new[] { layout.Battlefield, layout.LogPanel, layout.CommandPanel, layout.PartyPanel };

        for (var i = 0; i < panels.Length; i++)
        {
            for (var j = i + 1; j < panels.Length; j++)
            {
                Assert.That(Overlaps(panels[i], panels[j]), Is.False, $"Panel {i} overlaps panel {j}.");
            }
        }
    }

    private static bool Overlaps(Rectangle first, Rectangle second)
    {
        return first.Intersects(second);
    }
}
