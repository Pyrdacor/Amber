using Amberstar.Game;

namespace Amberstar;

internal class Configuration : IConfiguration
{
    const int DefaultWindowX = 10;
    const int DefaultWindowY = 10;
    const int DefaultWindowClientWidth = 1280;
    const int DefaultWindowClientHeight = 800;
    const int DefaultMonitorIndex = 0;

    public int WindowX { get; set; } = DefaultWindowX;
    public int WindowY { get; set; } = DefaultWindowY;
    public int WindowClientWidth { get; set; } = DefaultWindowClientWidth;
    public int WindowClientHeight { get; set; } = DefaultWindowClientHeight;
    public int MonitorIndex { get; set; } = DefaultMonitorIndex;
    public bool Fullscreen { get; set; } = false;
    public GameOptions GameOptions { get; set; } = GameOptions.UnmaskedDraggedItem; // TODO: for now we implement the original, later set this to GameOptions.Default
    public KeyMovement3D KeyMovement3D { get; set; } = KeyMovement3D.QEWASD;
    public Shortcut[] Shortcuts { get; set; } =
    [
        new(ShortcutAction.UseItem, MouseButtons.Left, KeyModifiers.Control), // Ctrl + Click
        new(ShortcutAction.EquipItem, MouseButtons.Left, KeyModifiers.Shift), // Shift + Click
    ];
}
