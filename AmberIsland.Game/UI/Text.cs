namespace AmberIsland.Game.UI;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

internal interface IRenderText
{
    bool SupportsScrolling { get; }
    int TextLineCount { get; }
	int LineHeight { get; }
    bool Visible { get; set; }

    event Action? ScrollEnded;

    void Show(int x, int y, byte displayLayer);
    void ShowInArea(int x, int y, int width, int height, byte displayLayer, TextAlignment textAlignment = TextAlignment.Left);
    void Delete();
    bool Scroll(int lines);
    bool ScrollFullHeight();
}
