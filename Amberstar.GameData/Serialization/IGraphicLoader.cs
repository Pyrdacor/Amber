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
	/// <summary>
	/// Loads the 16x16 icons of the spell animations in battles (6 icons per animation).
	/// </summary>
	IGraphic[] LoadSpellAnimationIcons();

	const int LoadingScreenWidth = 128;
	const int LoadingScreenHeight = 112;
	/// <summary>
	/// The loading screen fades in and out in this number of steps (each color component is decreased by 1 per step).
	/// </summary>
	const int LoadingScreenFadeSteps = 7;

	/// <summary>
	/// Loads the loading screen of the original (a dwarf with "Loading, please wait", 128x112).
	/// Null if it is not available.
	/// </summary>
	IGraphic? LoadLoadingScreen();
	/// <summary>
	/// Loads the palette of the loading screen at a fade step (0: normal, <see cref="LoadingScreenFadeSteps"/>: black).
	/// </summary>
	IGraphic? LoadLoadingScreenPalette(int fadeStep);
}
