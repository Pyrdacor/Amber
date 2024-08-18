using Amber.Common;

namespace Amber.Renderer.Common;

public interface ITexture
{
    Size Size { get; }

    void Use();
}
