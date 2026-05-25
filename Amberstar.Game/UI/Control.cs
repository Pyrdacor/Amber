namespace Amberstar.Game.UI;

internal abstract class Control
{
    public abstract bool Visible { get; set; }
    public abstract byte DisplayLayer { get; set; }
    public abstract byte PaletteIndex { get; set; }

    public virtual void Destroy() { }
}
