namespace Amberstar.Game.Screens
{
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
		// TODO ...
	}

	internal abstract class Screen
	{
		public abstract ScreenType Type { get; }

		public virtual void Init(Game game)
		{
			// default: empty
		}

		public virtual void Destroy(Game game)
		{
			// default: empty
		}

		public virtual void Open(Game game)
		{
			// default: empty
		}

		public virtual void Close(Game game)
		{
			// default: empty
		}

		public virtual void ScreenPushed(Game game, Screen screen)
		{
			// default: empty
		}

		public virtual void ScreenPopped(Game game, Screen screen)
		{
			// default: empty
		}

		public virtual void Update(Game game, double delta)
		{
			// default: empty
		}

		public virtual void Render(Game game, double delta)
		{
			// default: empty
		}
	}

	internal class ScreenHandler(Game game) : IDisposable
	{
		readonly Stack<Screen> screens = [];
		readonly List<Screen> createdScreens = [];

		public Screen? ActiveScreen => screens.Count == 0 ? null : screens.Peek();

		public Screen Create(ScreenType screenType)
		{
			var screen = screenType switch
			{
				ScreenType.Map2D => new Map2DScreen(),
				_ => throw new NotImplementedException()
			};

			screen.Init(game);
			createdScreens.Add(screen);

			return screen;
		}

		public bool PushScreen(Screen screen)
		{
			var currentScreen = ActiveScreen;

			if (currentScreen?.Type == screen.Type)
				return false;

			screens.Push(screen);

			currentScreen?.ScreenPushed(game, screen);
			screen.Open(game);

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

		public void ReplaceScreen(Screen screen)
		{
			PopScreen();
			PushScreen(screen);
		}

		public void Dispose()
		{
			createdScreens.ForEach(screen => screen.Destroy(game));
			createdScreens.Clear();
		}
	}
}
