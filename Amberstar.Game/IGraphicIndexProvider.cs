using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

public interface IGraphicIndexProvider
{
	int GetUIGraphicIndex(UIGraphic graphic);
    int GetButtonIndex(ButtonType button);
	int GetStatusIconIndex(StatusIcon statusIcon);
	int Get80x80ImageIndex(Image80x80 image);
	int GetItemGraphicIndex(ItemGraphic graphic);
	int GetWindowGraphicIndex(bool dark);
	int GetBackgroundGraphicIndex(int index);
	int GetCloudGraphicIndex(int index);
	int GetLabBlockGraphicIndex(int labBlockIndex, PerspectiveLocation location, BlockFacing facing);
	int GetCursorGraphicIndex(CursorType cursorType);
	int GetPersonPortraitIndex(int index);
	int GetPuzzleGraphicIndex(PuzzleGraphic graphic, int index = 0);
	int GetCombatBackgroundIndex(int background);
	int GetTacticalIconIndex(int icon);
	int GetMonsterGraphicIndex(int monsterGraphic, int frame);
	/// <summary>
	/// The spell animations are 1-based with 6 icons each.
	/// </summary>
	int GetSpellAnimationIconIndex(int animation, int icon);
	int GetDamageDigitIndex(int digit);
	int GetOutroGraphicIndex(OutroGraphic graphic, int frame = 0);
	/// <summary>
	/// Gets the texture index of the glyph of a character of the outro texts or null if it has no glyph.
	/// </summary>
	int? GetOutroGlyphIndex(char ch);
	/// <summary>
	/// Gets the texture index of the loading screen (the dwarf) or null if it is not available.
	/// </summary>
	int? GetLoadingScreenIndex();
}
