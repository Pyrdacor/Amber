namespace Amberstar.GameData
{
    public struct Tile3D
	{
		public byte LabBlock;
		public byte Event;
	}

	public interface IMap3D : IMap
	{
	    int LabDataIndex { get; }
		Tile3D[] Tiles { get; }
		ILabBlock LabBlocks { get; }
	}
}
