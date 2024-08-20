namespace Amberstar.Game.Screens
{
	internal class Map2DScreen : Screen
	{
		Game? game;

		public override ScreenType Type { get; } = ScreenType.Map2D;

		public override void Init(Game game)
		{
			this.game = game;
		}

		public override void Open(Game game)
		{
			var map = game.AssetProvider.MapLoader.LoadMap(65);

			// 
			Console.WriteLine(map.Type.ToString());
		}

		public override void Close(Game game)
		{
			// TODO
		}

		public override void Render(Game game, double delta)
		{
			// TODO
		}

		public override void Update(Game game, double delta)
		{
			// TODO
		}
	}
}
