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
		// Images ...
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
