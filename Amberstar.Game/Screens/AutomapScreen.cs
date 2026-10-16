using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The automap of 3D maps (Automapper in EYE.ASM of the original).
///
/// Explored tiles are shown as small icons (walls, floor, doors, exits, chests,
/// riddlemouths, places and teleporters). The 5 outer tiles of each side are not
/// shown. The view can be scrolled with the arrow buttons.
/// </summary>
internal sealed class AutomapScreen : ButtonGridScreen
{
	const int IconSize = 8;
	const int IconGraphicWidth = 16;
	const int MaxViewWidth = 22;
	const int MaxViewHeight = 18;
	const int MapBorder = 5;
	const int ExitButtonIndex = 4;
	const int LegendX = 208;
	const int LegendY = 49;
	const int LegendLineHeight = 9;
	static readonly Position mapOffset = new(16, 49);
	// Automap icon of event types (index = event type - 1), -1 means no icon
	static readonly int[] eventIcons = [4, 3, -1, 5, -1, 8, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, 7, -1, 3];
	readonly List<ISprite> mapSprites = [];
	readonly List<ISprite> legendSprites = [];
	readonly List<Label> labels = [];
	IMap3D? map;
	int viewX = MapBorder + 1;
	int viewY = MapBorder + 1;
	int viewWidth = MaxViewWidth;
	int viewHeight = MaxViewHeight;

	public enum AutomapIcon
	{
		Unknown,
		Wall,
		Floor,
		Door,
		Exit,
		Treasure,
		Riddlemouth,
		Merchant,
		Teleporter,
		Party,
		PartyNorth,
		PartyEast,
		PartySouth,
		PartyWest
	}

	public override ScreenType Type { get; } = ScreenType.Automap;

	internal override byte ButtonGridPaletteIndex => Palette;

	byte Palette => Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Automap];

	public override void Open(Action? closeAction)
	{
		map = (Game.ScreenHandler.FindMapScreen() as Map3DScreen)?.Map ?? throw new AmberException(ExceptionScope.Application, "The automap needs a 3D map.");

		// The 5 outer tiles of each side are not shown.
		viewWidth = Math.Min(MaxViewWidth, map.Width - 2 * MapBorder);
		viewHeight = Math.Min(MaxViewHeight, map.Height - 2 * MapBorder);
		var position = Game.State.PartyPosition + new Position(1, 1);
		viewX = Math.Clamp(position.X - 10, MapBorder + 1, Math.Max(MapBorder + 1, map.Width - MapBorder - viewWidth + 1));
		viewY = Math.Clamp(position.Y - 8, MapBorder + 1, Math.Max(MapBorder + 1, map.Height - MapBorder - viewHeight + 1));

		base.Open(closeAction);

		Game.SetLayout(Layout.Automap, Palette);
		Game.Cursor.CursorType = CursorType.Sword;

		ShowLegend();
		ShowMap();
	}

	public override void Close()
	{
		foreach (var sprite in mapSprites.Concat(legendSprites))
			sprite.Visible = false;

		mapSprites.Clear();
		legendSprites.Clear();

		foreach (var label in labels)
			label.Destroy();

		labels.Clear();

		base.Close();
	}

	private ISprite CreateIconSprite(Position position, AutomapIcon icon)
	{
		var layer = Game.GetRenderLayer(Layer.UI);
		var sprite = Game.CreateSprite(Layer.UI, position, new Size(IconSize, IconSize), Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.AutomapIcons), Palette)!;
		sprite.TextureOffset = layer.Config.Texture!.GetOffset(Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.AutomapIcons)) + new Position((int)icon * IconGraphicWidth, 0);
		sprite.DisplayLayer = 10;

		return sprite;
	}

	private AutomapIcon PartyIcon => Game.State.SpecialItems.HasFlag(SpecialItems.Compass)
		? AutomapIcon.PartyNorth + (int)Game.State.PartyDirection
		: AutomapIcon.Party;

	private void ShowLegend()
	{
		Label AddText(int x, int y, int width, string text)
		{
			var label = AddLabel(x, y, width, 7, 20);
			label.SetText(text, 15, TextManager.TransparentPaper, Palette);
			labels.Add(label);

			return label;
		}

		string Text(Message message) => Game.GetFullText(Game.LoadMessageText(message));

		AddText(mapOffset.X, 39, MaxViewWidth * IconSize, map!.Name).Alignment = TextAlignment.Center;
		AddText(LegendX, 39, 96, Text(Message.MapLegend)).Alignment = TextAlignment.Center;

		for (int i = 0; i < 9; i++)
		{
			var icon = i == 8 ? PartyIcon : AutomapIcon.Wall + i;
			legendSprites.Add(CreateIconSprite(new Position(LegendX, LegendY + i * LegendLineHeight), icon));
			AddText(LegendX + 14, LegendY + 1 + i * LegendLineHeight, 82, Text(Message.MapLegendWall + i));
		}
	}

	/// <summary>
	/// Gets the automap icon of a 1-based map position.
	/// </summary>
	internal AutomapIcon GetIcon(int x, int y)
	{
		var partyPosition = Game.State.PartyPosition;

		if (x == partyPosition.X + 1 && y == partyPosition.Y + 1)
			return PartyIcon;

		if (!Game.State.IsAutomapped(map!, x - 1, y - 1))
			return AutomapIcon.Unknown;

		var tile = map!.Tiles[x - 1 + (y - 1) * map.Width];

		if (tile.Event != 0)
		{
			int eventType = (int)map.Events[tile.Event - 1].Type;

			if (eventType >= 1 && eventType <= eventIcons.Length && eventIcons[eventType - 1] != -1)
				return (AutomapIcon)eventIcons[eventType - 1];
		}

		if (tile.LabTileIndex == 0)
			return AutomapIcon.Unknown;

		var flags = map.LabTiles[tile.LabTileIndex - 1].Flags;
		bool blocked = flags.HasFlag(LabTileFlags.BlockAllMovement) != flags.HasFlag(LabTileFlags.AutomapReverse);

		return blocked ? AutomapIcon.Wall : AutomapIcon.Floor;
	}

	private void ShowMap()
	{
		foreach (var sprite in mapSprites)
			sprite.Visible = false;

		mapSprites.Clear();

		for (int y = 0; y < viewHeight; y++)
		{
			for (int x = 0; x < viewWidth; x++)
			{
				var icon = GetIcon(viewX + x, viewY + y);

				if (icon != AutomapIcon.Unknown)
					mapSprites.Add(CreateIconSprite(mapOffset + new Position(x * IconSize, y * IconSize), icon));
			}
		}

		RequestButtonSetup();
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(0, ButtonType.ArrowUpLeft);
		buttonGrid.SetButton(1, ButtonType.ArrowUp);
		buttonGrid.SetButton(2, ButtonType.ArrowUpRight);
		buttonGrid.SetButton(3, ButtonType.ArrowLeft);
		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(5, ButtonType.ArrowRight);
		buttonGrid.SetButton(6, ButtonType.ArrowDownLeft);
		buttonGrid.SetButton(7, ButtonType.ArrowDown);
		buttonGrid.SetButton(8, ButtonType.ArrowDownRight);

		if (map == null)
			return;

		// Scrolling is only possible if the map is larger than the view.
		bool horizontal = map.Width - 2 * MapBorder > MaxViewWidth;
		bool vertical = map.Height - 2 * MapBorder > MaxViewHeight;

		foreach (int index in new[] { 0, 2, 3, 5, 6, 8 })
			buttonGrid.EnableButton(index, horizontal);

		foreach (int index in new[] { 0, 1, 2, 6, 7, 8 })
		{
			if (!vertical)
				buttonGrid.EnableButton(index, false);
		}
	}

	internal int ScriptViewX => viewX;

	internal int ScriptViewY => viewY;

	/// <summary>
	/// Scrolls the view by the given offsets (like the arrow buttons).
	/// </summary>
	internal void Scroll(int x, int y)
	{
		viewX = Math.Clamp(viewX + x, MapBorder + 1, Math.Max(MapBorder + 1, map!.Width - MapBorder - viewWidth + 1));
		viewY = Math.Clamp(viewY + y, MapBorder + 1, Math.Max(MapBorder + 1, map.Height - MapBorder - viewHeight + 1));
		ShowMap();
	}

	protected override void ButtonClicked(int index)
	{
		if (index == ExitButtonIndex)
		{
			Game.ScreenHandler.PopScreen();
			return;
		}

		Scroll(index % 3 - 1, index / 3 - 1);
	}
}
