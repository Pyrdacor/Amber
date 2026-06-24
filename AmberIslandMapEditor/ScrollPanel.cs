namespace AmberIslandMapEditor;

/// <summary>
/// An AutoScroll panel that does NOT auto-scroll to a child when that child
/// gets focus. Without this, clicking the (large) map/tileset child makes the
/// panel scroll it into view, jumping the scroll position to the top-left.
/// </summary>
internal sealed class ScrollPanel : Panel
{
    public ScrollPanel()
    {
        AutoScroll = true;
    }

    protected override Point ScrollToControl(Control activeControl) => AutoScrollPosition;
}
