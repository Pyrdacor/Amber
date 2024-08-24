using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Events;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal class Map3DScreen : Screen
{
	private static readonly Dictionary<Direction, Dictionary<PerspectiveLocation, Position>> PerspectiveMappings =
		new()
		{
			{
				Direction.North, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(-1, -3) },
					{ PerspectiveLocation.Forward3Right1, new Position(1, -3) },
					{ PerspectiveLocation.Forward3, new Position(0, -3) },
					{ PerspectiveLocation.Forward2Left1, new Position(-1, -2) },
					{ PerspectiveLocation.Forward2Right1, new Position(1, -2) },
					{ PerspectiveLocation.Forward2, new Position(0, -2) },
					{ PerspectiveLocation.Forward1Left1, new Position(-1, -1) },
					{ PerspectiveLocation.Forward1Right1, new Position(1, -1) },
					{ PerspectiveLocation.Forward1, new Position(0, -1) },
					{ PerspectiveLocation.Left1, new Position(-1, 0) },
					{ PerspectiveLocation.Right1, new Position(1, 0) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(-2, -3) },
					{ PerspectiveLocation.Forward3Right2, new Position(2, -3) },
				}
			},
			{
				Direction.East, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(3, -1) },
					{ PerspectiveLocation.Forward3Right1, new Position(3, 1) },
					{ PerspectiveLocation.Forward3, new Position(3, 0) },
					{ PerspectiveLocation.Forward2Left1, new Position(2, -1) },
					{ PerspectiveLocation.Forward2Right1, new Position(2, 1) },
					{ PerspectiveLocation.Forward2, new Position(2, 0) },
					{ PerspectiveLocation.Forward1Left1, new Position(1, -1) },
					{ PerspectiveLocation.Forward1Right1, new Position(1, 1) },
					{ PerspectiveLocation.Forward1, new Position(1, 0) },
					{ PerspectiveLocation.Left1, new Position(0, -1) },
					{ PerspectiveLocation.Right1, new Position(0, 1) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(3, -2) },
					{ PerspectiveLocation.Forward3Right2, new Position(3, 2) },
				}
			},
			{
				Direction.South, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(1, 3) },
					{ PerspectiveLocation.Forward3Right1, new Position(-1, 3) },
					{ PerspectiveLocation.Forward3, new Position(0, 3) },
					{ PerspectiveLocation.Forward2Left1, new Position(1, 2) },
					{ PerspectiveLocation.Forward2Right1, new Position(-1, 2) },
					{ PerspectiveLocation.Forward2, new Position(0, 2) },
					{ PerspectiveLocation.Forward1Left1, new Position(1, 1) },
					{ PerspectiveLocation.Forward1Right1, new Position(-1, 1) },
					{ PerspectiveLocation.Forward1, new Position(0, 1) },
					{ PerspectiveLocation.Left1, new Position(1, 0) },
					{ PerspectiveLocation.Right1, new Position(-1, 0) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(2, 3) },
					{ PerspectiveLocation.Forward3Right2, new Position(-2, 3) },
				}
			},
			{
				Direction.West, new Dictionary<PerspectiveLocation, Position>
				{
					{ PerspectiveLocation.Forward3Left1, new Position(-3, 1) },
					{ PerspectiveLocation.Forward3Right1, new Position(-3, -1) },
					{ PerspectiveLocation.Forward3, new Position(-3, 0) },
					{ PerspectiveLocation.Forward2Left1, new Position(-2, 1) },
					{ PerspectiveLocation.Forward2Right1, new Position(-2, -1) },
					{ PerspectiveLocation.Forward2, new Position(-2, 0) },
					{ PerspectiveLocation.Forward1Left1, new Position(-1, 1) },
					{ PerspectiveLocation.Forward1Right1, new Position(-1, -1) },
					{ PerspectiveLocation.Forward1, new Position(-1, 0) },
					{ PerspectiveLocation.Left1, new Position(0, 1) },
					{ PerspectiveLocation.Right1, new Position(0, -1) },
					{ PerspectiveLocation.PlayerLocation, new Position(0, 0) },
					{ PerspectiveLocation.Forward3Left2, new Position(-3, 2) },
					{ PerspectiveLocation.Forward3Right2, new Position(-3, -2) },
				}
			}
		};

	enum ButtonLayout
	{
		Movement,
		Actions
	}

	const int TicksPerStep = 120; // TODO

	const int ViewWidth = 144;
	const int ViewHeight = 144;
	const int OffsetX = 32;
	const int OffsetY = 49;
	Game? game;
	IMap3D? map;
	ILabData? labData;
	readonly List<IAnimatedSprite> images = [];
	ButtonLayout buttonLayout = ButtonLayout.Movement;
	long currentTicks = 0;
	byte palette = 0;

	public override ScreenType Type { get; } = ScreenType.Map3D;

	internal void MapChanged()
	{
		LoadMap(game!.State.MapIndex);
		AfterMove();
	}

	public override void Init(Game game)
	{
		this.game = game;
	}

	public override void ScreenPushed(Game game, Screen screen)
	{
		base.ScreenPushed(game, screen);

		// TODO: check for transparent screens?
		images.ForEach(image => image.Visible = false);
	}

	public override void ScreenPopped(Game game, Screen screen)
	{
		game.SetLayout(Layout.Map3D);
		images.ForEach(image => image.Visible = true);

		base.ScreenPopped(game, screen);
	}

	public override void Open(Game game, Action? closeAction)
	{
		base.Open(game, closeAction);

		currentTicks = 0;

		game.SetLayout(Layout.Map3D);
		buttonLayout = ButtonLayout.Movement;
		LoadMap(game.State.MapIndex);
		AfterMove();
	}

	public override void Close(Game game)
	{
		ClearView();

		base.Close(game);
	}

	public override void Update(Game game, long elapsedTicks)
	{
		if (elapsedTicks == 0)
			return;

		currentTicks += elapsedTicks;
	}

	private void AfterMove()
	{
		// TODO

		var playerPosition = game!.State.PartyPosition;
		UpdateView();

		game.Time.Moved3D();

		// Check for events
		var eventIndex = map!.Tiles[playerPosition.X + playerPosition.Y * map.Width].Event;

		if (eventIndex != 0)
			game.EventHandler.HandleEvent(EventTrigger.Move, Event.CreateEvent(map.Events[eventIndex - 1]), map);
	}

	public override void KeyDown(Key key, KeyModifiers keyModifiers)
	{
		bool left = game!.IsKeyDown(Key.Left) || game.IsKeyDown('A');
		bool right = game!.IsKeyDown(Key.Right) || game.IsKeyDown('D');
		bool forward = game!.IsKeyDown(Key.Up) || game.IsKeyDown('W');
		bool backward = game!.IsKeyDown(Key.Down) || game.IsKeyDown('S');

		void Move(int x, int y)
		{
			game!.State.SetPartyPosition(game.State.PartyPosition.X + x, game.State.PartyPosition.Y + y);
			AfterMove();
		}

		void TurnTo(Direction newDirection)
		{
			game!.State.PartyDirection = newDirection;
			UpdateView();
		}

		// TODO
		switch (game!.State.PartyDirection)
		{
			case Direction.North:
				if (forward && !backward)
					Move(0, -1);
				else if (backward && !forward)
					Move(0, 1);
				else if (left && !right)
					TurnTo(Direction.West);
				else if (right && !left)
					TurnTo(Direction.East);
				break;
			case Direction.East:
				if (forward && !backward)
					Move(1, 0);
				else if (backward && !forward)
					Move(-1, 0);
				else if (left && !right)
					TurnTo(Direction.North);
				else if (right && !left)
					TurnTo(Direction.South);
				break;
			case Direction.South:
				if (forward && !backward)
					Move(0, 1);
				else if (backward && !forward)
					Move(0, -1);
				else if (left && !right)
					TurnTo(Direction.East);
				else if (right && !left)
					TurnTo(Direction.West);
				break;
			case Direction.West:
				if (forward && !backward)
					Move(-1, 0);
				else if (backward && !forward)
					Move(1, 0);
				else if (left && !right)
					TurnTo(Direction.South);
				else if (right && !left)
					TurnTo(Direction.North);
				break;
		}
	}

	public override void KeyUp(Key key, KeyModifiers keyModifiers)
	{
		// TODO
	}

	private BlockFacing FacingByRelativeOffset(Position offset)
	{
		switch (game!.State.PartyDirection)
		{
			case Direction.North:
				if (offset.X < 0)
					return BlockFacing.LeftOfPlayer;
				else if (offset.X > 0) 
					return BlockFacing.RightOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.East:
				if (offset.Y < 0)
					return BlockFacing.LeftOfPlayer;
				else if (offset.Y > 0)
					return BlockFacing.RightOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.South:
				if (offset.X < 0)
					return BlockFacing.RightOfPlayer;
				else if (offset.X > 0)
					return BlockFacing.LeftOfPlayer;
				return BlockFacing.FacingPlayer;
			case Direction.West:
				if (offset.Y < 0)
					return BlockFacing.RightOfPlayer;
				else if (offset.Y > 0)
					return BlockFacing.LeftOfPlayer;
				return BlockFacing.FacingPlayer;
			default:
				return BlockFacing.FacingPlayer;
		}
	}

	private void UpdateView()
	{
		// TODO: floor and ceiling

		var playerPosition = game!.State.PartyPosition;

		ClearView();

		var offsets = PerspectiveMappings[game.State.PartyDirection];
		var layer = game.Renderer.Layers[(int)Layer.Map3D];
		var textureAtlas = layer.Config.Texture!;
		byte displayLayer = 10;

		for (int i = 0; i < 12; i++)
		{
			var perspectiveLocation = (PerspectiveLocation)i;
			var offset = offsets[perspectiveLocation];
			int x = playerPosition.X + offset.X;
			int y = playerPosition.Y + offset.Y;
			var tile = map!.Tiles[x + y * map.Width];

			if (tile.LabTileIndex == 0)
				continue;

			var labTile = map!.LabTiles[tile.LabTileIndex - 1];

			void DrawBlock(ILabBlock labBlock)
			{
				var facing = labBlock.Type == LabBlockType.Overlay ? FacingByRelativeOffset(offset) : BlockFacing.FacingPlayer;
				var perspective = labBlock.Perspectives.FirstOrDefault(p => p.Location == perspectiveLocation && p.Facing == facing);

				if (perspective.Frames == null)
					return;

				var blockSprite = layer.SpriteFactory!.CreateAnimated();
				blockSprite.FrameCount = perspective.Frames.Length;
				blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
				blockSprite.DisplayLayer = displayLayer;
				blockSprite.PaletteIndex = palette;
				blockSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing));
				blockSprite.Position = new(OffsetX + perspective.RenderPosition.X, OffsetY + perspective.RenderPosition.Y);
				blockSprite.Visible = true;

				images.Add(blockSprite);

				displayLayer += 10;
			}

			var primary = labData!.LabBlocks[labTile.PrimaryLabBlockIndex - 1];

			if (primary.Type == LabBlockType.Overlay && labTile.SecondaryLabBlockIndex != 0)
			{
				// Draw underlay for overlays
				DrawBlock(labData!.LabBlocks[labTile.SecondaryLabBlockIndex - 1]);
			}

			// Draw underlay or overlay
			if (labTile.PrimaryLabBlockIndex != 1) // TODO: 1 seems to be an NPC/Object marker
				DrawBlock(primary);
		}
	}

	private void ClearView()
	{
		images.ForEach(image => image.Visible = false);
		images.Clear();
	}

	/*private IAnimatedSprite CreateTileSprite(Dictionary<int, IAnimatedSprite> mapLayer, int gridIndex, int x, int y, int index, int baseLineOffset = 0)
	{
		var renderLayer = game!.Renderer.Layers[(int)Layer.Map2D];

		if (!mapLayer.TryGetValue(gridIndex, out var tileSprite))
		{
			tileSprite = renderLayer.SpriteFactory!.CreateAnimated();
			tileSprite.Position = new(x, y);
			tileSprite.Size = new(TileWidth, TileHeight);
			tileSprite.Opaque = mapLayer == underlay;
			mapLayer.Add(gridIndex, tileSprite);
		}

		var tileInfo = GetTileInfo(index);

		tileSprite.FrameCount = Math.Max(1, tileInfo.FrameCount);			
		tileSprite.TextureOffset = renderLayer.Config.Texture!.GetOffset(tileGraphicOffset + tileInfo.ImageIndex);
		tileSprite.PaletteIndex = game.PaletteIndexProvider.GetTilesetPaletteIndex(map!.TilesetIndex);
		tileSprite.BaseLineOffset = baseLineOffset;
		tileSprite.Visible = true;

		return tileSprite;
	}*/

	/*private ITile GetTileInfo(int index)
	{
		var tileset = tilesets![map!.TilesetIndex - 1];
		return tileset!.Tiles[index - 1];
	}*/

	private void LoadMap(int index)
	{
		map = game!.AssetProvider.MapLoader.LoadMap(index) as IMap3D; // TODO: catch exceptions
		labData = game!.AssetProvider.LabDataLoader.LoadLabData(map!.LabDataIndex);
		palette = game.PaletteIndexProvider.GetLabyrinthPaletteIndex(labData.PaletteIndex - 1);

		game.State.MapIndex = index;
		game.State.TravelType = TravelType.Walk;
	}
}
