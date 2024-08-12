namespace Amberstar.GameData
{
	public struct Tile2D
	{
		public byte Underlay;
		public byte Overlay;
		public byte Event;
	}

	public interface IMap2D : IMap
	{
		Tile2D[] Tiles { get; }
	}
}
