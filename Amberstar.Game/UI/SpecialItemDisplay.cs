using Amber.Common;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.UI;

/// <summary>
/// Shows the used special items right of the map view (Show_special_items in the original):
/// compass, Amberstar, day/night display (magical picture), wind chain, coordinates (map locator) and time (clock).
/// Like in the original the graphics are drawn with the palette of the map.
/// </summary>
internal sealed class SpecialItemDisplay(Game game)
{
	const byte DisplayLayer = 10;
	const int TextInkColorIndex = 15;
	const int TextPaperColorIndex = 2;
	// Day/night graphic by hour (Daynight_tab in the original)
	static readonly UIGraphic[] DayTimeGraphics =
	[
		UIGraphic.Night, UIGraphic.Night, UIGraphic.Night, UIGraphic.Night, UIGraphic.Night, UIGraphic.Night,
		UIGraphic.Dawn, UIGraphic.Dawn,
		UIGraphic.Day, UIGraphic.Day, UIGraphic.Day, UIGraphic.Day, UIGraphic.Day,
		UIGraphic.Day, UIGraphic.Day, UIGraphic.Day, UIGraphic.Day, UIGraphic.Day,
		UIGraphic.Dusk, UIGraphic.Dusk,
		UIGraphic.Night, UIGraphic.Night, UIGraphic.Night, UIGraphic.Night
	];
	readonly List<Image> images = [];
	readonly List<IRenderText> texts = [];
	string? shownState = null;
	bool covered = false;

	/// <summary>
	/// While another screen covers the map nothing is shown.
	/// </summary>
	public bool Covered
	{
		get => covered;
		set
		{
			covered = value;

			if (value)
				Hide();
		}
	}

	/// <summary>
	/// The texts which are shown (used by scenario scripts).
	/// </summary>
	public IEnumerable<string> ShownItems { get; private set; } = [];

	/// <summary>
	/// Updates the display. The compass shows the given direction (always north on 2D maps).
	/// The coordinates are 1-based (on world maps they are global).
	/// </summary>
	public void Update(byte paletteIndex, Direction compassDirection, Position coordinates)
	{
		var specialItems = game.State.SpecialItems;
		int hour = game.State.Hour;
		string state = $"{paletteIndex} {specialItems} {compassDirection} {coordinates} {hour} {game.State.Minute}";

		if (covered || state == shownState)
			return;

		Hide();
		shownState = state;

		var shownItems = new List<string>();
		var graphicIndexProvider = game.GraphicIndexProvider;

		void AddImage(int x, int y, int width, int height, int textureIndex)
		{
			var image = new Image(game, x, y, width, height, textureIndex, DisplayLayer, Layer.UI, paletteIndex, true);
			images.Add(image);
		}

		void AddText(int x, int y, string text)
		{
			var renderText = game.TextManager.Create(text, TextInkColorIndex, TextPaperColorIndex, paletteIndex);
			renderText.Show(x, y, (byte)(DisplayLayer + 1));
			texts.Add(renderText);
			shownItems.Add(text);
		}

		if (specialItems.HasFlag(SpecialItems.Compass))
		{
			var direction = (int)compassDirection % 4;
			AddImage(208, 49 + 16, 32, 32, graphicIndexProvider.GetUIGraphicIndex((UIGraphic)((int)UIGraphic.CompassNorth + direction)));
			shownItems.Add($"Compass {(Direction)direction}");
		}

		if (specialItems.HasFlag(SpecialItems.Amberstar))
		{
			AddImage(208 + 32, 49, 32, 32, graphicIndexProvider.GetUIGraphicIndex(UIGraphic.Amberstar));
			shownItems.Add("Amberstar");
		}

		if (specialItems.HasFlag(SpecialItems.MagicalPicture))
		{
			var graphic = DayTimeGraphics[hour % DayTimeGraphics.Length];
			AddImage(208 + 64, 49 + 16, 32, 32, graphicIndexProvider.GetUIGraphicIndex(graphic));
			shownItems.Add(graphic.ToString());
		}

		if (specialItems.HasFlag(SpecialItems.WindChain))
		{
			AddImage(208 + 32, 49 + 32, 32, 16, graphicIndexProvider.GetUIGraphicIndex(UIGraphic.Windchain));
			shownItems.Add("WindChain");
		}

		if (specialItems.HasFlag(SpecialItems.MapLocator))
		{
			AddImage(208, 49, 32, 16, graphicIndexProvider.GetButtonIndex(ButtonType.EmptyArea));
			AddText(208 + 2, 49 + 2, $"X:{coordinates.X:000}");
			AddText(208 + 2, 49 + 10, $"Y:{coordinates.Y:000}");
		}

		if (specialItems.HasFlag(SpecialItems.Clock))
		{
			AddImage(208 + 64, 49, 32, 16, graphicIndexProvider.GetButtonIndex(ButtonType.EmptyArea));
			AddText(208 + 65, 49 + 5, $"{hour:00}:{game.State.Minute:00}");
		}

		ShownItems = shownItems;
	}

	/// <summary>
	/// Hides the display (e.g. while another screen is shown). The next update shows it again.
	/// </summary>
	public void Hide()
	{
		images.ForEach(image => image.Destroy());
		images.Clear();
		texts.ForEach(text => text.Delete());
		texts.Clear();
		shownState = null;
		ShownItems = [];
	}
}
