using Amber.Common;

namespace Amberstar.Game.Screens;

public enum ScreenType
{
	CharacterCreation,
	Map2D,
	Map3D,
	Inventory,
	CharacterStats,
	Camp,
	BattlePositions,
	Door,
	Chest,
	PictureText,
	// TODO ...
}

internal abstract class Screen
{
	Action? closeAction;

	public abstract ScreenType Type { get; }

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
}

internal class ScreenHandler(Game game) : IDisposable
{
	readonly Stack<Screen> screens = [];
	readonly Dictionary<ScreenType, Screen> createdScreens = [];

	public Screen? ActiveScreen => screens.Count == 0 ? null : screens.Peek();

	public Screen Create(ScreenType screenType)
	{
		Screen screen = screenType switch
		{
			ScreenType.Map2D => new Map2DScreen(),
			ScreenType.Map3D => new Map3DScreen(),
			ScreenType.PictureText => new PictureTextScreen(),
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

	public void Dispose()
	{
		createdScreens.Values.ToList().ForEach(screen => screen.Destroy(game));
		createdScreens.Clear();
	}
}
