namespace Amberstar.Game
{
	public enum Layer
	{
		Layout, // opaque, drawn in the back
		UI, // UI elements, supports transparency
		Map2D, // all tilsets
		Text,
		Map3D, // images, block movement
		Billboard3D, // freely 3D map (billboards)
		Map3DNew, // TODO: freely 3D map (floor and walls)
	}
}
