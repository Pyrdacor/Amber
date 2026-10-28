using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData;

/// <summary>
/// The palettes of the outro pictures. Some lines use another palette (raster interrupts in the original).
/// </summary>
public enum OutroPalette
{
	ThroneRoom,
	/// <summary>
	/// From line <see cref="IOutroData.ThroneRoomLowerLine"/> color 5 differs.
	/// </summary>
	ThroneRoomLower,
	/// <summary>
	/// The sky of the balcony above line <see cref="IOutroData.LandscapeLine"/>.
	/// </summary>
	Sky,
	/// <summary>
	/// Replaces the sky palette when the third moon gets red.
	/// </summary>
	RedMoon,
	Landscape,
	/// <summary>
	/// From line <see cref="IOutroData.LandscapeLowerLine"/> color 5 differs.
	/// </summary>
	LandscapeLower
}

/// <summary>
/// The graphics of the outro. The pictures are split into the parts with different palettes.
/// </summary>
public enum OutroGraphic
{
	ThroneRoomTop,
	ThroneRoomBottom,
	BalconySky,
	BalconyLandscape,
	BalconyLower,
	LeftTorch,
	RightTorch,
	Chancellor,
	PartyFigure,
	PulsingMoon,
	Dragon,
	Eagle,
	Moon,
	BalconyPerson1,
	BalconyPerson2,
	BalconyPerson3
}

/// <summary>
/// The data of the outro (EXTRO.68K, a separate program of the original). The chancellor speaks to the party
/// in the throne room, then the people watch the dragon, an eagle and the pulsing third moon from the balcony.
/// </summary>
public interface IOutroData
{
	public const int PictureWidth = 320;
	public const int PictureHeight = 200;

	/// <summary>
	/// The throne room (320x200). The lines from <see cref="ThroneRoomLowerLine"/> use the lower palette.
	/// </summary>
	IGraphic ThroneRoom { get; }
	public const int ThroneRoomLowerLine = 100;

	/// <summary>
	/// The balcony (320x200). The sky (above <see cref="LandscapeLine"/>) uses the sky palette which is replaced
	/// by the red moon palette later. The landscape lines from <see cref="LandscapeLowerLine"/> use the lower palette.
	/// </summary>
	IGraphic Balcony { get; }
	public const int LandscapeLine = 68;
	public const int LandscapeLowerLine = 82;

	/// <summary>
	/// Gets a palette while it fades: 0 is the normal palette, 1 to <see cref="FadeSteps"/> - 1 fade in from black
	/// (each color channel is increased by 1 per step), -1 to -<see cref="FadeSteps"/> fade out (each channel is
	/// decreased by 1 per step). The palettes have 16 colors, in the Amiga version the palettes of the pictures have
	/// more colors (color 5 changes every few lines in the original, these pixels use the colors from 16).
	/// </summary>
	IGraphic GetPalette(OutroPalette palette, int fadeStep = 0);

	/// <summary>
	/// The number of fading steps (7 on the Atari ST, 15 on the Amiga) and the vertical blanks per step.
	/// </summary>
	int FadeSteps { get; }
	int FadeInVerticalBlanks { get; }
	int FadeOutVerticalBlanks { get; }

	/// <summary>
	/// Gets the frames of a graphic (the picture parts have 1 frame).
	/// </summary>
	IReadOnlyList<IGraphic> GetGraphics(OutroGraphic graphic);

	/// <summary>
	/// The characters of all texts (for which glyphs are needed).
	/// </summary>
	IEnumerable<char> UsedCharacters => ChancellorTexts.Concat(BalconyTexts).SelectMany(text => text).Distinct();

	IReadOnlyList<IGraphic> LeftTorch { get; }
	IReadOnlyList<IGraphic> RightTorch { get; }
	IReadOnlyList<IGraphic> Chancellor { get; }

	/// <summary>
	/// 3 figures for the party members (0: big, 1: dwarves, 2: gnomes) with their y offsets.
	/// </summary>
	IReadOnlyList<IGraphic> PartyFigures { get; }
	IReadOnlyList<int> PartyFigureYOffsets { get; }

	/// <summary>
	/// The position of the figure of each party slot (x is the center).
	/// </summary>
	IReadOnlyList<Position> PartyFigurePositions { get; }

	/// <summary>
	/// The third moon which pulses red at the end (4 frames).
	/// </summary>
	IReadOnlyList<IGraphic> PulsingMoon { get; }
	IReadOnlyList<IGraphic> Dragon { get; }
	IReadOnlyList<int> DragonYOffsets { get; }
	IReadOnlyList<IGraphic> Eagle { get; }
	IReadOnlyList<int> EagleYOffsets { get; }
	IGraphic Moon { get; }
	Position MoonPosition { get; }

	/// <summary>
	/// The frames of the 3 people on the balcony and their positions.
	/// </summary>
	IReadOnlyList<IReadOnlyList<IGraphic>> BalconyPeople { get; }
	IReadOnlyList<Position> BalconyPeoplePositions { get; }

	/// <summary>
	/// The script of the balcony scene: 0 n = wait n steps, 1-3 n = frame of a person, 4 = next text,
	/// 5 = red moon, 0xff = end.
	/// </summary>
	IReadOnlyList<byte> BalconyScript { get; }

	/// <summary>
	/// The texts of the chancellor and the texts of the balcony scene (lines separated by '\n').
	/// </summary>
	IReadOnlyList<string> ChancellorTexts { get; }
	IReadOnlyList<string> BalconyTexts { get; }

	/// <summary>
	/// Gets the glyph (8x5 pixels, color 15) of a character or null for characters without a glyph (like spaces).
	/// </summary>
	IGraphic? GetGlyph(char ch);
	public const int GlyphWidth = 8;
	public const int GlyphHeight = 5;
	public const int CharacterWidth = 6;
	public const int LineHeight = 7;

	ISong? Music { get; }
}
