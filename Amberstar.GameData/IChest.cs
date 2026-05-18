namespace Amberstar.GameData;

public interface IChest
{
	public const int SlotCount = 12;

	int Index { get; }

	IItem?[] Items { get; }
}
