using Amber.Assets.Common;
using Amber.Serialization;

namespace Amberstar.GameData.Legacy;

internal class Tileset : ITileset
{
	class Tile(int frameCount, int imageIndex, int minimapColorIndex, TileFlags tileFlags) : ITile
	{
		public int FrameCount { get; } = frameCount;

		public int ImageIndex { get; } = imageIndex;

		public int MinimapColorIndex { get; } = minimapColorIndex;

		public TileFlags Flags { get; } = tileFlags;
	}

	const int ImageSize = 16 * 16 / 2;
	readonly List<ITile> tiles = [];
	readonly List<IGraphic> graphics = [];

	public Tileset(IDataReader dataReader)
	{
		PlayerSpriteIndex = dataReader.ReadWord();

		var frameCounts = dataReader.ReadBytes(ITileset.TileCount);
		var imageIndices = dataReader.ReadBytes(ITileset.TileCount * 2);
		var tileFlags = dataReader.ReadBytes(ITileset.TileCount * 4);
		var colors = dataReader.ReadBytes(ITileset.TileCount);

		Palette = PaletteLoader.LoadWidePalette(dataReader);

		while (dataReader.Position <= dataReader.Size - ImageSize)
		{
			graphics.Add(GraphicLoader.LoadGraphicWithHeader(dataReader));
		}

		int ToWord(byte[] data, int index)
		{
			return (data[index] << 8) | data[index + 1];
		}

		dword ToDword(byte[] data, int index)
		{
			return (((dword)data[index] << 24) | ((dword)data[index + 1] << 16) | ((dword)data[index + 2] << 8) | data[index + 3]);
		}

		for (int i = 0; i < ITileset.TileCount; i++)
		{
			tiles.Add(new Tile(frameCounts[i], ToWord(imageIndices, i * 2), colors[i], (TileFlags)ToDword(tileFlags, i * 4)));
		}
	}

	public int PlayerSpriteIndex { get; }

	public IReadOnlyList<ITile> Tiles => tiles.AsReadOnly();

	public IGraphic Palette { get; }

	public IReadOnlyList<IGraphic> Graphics => graphics.AsReadOnly();
}
