using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Serialization;

public interface IGraphicLoader
{
	IGraphic[] LoadWindowGraphics(bool dark);
	IPaletteGraphic Load80x80Graphic(Image80x80 index);
	IGraphic LoadItemGraphic(ItemGraphic index);
	IReadOnlyDictionary<int, IGraphic> LoadAllBackgroundGraphics();
    IReadOnlyDictionary<int, IGraphic> LoadAllCloudGraphics();
    IReadOnlyDictionary<DayTime, Color[]> LoadSkyGradients();
    IReadOnlyDictionary<int, IGraphic> LoadPersonPortraits();
	/// <summary>
	/// Loads the graphics of the altar puzzle in the order of <see cref="PuzzleGraphic"/>.
	/// </summary>
	IGraphic[] LoadPuzzleGraphics();
	/// <summary>
	/// Loads a combat background (176x112).
	/// </summary>
	IGraphic LoadCombatBackground(int index);
	/// <summary>
	/// Loads the frames of a monster graphic. There are 3 groups of frames for the
	/// 3 monster rows (see <see cref="ICombatData.GetMonsterFrameCounts"/>).
	/// </summary>
	IGraphic[] LoadMonsterGraphic(int index);
	/// <summary>
	/// Loads the 16x16 icons of the tactical battle field (3 animation frames each:
	/// classes of the party members and then the monster graphics + 9).
	/// </summary>
	IGraphic[] LoadTacticalIcons();
}
