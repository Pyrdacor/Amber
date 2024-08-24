using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Legacy;

internal class LabData : ILabData
{
	static readonly Dictionary<int, LabBlock> labBlockCache = [];

	private static LabBlock GetLabBlock(int index, Amber.Assets.Common.IAssetProvider assetProvider)
	{
		if (labBlockCache.TryGetValue(index, out var labBlock))
			return labBlock;

		var asset = assetProvider.GetAsset(new(AssetType.LabBlock, index));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Lab block {index} not found.");

		labBlock = LabBlock.Load(asset);

		labBlockCache.Add(index, labBlock);

		return labBlock;
	}

	public static LabData Load(IAsset asset, Amber.Assets.Common.IAssetProvider assetProvider)
	{
		var reader = asset.GetReader();

		if (reader.ReadByte() != 0)
			throw new AmberException(ExceptionScope.Data, "Invalid lab data.");

		int numImages = reader.ReadByte();
		int numLabBlocks = reader.ReadByte();
		var labBlocks = new LabBlock[numLabBlocks];

		for (int i = 0; i < numLabBlocks; i++)
			labBlocks[i] = GetLabBlock(reader.ReadByte(), assetProvider);

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
			LabBlocks = labBlocks,
		};
	}

	public int CeilingIndex { get; private init; }
	public int FloorIndex { get; private init; }
	public int PaletteIndex { get; private init; }
	public ILabBlock[] LabBlocks { get; private init; } = [];
}