using Amberstar.Game;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Frontend
{
	internal class GraphicIndexProvider
	(
		int buttonOffset,
		int statusIconOffset,
		int uiGraphicOffset,
		int image80x80Offset,
		int itemGraphicOffset,
		int windowGraphicOffset,
		int cursorGraphicOffset,
		Dictionary<int, int> backgroundGraphicIndices,
		Dictionary<int, int> cloudGraphicIndices,
		Dictionary<int, Dictionary<PerspectiveLocation, Dictionary<BlockFacing, int>>> labBlockImageIndices,
        Dictionary<int, int> personPortraitIndices,
		int puzzleGraphicOffset,
		int combatBackgroundOffset,
		int tacticalIconOffset,
		Dictionary<int, int> monsterGraphicOffsets,
		int spellAnimationOffset,
		int damageDigitOffset,
		Dictionary<OutroGraphic, int> outroGraphicOffsets,
		Dictionary<char, int> outroGlyphIndices,
		Dictionary<IntroGraphic, int> introGraphicOffsets,
		int thalionLogoIndex,
		int? loadingScreenIndex

    ) : IGraphicIndexProvider
	{
		public int Get80x80ImageIndex(Image80x80 image) => image80x80Offset + (int)image;

		public int GetButtonIndex(ButtonType button) => buttonOffset + (int)button;

		public int GetItemGraphicIndex(ItemGraphic graphic) => itemGraphicOffset + (int)graphic;

		public int GetStatusIconIndex(StatusIcon statusIcon) => statusIconOffset + (int)statusIcon;

		public int GetUIGraphicIndex(UIGraphic graphic) => uiGraphicOffset + (int)graphic;

		public int GetLabBlockGraphicIndex(int labBlockIndex, PerspectiveLocation location, BlockFacing facing) => labBlockImageIndices[labBlockIndex][location][facing];

		public int GetBackgroundGraphicIndex(int index) => backgroundGraphicIndices[index];

		public int GetCloudGraphicIndex(int index) => cloudGraphicIndices[index];

		public int GetWindowGraphicIndex(bool dark) => windowGraphicOffset + (dark ? 0 : 1);

		public int GetCursorGraphicIndex(CursorType cursorType) => cursorGraphicOffset + (int)cursorType;

        public int GetPersonPortraitIndex(int index) => personPortraitIndices[index];

		public int GetPuzzleGraphicIndex(PuzzleGraphic graphic, int index = 0) => puzzleGraphicOffset + (int)graphic + index;

		public int GetCombatBackgroundIndex(int background) => combatBackgroundOffset + background - 1;

		public int GetTacticalIconIndex(int icon) => tacticalIconOffset + icon;

		public int GetMonsterGraphicIndex(int monsterGraphic, int frame) => monsterGraphicOffsets[monsterGraphic] + frame;

		public int GetSpellAnimationIconIndex(int animation, int icon) => spellAnimationOffset + (animation - 1) * 6 + icon;

		public int GetDamageDigitIndex(int digit) => damageDigitOffset + digit;

		public int GetOutroGraphicIndex(OutroGraphic graphic, int frame = 0) => outroGraphicOffsets[graphic] + frame;

		public int? GetOutroGlyphIndex(char ch) => outroGlyphIndices.TryGetValue(ch, out int index) ? index : null;

		public int GetIntroGraphicIndex(IntroGraphic graphic, int frame = 0) => introGraphicOffsets[graphic] + frame;

		public int GetThalionLogoIndex() => thalionLogoIndex;

		public int? GetLoadingScreenIndex() => loadingScreenIndex;
    }
}
