namespace Amberstar.GameData.Events;

public enum TrapType
{
    None, // abort
    DamageTrap, // whole party, text 7
    PoisonNeedle, // active player, text 8
    PoisonGasCloud, // whole party, text 9
    BlindingFlash, // whole party, text 10
    ParalyzingGasCloud, // whole party, text 11
    StoneGaze, // active player, text 12
    Disease // active player, text 13
}

// Each trap type has a fixed text index (Code0003Sys) and specifies if only
// the active player or whole party is affected.

// Traps check against LUCK.
// If the trap affects the whole party and nobody gets hurt,
// text index 1 (Code0002Sys) is used to display a text.

public interface IExecuteTrapEvent : IEvent
{
    /// <summary>
    /// Byte 1
    /// </summary>
    TrapType TrapType { get; }

    /// <summary>
    /// Byte 3
    /// </summary>
    byte Damage { get; }

    /// <summary>
    /// Byte 4
    /// Index of map text (seems to be limit to the range 0..25).
    /// </summary>
    byte TextIndex { get; }
}
