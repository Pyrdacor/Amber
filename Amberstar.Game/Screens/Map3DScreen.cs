using Amber.Assets.Common;
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

	class NPC
	{
		private readonly IMap3D map;
		private readonly MapNPC data;
		private readonly Position[] positions;
		private readonly Func<int, int, int, bool> canMoveChecker;
		private int currentPathLength = 0;
		private Direction direction = Direction.North;
		private Position position; // this one is 1-based

		public int Index { get; }

		public MapNPCType Type => data.Type;

		public Position Position => new(position.X - 1, position.Y - 1);

		public int Icon => data.Icon;

		public NPC(IMap3D map, int index, Position[] positions, GameState gameState,
			Func<int, int, int, bool> canMoveChecker)
		{
			this.map = map;
			Index = index;
			data = map.NPCs[index];
			this.positions = positions;
			this.canMoveChecker = canMoveChecker;

			UpdatePosition(gameState);
		}

		private void UpdatePosition(GameState gameState)
		{
			switch (data.WalkType)
			{
				case MapNPCWalkType.Stationary:
					position = positions[0];
					break;
				case MapNPCWalkType.Path:
				{
					int totalSteps = gameState.Hour * 12 + gameState.Minute / 5;
					position = positions[totalSteps];
					break;
				}
				case MapNPCWalkType.Chase:
					// TODO
					break;
				default: // random
					if (position == new Position()) // first time
						position = positions[0];
					else
						MoveRandomly();
					break;
			}
		}

		private void SetupNewRandomPath()
		{
			currentPathLength = Game.Random(1, 4);
			int dir = (int)direction + 1;
			dir += Game.Random(0, 1);
			dir &= 0x3;
			direction = (Direction)dir;

			switch (direction)
			{
				case Direction.North:
					if (currentPathLength <= position.Y)
						currentPathLength += position.Y - currentPathLength - 1;
					break;
				case Direction.East:
					if (currentPathLength > map.Width - position.X)
						currentPathLength += map.Width - position.X - currentPathLength - 1;
					break;
				case Direction.South:
					if (currentPathLength > map.Height - position.Y)
						currentPathLength += map.Height - position.Y - currentPathLength - 1;
					break;
				case Direction.West:
					if (currentPathLength <= position.X)
						currentPathLength += position.X - currentPathLength - 1;
					break;
			}
		}

		private void MoveRandomly()
		{
			if (currentPathLength == 0)
				SetupNewRandomPath();

			for (int i = 0; i < 4; i++)
			{
				// 4 tries
				var offset = direction.Offset();

				if (canMoveChecker(Position.X + offset.X, Position.Y + offset.Y, data.TravelType))
				{
					currentPathLength--;
					position = new(position.X + offset.X, position.Y + offset.Y);
					return;
				}

				SetupNewRandomPath();
			}

			currentPathLength = 0;
		}
		
		public void Update(Game game)
		{
			UpdatePosition(game.State);
		}
	}

	const int TicksPerStep = 120; // TODO

	const int ViewWidth = 144;
	const int ViewHeight = 144;
	const int OffsetX = 32;
	const int OffsetY = 49;
	Dictionary<int, IGraphic> backgrounds = [];
	Game? game;
	IMap3D? map;
	ILabData? labData;
	readonly List<IAnimatedSprite> images = [];
	readonly List<NPC> npcs = [];
	ButtonLayout buttonLayout = ButtonLayout.Movement;
	long currentTicks = 0;
	byte palette = 0;
	bool paused = false;

	public override ScreenType Type { get; } = ScreenType.Map3D;

	internal void MapChanged()
	{
		LoadMap(game!.State.MapIndex);
		AfterMove();
	}

	public override void Init(Game game)
	{
		this.game = game;
		backgrounds = game.AssetProvider.GraphicLoader.LoadAllBackgroundGraphics();
	}

	public override void ScreenPushed(Game game, Screen screen)
	{
		base.ScreenPushed(game, screen);

		paused = true;

		// TODO: check for transparent screens?
		images.ForEach(image => image.Visible = false);
	}

	public override void ScreenPopped(Game game, Screen screen)
	{
		game.SetLayout(Layout.Map3D);
		images.ForEach(image => image.Visible = true);

		paused = false;

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

		game.Time.MinuteChanged += MinuteChanged;
	}

	public override void Close(Game game)
	{
		game.Time.MinuteChanged -= MinuteChanged;
		ClearView();

		base.Close(game);
	}

	private void MinuteChanged()
	{
		if (paused)
			return;

		if (npcs.Count != 0) // TODO: check for active ones only (or maybe remove inactive ones)
		{
			foreach (var npc in npcs)
			{
				npc.Update(game!);
			}

			var offsets = PerspectiveMappings[game!.State.PartyDirection];
			var playerPosition = game!.State.PartyPosition;

			for (int i = 0; i < 10; i++)
			{
				var offset = offsets[(PerspectiveLocation)i];
				int x = playerPosition.X + offset.X;
				int y = playerPosition.Y + offset.Y;

				if (npcs.Any(npc => npc.Position.X == x && npc.Position.Y == y))
				{
					UpdateView();
					break;
				}
			}
		}
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

	private bool CanMoveTo(int x, int y, bool player, int collisionClass)
	{
		if (x < 0 || y < 0 || x >= map!.Width || y >= map.Height)
			return false;

		if (!player)
		{
			var testPosition = new Position(x, y);

			if (npcs.Any(npc => npc.Position == testPosition))
				return false;
		}

		var tileFlags = GetTileFlags(x, y);

		if (tileFlags.HasFlag(LabTileFlags.BlockAllMovement))
			return false;

		if (!tileFlags.HasFlag((LabTileFlags)(1 << (8 + collisionClass))))
			return false;

		return true;
	}

	private LabTileFlags GetTileFlags(int x, int y)
	{
		var tile = map!.Tiles[x + y * map.Width];

		if (tile.LabTileIndex == 0)
			return LabTileFlags.None;

		var labTile = map.LabTiles[tile.LabTileIndex - 1];

		return labTile.Flags;
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
		var playerPosition = game!.State.PartyPosition;

		ClearView();

		var offsets = PerspectiveMappings[game.State.PartyDirection];
		var layer = game.Renderer.Layers[(int)Layer.Map3D];
		var textureAtlas = layer.Config.Texture!;
		byte displayLayer = 40;

		var floor = backgrounds[labData!.FloorIndex];
		var floorSprite = layer.SpriteFactory!.CreateAnimated();
		floorSprite.Size = new(floor.Width, floor.Height);
		floorSprite.Position = new(OffsetX, OffsetY + ViewHeight - floor.Height);
		floorSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetBackgroundGraphicIndex(labData.FloorIndex));
		floorSprite.Opaque = true;
		floorSprite.DisplayLayer = 0;
		floorSprite.PaletteIndex = palette;
		floorSprite.Visible = true;
		images.Add(floorSprite);

		var ceiling = backgrounds[labData!.CeilingIndex];
		var ceilingSprite = layer.SpriteFactory!.CreateAnimated();
		ceilingSprite.Size = new(ceiling.Width, ceiling.Height);
		ceilingSprite.Position = new(OffsetX, OffsetY);
		ceilingSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetBackgroundGraphicIndex(labData.CeilingIndex));
		ceilingSprite.Opaque = true;
		ceilingSprite.DisplayLayer = 10;
		ceilingSprite.PaletteIndex = palette;
		ceilingSprite.Visible = true;
		images.Add(ceilingSprite);

		for (int i = 0; i < 14; i++)
		{
			var perspectiveLocation = (PerspectiveLocation)i;
			var offset = offsets[perspectiveLocation];
			int x = playerPosition.X + offset.X;
			int y = playerPosition.Y + offset.Y;
			var tile = map!.Tiles[x + y * map.Width];

			if (tile.LabTileIndex == 0)
				continue;

			var labTile = map!.LabTiles[tile.LabTileIndex - 1];

			void DrawBlock(ILabBlock labBlock, int? customRenderX = null)
			{
				if (labBlock.Type != LabBlockType.Wall && i > 11)
					return;

				if (i > 11)
				{
					displayLayer = (byte)(20 + (i % 12) * 10);
					perspectiveLocation = (PerspectiveLocation)((int)perspectiveLocation % 12);
				}

				var facing = labBlock.Type == LabBlockType.Overlay ? FacingByRelativeOffset(offset) : BlockFacing.FacingPlayer;
				byte displayPlayerAdd = (byte)(facing == BlockFacing.FacingPlayer ? 10 : 5);

				if (facing != BlockFacing.FacingPlayer)
					AddBlockSprite(facing);

				AddBlockSprite(BlockFacing.FacingPlayer);

				void AddBlockSprite(BlockFacing facing)
				{
					var perspective = labBlock.Perspectives.FirstOrDefault(p => p.Location == perspectiveLocation && p.Facing == facing);

					if (perspective.Frames == null)
						return;

					if (customRenderX == OffsetX + ViewWidth)
						customRenderX -= perspective.Frames[0].Width / 2;

					if (perspective.SpecialRenderPosition != null)
					{
						var blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = 1;
						blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing));
						blockSprite.Position = new(customRenderX ?? (OffsetX + perspective.RenderPosition.X), OffsetY + perspective.RenderPosition.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);

						displayLayer += (byte)(displayPlayerAdd / 2);

						int x = customRenderX ?? (OffsetX + perspective.RenderPosition.X);
						x += perspective.SpecialRenderPosition.Value.Y - perspective.RenderPosition.X;

						blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = perspective.Frames.Length - 1;
						blockSprite.Size = new Size(perspective.Frames[1].Width, perspective.Frames[1].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing) + 1);
						blockSprite.Position = new(x, OffsetY + perspective.SpecialRenderPosition.Value.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);
					}
					else
					{
						var blockSprite = layer.SpriteFactory!.CreateAnimated();
						blockSprite.FrameCount = perspective.Frames.Length;
						blockSprite.Size = new Size(perspective.Frames[0].Width, perspective.Frames[0].Height);
						blockSprite.DisplayLayer = displayLayer;
						blockSprite.PaletteIndex = palette;
						blockSprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetLabBlockGraphicIndex(labBlock.Index, perspectiveLocation, facing));
						blockSprite.Position = new(customRenderX ?? (OffsetX + perspective.RenderPosition.X), OffsetY + perspective.RenderPosition.Y);
						blockSprite.Visible = true;

						images.Add(blockSprite);
					}

					displayLayer += displayPlayerAdd;
				}
			}

			var primary = labData!.LabBlocks[labTile.PrimaryLabBlockIndex - 1];

			if (primary.Type == LabBlockType.Overlay && labTile.SecondaryLabBlockIndex != 0)
			{
				// Draw underlay for overlays
				DrawBlock(labData!.LabBlocks[labTile.SecondaryLabBlockIndex - 1]);
			}

			// Draw underlay or overlay
			if (labTile.PrimaryLabBlockIndex != 1) // 1 seems to be a marker for free tiles
				DrawBlock(primary);

			var npc = npcs.FirstOrDefault(npc => npc.Position == new Position(x, y));

			if (npc != null)
			{
				var objectBlock = labData!.LabBlocks[npc.Icon - 1];
				int? customX = null;

				if ((int)perspectiveLocation % 3 == 0) // left row
					customX = OffsetX;
				else if ((int)perspectiveLocation % 3 == 1) // right row
					customX = OffsetX + ViewWidth;

				DrawBlock(objectBlock, customX);
			}
		}
	}

	private void ClearView()
	{
		images.ForEach(image => image.Visible = false);
		images.Clear();
	}

	private void LoadMap(int index)
	{
		map = game!.AssetProvider.MapLoader.LoadMap(index) as IMap3D; // TODO: catch exceptions
		labData = game!.AssetProvider.LabDataLoader.LoadLabData(map!.LabDataIndex);
		palette = game.PaletteIndexProvider.GetLabyrinthPaletteIndex(labData.PaletteIndex - 1);

		for (int i = 0; i < map.NPCs.Length; i++)
		{
			var npcData = map.NPCs[i];

			if (npcData.Index != 0 && npcData.Icon != 0)
			{
				npcs.Add(new NPC(map, i, map.NPCPositions[i], game.State,
					(int x, int y, int collisionClass) => CanMoveTo(x, y, false, collisionClass)));
			}
		}

		game.State.MapIndex = index;
		game.State.TravelType = TravelType.Walk;
	}
}
