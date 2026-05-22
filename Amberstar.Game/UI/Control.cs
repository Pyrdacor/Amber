namespace Amberstar.Game.UI;

internal abstract class Control
{
    public abstract bool Visible { get; set; }

    public virtual void Destroy() { }
}
