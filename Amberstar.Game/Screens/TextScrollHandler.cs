using Amberstar.Game.UI;

namespace Amberstar.Game.Screens;

internal class TextScrollHandler
{
    Label? label = null;
    bool scrolling = false;

    public bool CanAbort { get; set; } = false;

    public event Action? Aborted;
    public event Action? ScrollEnded;

    /// <summary>
    /// Attach the scroll handler to a text.
    /// 
    /// Call it shortly after setting the initial text if possible.
    /// </summary>
    /// <param name="label"></param>
    public void Attach(Label label)
    {
        this.label = label;
        scrolling = label.Text!.Scrolling;

        if (scrolling)
            label.Text!.ScrollEnded += ScrolledToEnd;
    }

    public void Detach()
    {
        if (label != null)
            label.Text!.ScrollEnded -= ScrolledToEnd;

        label = null;
        scrolling = false;
    }

    private void ScrolledToEnd()
    {
        label!.Text!.ScrollEnded -= ScrolledToEnd;
        scrolling = false;
    }

    private bool ScrollOrEnd()
    {
        if (!scrolling && label?.SupportsScrolling == true)
        {
            if (!label!.Text!.ScrollFullHeight())
            {
                ScrollEnded?.Invoke();
                return true;
            }
            else
            {
                scrolling = true;
                label.Text!.ScrollEnded += ScrolledToEnd;
            }

            return true;
        }

        return scrolling;
    }

    public bool MouseWheel(float scrollY)
    {
        if (label == null || !label.Visible || !label.SupportsScrolling)
            return false;

        if (scrollY > 0)
            return ScrollOrEnd();

        return true;
    }

    public bool MouseDown(MouseButtons mouseButtons)
    {
        if (mouseButtons == MouseButtons.None)
            return false;

        if (label == null || !label.Visible || !label.SupportsScrolling)
            return false;

        return ScrollOrEnd();
    }

    public bool KeyDown(Key key, KeyModifiers keyModifiers)
    {
        if (label == null || !label.Visible || !label.SupportsScrolling)
            return false;

        if (key == Key.Escape && CanAbort)
        {
            label!.Text!.ScrollEnded -= ScrolledToEnd;
            scrolling = false;           
            Aborted?.Invoke();
            return true;
        }

        if (key == Key.Escape ||
            key == Key.Space ||
            key == Key.Down ||
            key == Key.PageDown)
        {
            return ScrollOrEnd();
        }

        return false;
    }
}
