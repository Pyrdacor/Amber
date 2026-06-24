namespace AmberIslandTilesetEditor;

/// <summary>
/// An AutoScroll panel that does NOT auto-scroll to a child when that child
/// gets focus, which would otherwise jump the scroll position on click.
/// </summary>
internal sealed class ScrollPanel : Panel
{
    public ScrollPanel()
    {
        AutoScroll = true;
    }

    protected override Point ScrollToControl(Control activeControl) => AutoScrollPosition;
}
