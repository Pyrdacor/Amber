namespace Amber.Common
{
	public enum AssetType
	{
		Unknown,
		// Names
		SpellName,
		SpellSchoolName,
		ClassName,
        RaceName,
        AttributeName,
		SkillName,
		CharInfoText, // labels like "APR", "LP", "SP" etc
		LanguageName,
		ConditionName,
		ItemTypeName,
		// Texts
		MapText,
		ItemText,
		PuzzleText, // Amberstar assembling texts
		UIText,
		Message,
        // Graphics
        Layout,
		UIGraphic,
		Button,
		StatusIcon,
		Graphics80x80,
		ItemGraphic,
		Background,
		SkyGradient,
		Window,
		Cursor,
		// ...
		// Other stuff
		Savegame,
		Tileset,
		Font,
		Map,
		LabData,
		LabBlock,
		Person,
		Monster,
		MonsterGroup,
		Place,
		PlaceName,
		Merchant,
		Item,
		Palette,
		Music,
	}

	public struct AssetIdentifier
	{
		public AssetType Type;
		public int Index;

		public AssetIdentifier(AssetType type, int index)
		{
			Type = type;
			Index = index;
		}

		public override string ToString()
		{
			return $"[{Type}, {Index}]";
		}
	}
}
