using Amber.Common;
using Amberstar.Game.UI;

namespace Amberstar.Game.Screens;

internal abstract class WindowScreen(int x, int y, int widthInTiles, int heightInTiles, byte windowDisplayLayer = 0) : Screen
{
	Game? game;
	Window? window;

    public virtual int HeightInTiles { get; } = heightInTiles;

	public virtual bool CloseOnNextInput { get; } = false;

    public virtual bool CreateWindowInOpenHandler { get; } = false;

    public sealed override bool Transparent { get; } = true;

    public Rect ClientArea => window?.ClientArea ?? new Rect(x, y, widthInTiles * 16, HeightInTiles * 16);

    public override void Init(Game game)
    {
        this.game = game;

        base.Init(game);

		if (!CreateWindowInOpenHandler)
			CreateWindow();
    }

	public override void Open(Game game, Action? closeAction)
	{
        if (CreateWindowInOpenHandler)
            CreateWindow();

        base.Open(game, closeAction);
	}

	private void CreateWindow()
	{
		window?.Destroy();
		window = AddWindow(x, y, widthInTiles, HeightInTiles, dark: true, windowDisplayLayer);
        SetAnchorToWindow(window);
    }

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
        if (CloseOnNextInput)
        {
            game!.ScreenHandler.PopScreen();
            return true;
        }

        return base.KeyDown(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (CloseOnNextInput)
        {
            game!.ScreenHandler.PopScreen();
            return true;
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }
}
