namespace Amberstar.Game
{
	public enum Layer
	{
		Layout, // opaque, drawn in the back
		UI, // UI elements, supports transparency
		MapUnderlay, // all tilsets
		MapOverlay, // all tilsets
		Map3D, // freely 3D map (floor and walls)
		Billboard3D, // freely 3D map (billboards)
		Map3DLegay, // images, block movement
		Text,
	}
}
