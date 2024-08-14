namespace Amber.Common
{
	public enum AssetType
	{
		Unknown,
		Image,
		Audio,
		Text,
		TextList,
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
	}
}
