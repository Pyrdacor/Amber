using Amber.Common;
using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// A map while the party is on it.
///
/// The tiles are a copy of the loaded map with all saved tile changes applied.
/// In the original the map was loaded again on every visit, so tile changes
/// which are not saved (event without save index) are lost after leaving
/// the map. This is the same here as the loaded map itself is never changed.
/// </summary>
internal abstract class MapInstance(IMap map) : IMap
{
	IMap IMapEventProvider.Map => this;
	public int Index => map.Index;
	public int Width => map.Width;
	public int Height => map.Height;
	public MapType Type => map.Type;
	public MapFlags Flags => map.Flags;
	public string Name => map.Name;
	public int SongIndex => map.SongIndex;
	public MapCharacter[] Characters => map.Characters;
	public Position[][] CharacterPositions => map.CharacterPositions;
	public List<IEvent> Events => map.Events;

	/// <summary>
	/// Changes the tile at the 1-based position (like in event data and tile changes).
	/// </summary>
	public abstract void ChangeTile(int x, int y, int tileIndex);

	protected void ApplyTileChanges(IEnumerable<TileChange> tileChanges)
	{
		foreach (var tileChange in tileChanges.Where(tileChange => tileChange.MapIndex == Index))
			ChangeTile(tileChange.X, tileChange.Y, tileChange.TileIndex);
	}
}

internal sealed class MapInstance2D : MapInstance, IMap2D
{
	readonly ITileset tileset;

	public MapInstance2D(IMap2D map, ITileset tileset, IEnumerable<TileChange> tileChanges)
		: base(map)
	{
		this.tileset = tileset;
		TilesetIndex = map.TilesetIndex;
		Tiles = [.. map.Tiles];
		ApplyTileChanges(tileChanges);
	}

	public int TilesetIndex { get; }
	public Tile2D[] Tiles { get; }

	public override void ChangeTile(int x, int y, int tileIndex)
	{
		if (x < 1 || y < 1 || x > Width || y > Height)
			return;

		ref var tile = ref Tiles[x - 1 + (y - 1) * Width];

		// The tile flags decide if the new tile replaces the overlay or the underlay.
		if (tileIndex != 0 && tileset.Tiles[tileIndex - 1].Flags.HasFlag(TileFlags.Overlay))
			tile.Overlay = (byte)tileIndex;
		else
			tile.Underlay = (byte)tileIndex;
	}
}

internal sealed class MapInstance3D : MapInstance, IMap3D
{
	public MapInstance3D(IMap3D map, IEnumerable<TileChange> tileChanges)
		: base(map)
	{
		LabDataIndex = map.LabDataIndex;
		LabTiles = map.LabTiles;
		Tiles = [.. map.Tiles];
		ApplyTileChanges(tileChanges);
	}

	public int LabDataIndex { get; }
	public Tile3D[] Tiles { get; }
	public LabTile[] LabTiles { get; }

	public override void ChangeTile(int x, int y, int tileIndex)
	{
		if (x < 1 || y < 1 || x > Width || y > Height)
			return;

		Tiles[x - 1 + (y - 1) * Width].LabTileIndex = (byte)tileIndex;
	}
}
