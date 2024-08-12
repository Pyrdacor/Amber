namespace Amber.Renderer;

public interface ILayer
{
	int Index { get; }
	bool Visible { get; set; }
	int TextureAssetIndex { get; set; }
}
