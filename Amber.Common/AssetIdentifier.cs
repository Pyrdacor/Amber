namespace Amber.Common
{
	public enum AssetType
	{
		Unknown,
		// Names
		SpellName,
		SpellSchoolName,
		ClassName,
		SkillName,
		CharInfoText, // labels like "APR", "LP", "SP" etc
		RaceName,
		ConditionName,
		ItemTypeName,
		// Texts
		MapText,
		ItemText,
		PuzzleText, // Amberstar assembling texts
		// Graphics
		Layout,
		UIGraphic,
		Button,
		StatusIcon,
		Graphics80x80,
		ItemGraphic,
		// ...
		// Other stuff
		Font,
		Map,
		Lab,
		Player,
		NPC,
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
