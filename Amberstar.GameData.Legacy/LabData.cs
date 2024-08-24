using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Legacy;

internal class LabData : ILabData
{
	public static LabData Load(IAsset asset, Dictionary<int, ILabBlock> labBlocks)
	{
		var reader = asset.GetReader();

		if (reader.ReadByte() != 0)
			throw new AmberException(ExceptionScope.Data, "Invalid lab data.");

		int numImages = reader.ReadByte();
		int numLabBlocks = reader.ReadByte();
		var blocks = new ILabBlock[numLabBlocks];

		for (int i = 0; i < numLabBlocks; i++)
			blocks[i] = labBlocks[(int)reader.ReadByte()];

		if (reader.ReadByte() != 0)
			throw new AmberException(ExceptionScope.Data, "Invalid lab data.");

		int unknown = reader.ReadByte();

		if (reader.ReadByte() != 2)
			throw new AmberException(ExceptionScope.Data, "Invalid lab data.");

		int ceilingIndex = reader.ReadByte();
		int floorIndex = reader.ReadByte();

		if (reader.ReadByte() != 1)
			throw new AmberException(ExceptionScope.Data, "Invalid lab data.");

		int paletteIndex = reader.ReadByte();

		return new()
		{
			CeilingIndex = ceilingIndex,
			FloorIndex = floorIndex,
			PaletteIndex = paletteIndex,
			LabBlocks = blocks,
		};
	}

	public int CeilingIndex { get; private init; }
	public int FloorIndex { get; private init; }
	public int PaletteIndex { get; private init; }
	public ILabBlock[] LabBlocks { get; private init; } = [];
}