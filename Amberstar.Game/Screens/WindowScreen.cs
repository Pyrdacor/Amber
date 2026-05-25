using Amber.Common;
using Amberstar.Game.UI;

namespace Amberstar.Game.Screens;

internal abstract class WindowScreen(int x, int y, int widthInTiles, int heightInTiles, byte windowDisplayLayer = 0) : Screen
{
	Window? window;

    public virtual int HeightInTiles { get; } = heightInTiles;

	public virtual bool CloseOnNextInput { get; } = false;

    public virtual bool CreateWindowInOpenHandler { get; } = false;

    public virtual bool TrapMouse { get; } = true;

    public virtual bool Dark { get; } = false;

    public sealed override bool Transparent { get; } = true;

    public Rect ClientArea => window?.ClientArea ?? new Rect(x, y, widthInTiles * 16, HeightInTiles * 16);

    public override void Init()
    {
        base.Init();

		if (!CreateWindowInOpenHandler)
			CreateWindow();
    }

	public override void Open(Action? closeAction)
	{
        if (CreateWindowInOpenHandler)
            CreateWindow();

        base.Open(closeAction);

        if (TrapMouse)
            Game.TrapMouse(ClientArea);
    }

    public override void Close()
    {
        if (TrapMouse)
            Game.UntrapMouse();

        base.Close();
    }

	private void CreateWindow()
	{
		window?.Destroy();
		window = AddWindow(x, y, widthInTiles, HeightInTiles, Dark, windowDisplayLayer);
        SetAnchorToWindow(window);
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (CloseOnNextInput)
        {
            Game.ScreenHandler.PopScreen();
            return true;
        }

        return base.KeyDown(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (CloseOnNextInput)
        {
            Game.ScreenHandler.PopScreen();
            return true;
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
