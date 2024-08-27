using Amberstar.Game.Screens;
using Amberstar.GameData;

namespace Amberstar.Game
{
	partial class Game
	{
		internal IText? CurrentText { get; private set; }

		internal IMap? CurrentMap
		{
			get
			{
				return (IMap?)(ScreenHandler.FindScreen(ScreenType.Map2D) as Map2DScreen)?.Map ??
					(ScreenHandler.FindScreen(ScreenType.Map3D) as Map3DScreen)?.Map;
			}
		}
	}
}
