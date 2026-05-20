namespace Amberstar.Game;

public enum KeyMovement3D
{
    /// <summary>
    /// A and D turn instead of strafe.
    /// Left and Right as well.
    /// </summary>
    WASD,

    /// <summary>
    /// A and D strafe, Q and E turn.
    /// Left and Right same as A and D.
    /// Del and PageDown same as Q and E.
    /// </summary>
    QEWASD,
}

public enum ShortcutAction
{
    UseItem,
    EquipItem,
}

public record Shortcut(ShortcutAction Action, Key Key, KeyModifiers KeyModifiers = KeyModifiers.None)
{
    public MouseButtons MouseButtons { get; set; } = MouseButtons.None;

    public Shortcut(ShortcutAction action, MouseButtons mouseButtons, KeyModifiers keyModifiers = KeyModifiers.None)
        : this(action, Key.Invalid, keyModifiers)
    {
        MouseButtons = mouseButtons;
    }
}

// TODO: Use the stuff from the config :D
public interface IConfiguration
{
    int WindowX { get; set; }

    int WindowY { get; set; }

    int WindowClientWidth { get; set; }

    int WindowClientHeight { get; set; }

    int MonitorIndex { get; set; }

    bool Fullscreen { get; set; }

    GameOptions GameOptions { get; set; }

    KeyMovement3D KeyMovement3D { get; set; }

    Shortcut[] Shortcuts { get; set; }
}
