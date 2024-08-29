using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class CursorLoader(AssetProvider assetProvider) : ICursorLoader
{
	private readonly Dictionary<CursorType, ICursor> cursors = [];

	public ICursor LoadCursor(CursorType cursorType)
	{
		if (cursors.TryGetValue(cursorType, out var cursor))
			return cursor;

		var asset = assetProvider.GetAsset(new(AssetType.Cursor, (int)cursorType));

		if (asset == null)
			throw new AmberException(ExceptionScope.Data, $"Cursor {cursorType} not found.");

		var reader = asset.GetReader();

		int hotspotX = reader.ReadWord();
		int hotspotY = reader.ReadWord();

		// Load graphic
		var graphic = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(32), 1);
		var mask = Graphic.FromBitPlanes(16, 16, reader.ReadBytes(32), 1);

		graphic.MaskWith(mask, 14);

		cursor = new Cursor(new(hotspotX, hotspotY), graphic);

		cursors.Add(cursorType, cursor);

		return cursor;
	}
}
