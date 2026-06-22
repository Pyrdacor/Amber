using Amber.Common;
using Amber.Renderer;
using Amber.Renderer.Common;
using AmberIsland.Game.Screens;
using AmberIsland.Game.UI;

namespace AmberIsland.Game;

public enum ScreenType
{
	Map2D
}

internal abstract class Screen
{
	Action? closeAction;

	public abstract ScreenType Type { get; }

    public virtual bool Transparent { get; } = false;

	public virtual void Init(Game game)
	{
		// default: empty
	}

	public virtual void Destroy(Game game)
	{
		// default: empty
	}

	public virtual void Open(Game game, Action? closeAction)
	{
		this.closeAction = closeAction;
	}

	public virtual void Close(Game game)
	{
		closeAction?.Invoke();
	}

	public virtual void ScreenPushed(Game game, Screen screen)
	{
		// default: empty
	}

	public virtual void ScreenPopped(Game game, Screen screen)
	{
		// default: empty
	}

	public virtual void Update(Game game, long elapsedTicks)
	{
		// default: empty
	}

	public virtual void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		// default: empty
	}

	public virtual void KeyUp(Key key, KeyModifiers keyModifiers)
	{
		// default: empty
	}

	public virtual void KeyChar(char ch, KeyModifiers keyModifiers)
	{
		// default: empty
	}

	public virtual void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		// default: empty
	}

	public virtual void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		// default: empty
	}

	public virtual void MouseMove(Position position, MouseButtons buttons)
	{
		// default: empty
	}

	public virtual void MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
	{
		// default: empty
	}

	#region Helper functions
	protected static void ShowSprites(IEnumerable<ISprite?> sprites, bool show = true)
    {
        foreach (var sprite in sprites)
        {
            if (sprite != null)
                sprite.Visible = show;
        }
    }

    protected static void ShowTexts(IEnumerable<IRenderText?> texts, bool show = true)
    {
        foreach (var text in texts)
        {
			if (text != null)
				text.Visible = show;
        }
    }

	protected static void DeleteSprites(IEnumerable<ISprite?> sprites) => ShowSprites(sprites, false);

    protected static void DeleteTexts(IEnumerable<IRenderText?> texts)
    {
        foreach (var text in texts)
        {
			text?.Delete();
        }
    }
    #endregion
}

internal class ScreenHandler(Game game) : IDisposable
{
	readonly Stack<Screen> screens = [];
	readonly Dictionary<ScreenType, Screen> createdScreens = [];

	public Screen? ActiveScreen => screens.Count == 0 ? null : screens.Peek();
	public Screen? LastScreen => screens.Skip(1).FirstOrDefault();

	public Screen Create(ScreenType screenType)
	{
		Screen screen = screenType switch
		{
			ScreenType.Map2D => new Map2DScreen(),
            _ => throw new NotImplementedException()
		};

		screen.Init(game);
		createdScreens.Add(screenType, screen);

		return screen;
	}

	public bool PushScreen(ScreenType screenType, Action? followAction = null)
	{
		var currentScreen = ActiveScreen;

		if (currentScreen?.Type == screenType)
		{
			followAction?.Invoke();
			return false;
		}

		if (!createdScreens.TryGetValue(screenType, out var screen))
			screen = Create(screenType);

		screens.Push(screen);

		currentScreen?.ScreenPushed(game, screen);
		screen.Open(game, followAction);

		return true;
	}

	public Screen? PopScreen()
	{
		if (screens.Count == 0)
			return null;

		var screen = screens.Pop();

		screen.Close(game);
		ActiveScreen?.ScreenPopped(game, screen);

		return screen;
	}

	public void ClearAllScreens()
	{
		while (screens.Count != 0)
		{
			screens.Pop().Close(game);
		}
	}

	public void ReplaceScreen(ScreenType screenType, Action? followAction = null)
	{
		PopScreen();
		PushScreen(screenType, followAction);
	}

	public Screen? FindScreen(ScreenType screenType)
	{
		foreach (var screen in screens)
		{
			if (screen.Type == screenType)
				return screen;
		}

		return null;
	}

	public void Dispose()
	{
		createdScreens.Values.ToList().ForEach(screen => screen.Destroy(game));
		createdScreens.Clear();
	}
}
