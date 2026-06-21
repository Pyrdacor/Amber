namespace Amberworlds.Game.Screens;

internal delegate void ScreenChangedHandler(Screen newScreen, Screen oldScreen);

internal class ScreenHandler(Game Game) : IDisposable
{
	readonly Stack<Screen> screens = [];
	readonly Dictionary<ScreenType, Screen> createdScreens = [];

	public Screen? ActiveScreen => screens.Count == 0 ? null : screens.Peek();
	public Screen? LastScreen => screens.Skip(1).FirstOrDefault();

	public event ScreenChangedHandler? ScreenChanged;

	public Screen Create(ScreenType screenType)
	{
		Screen screen = screenType switch
		{
			ScreenType.Map3D => new Map3DScreen(),
            _ => throw new NotImplementedException()
		};

        screen.PreInit(Game);
		screen.Init();
        screen.AfterInit();
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

		void Push()
		{
			screens.Push(screen!);

			currentScreen?.ScreenPushed(screen!);
            screen!.PreOpen();
            screen.Open(followAction);

			ScreenChanged?.Invoke(screen, currentScreen);
        }

        bool transparent = currentScreen?.Transparent == true || screen.Transparent;
		bool fadeOut = currentScreen != null && (currentScreen.FadeType == ScreenFadeType.Out || currentScreen.FadeType == ScreenFadeType.Both);
        bool fadeIn = screen != null && (screen.FadeType == ScreenFadeType.In || screen.FadeType == ScreenFadeType.Both);

        if (!transparent && (fadeIn || fadeOut))
        {
			// Game.Fade(Game.DefaultFadeTime, null, Push);
		}
		else
		{
			Push();
		}

        return true;
	}

	public Screen? PopScreen()
	{
		if (screens.Count == 0)
			return null;

        var screen = screens.Pop();
        var prevScreen = ActiveScreen;

        void Pop()
		{
			screen.Close();
            prevScreen?.ScreenPopped(screen);

            ScreenChanged?.Invoke(prevScreen ?? screen, screen);
        }

        bool transparent = prevScreen?.Transparent == true || screen.Transparent;
        bool fadeIn = prevScreen != null && (prevScreen.FadeType == ScreenFadeType.In || prevScreen.FadeType == ScreenFadeType.Both);
        bool fadeOut = screen.FadeType == ScreenFadeType.Out || screen.FadeType == ScreenFadeType.Both;

        if (!transparent && (fadeIn || fadeOut))
        {
            //Game.Fade(Game.DefaultFadeTime, null, Pop);
        }
        else
        {
            Pop();
        }

        return screen;
	}

	public void ClearAllScreens()
	{
		while (screens.Count != 0)
		{
			screens.Pop().Close();
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
		createdScreens.Values.ToList().ForEach(screen => screen.Destroy());
		createdScreens.Clear();
	}
}
