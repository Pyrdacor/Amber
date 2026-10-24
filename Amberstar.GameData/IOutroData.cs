using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData;

/// <summary>
/// The data of the outro (EXTRO.68K, a separate program of the original). The chancellor speaks to the party
/// in the throne room, then the people watch the dragon and the eagles from the balcony.
/// </summary>
public interface IOutroData
{
	public const int PictureWidth = 320;
	public const int PictureHeight = 200;

	/// <summary>
	/// The throne room (320x200). The lines from <see cref="ThroneRoomLowerLine"/> use the lower palette.
	/// </summary>
	IGraphic ThroneRoom { get; }
	IGraphic ThroneRoomPalette { get; }
	IGraphic ThroneRoomLowerPalette { get; }
	public const int ThroneRoomLowerLine = 100;

	/// <summary>
	/// The balcony (320x200). The sky (above <see cref="LandscapeLine"/>) uses the sky palette which is replaced
	/// by the red moon palette later. The landscape lines from <see cref="LandscapeLowerLine"/> use the lower palette.
	/// </summary>
	IGraphic Balcony { get; }
	IGraphic SkyPalette { get; }
	IGraphic RedMoonPalette { get; }
	IGraphic LandscapePalette { get; }
	IGraphic LandscapeLowerPalette { get; }
	public const int LandscapeLine = 68;
	public const int LandscapeLowerLine = 82;

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

	IReadOnlyList<IGraphic> Eagles { get; }
	IReadOnlyList<IGraphic> Dragon { get; }
	IReadOnlyList<int> DragonYOffsets { get; }
	IReadOnlyList<IGraphic> Flyer { get; }
	IReadOnlyList<int> FlyerYOffsets { get; }
	IGraphic Moon { get; }

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
