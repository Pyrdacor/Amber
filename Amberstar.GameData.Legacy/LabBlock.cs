using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Legacy;

internal class LabBlock : ILabBlock
{
	private static PerspectiveLocation LocationByIndex(int index, LabBlockType type,
		out BlockFacing blockFacing)
	{
		blockFacing = BlockFacing.FacingPlayer;

		if (type == LabBlockType.Wall)
		{
			return (PerspectiveLocation)(index % 11);
		}
		else if (type == LabBlockType.Object)
		{
			return (PerspectiveLocation)(2 + index * 3);
		}
		else // LabBlockType.Overlay
		{
			blockFacing = index switch
			{
				>= 9 => index % 2 == 0
					? BlockFacing.RightOfPlayer
					: BlockFacing.LeftOfPlayer,
				_ => BlockFacing.FacingPlayer
			};

			if (index < 11)
				return (PerspectiveLocation)(index);

			index %= 11;

			return (PerspectiveLocation)(index % 2 + (index / 2) * 3);
		}
	}

	public static LabBlock Load(IAsset asset, int index)
	{
		var reader = asset.GetReader();

		if (reader.ReadByte() != 0)
			throw new AmberException(ExceptionScope.Data, "Invalid lab block data.");

		var type = (LabBlockType)reader.ReadByte();
		int numPerspectives = reader.ReadByte();

		if (numPerspectives == 0 || numPerspectives > type switch
		{
			LabBlockType.Wall => 13,
			LabBlockType.Overlay => 17,
			LabBlockType.Object => 4,
			_ => int.MaxValue
		})
		{
			throw new AmberException(ExceptionScope.Data, "Invalid lab block data.");
		}

		int numAnimationFrames = reader.ReadByte();

		int[] ReadWords(int count)
		{
			int[] words = new int[count];

			for (int i = 0; i < count; i++)
				words[i] = reader.ReadWord();

			return words;
		}

		var xOffsets = ReadWords(17);
		var yOffsets = ReadWords(17);

		int specialOffsetX = type == LabBlockType.Object ? reader.ReadWord() : 0;
		int specialOffsetY = type == LabBlockType.Object ? reader.ReadWord() : 0;

		var images = new List<IGraphic>(numPerspectives * numAnimationFrames);

		while (true)
		{
			if (reader.Size - reader.Position < 4)
				break;

			var imageDataSize = reader.ReadDword();

			if (imageDataSize == 0)
				break;

			var endPosition = reader.Position + imageDataSize;

			if (endPosition > reader.Size)
				throw new AmberException(ExceptionScope.Data, "Invalid lab block data.");

			var image = GraphicLoader.LoadGraphicWithHeader(reader);

			if (reader.Position != endPosition)
				throw new AmberException(ExceptionScope.Data, "Invalid lab block data.");

			images.Add(image);
		}

		if (images.Count != numPerspectives * numAnimationFrames)
			throw new AmberException(ExceptionScope.Data, "Invalid lab block data.");

		var perspectives = new PerspectiveInfo[numPerspectives];

		for (int i = 0; i < perspectives.Length; i++)
		{
			var location = LocationByIndex(i, type, out var facing);
			var frames = new IGraphic[numAnimationFrames];
			int frameIndex = i;

			for (int n = 0; n < frames.Length; n++)
			{
				frames[n] = images[frameIndex];
				frameIndex += numPerspectives;
			}

			perspectives[i] = new()
			{
				Location = location,
				Facing = facing,
				RenderPosition = new(xOffsets[i], yOffsets[i]),
				SpecialRenderPosition = type == LabBlockType.Object && i == 3 && numAnimationFrames > 1
					? new(specialOffsetX, specialOffsetY)
					: null,
				Frames = frames
			};
		}

		return new()
		{
			Index = index,
			Type = type,
			Perspectives = perspectives,
		};
	}

	public int Index { get; private init; }

	public LabBlockType Type { get; private init; }

	public PerspectiveInfo[] Perspectives { get; private init; } = [];
}