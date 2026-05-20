namespace Amberstar.Game;

[Flags]
public enum GameOptions : uint
{
    /// <summary>
    /// The original had only one save slot, but we can support more.
    /// </summary>
    MultipleSaveSlots = (1 << 0),

    /// <summary>
    /// In the original when dragging items, they are masked (only white color, as it is a cursor).
    /// We support dragging the item directly without masking it.
    /// </summary>
    UnmaskedDraggedItem = (1 << 1),

    /// <summary>
    /// Dungeon maps in style of Ambermoon.
    /// </summary>
    AmbermoonStyleDungeonMap = (1 << 2),

    /// <summary>
    /// In original you have to click a button to activate item pickup.
    /// For example in chests or inventory.
    /// 
    /// If this option is active, the button is replace:
    /// - In inventory by a new "Assembly all food" button
    /// - In chest by a new "Distribute all items" button
    /// 
    /// Items can then just be dragged & dropped directly while
    /// no other mode like "Examine item" is active.
    /// </summary>
    AdvancedItemPickup = (1 << 3),

    /// <summary>
    /// Free 3D movement like in Ambermoon.
    /// </summary>
    Free3DMovement = (1 << 4),

    /// <summary>
    /// If active each screen transition which usually uses fading,
    /// will instead show the original loading screen with the dwarf.
    /// </summary>
    ShowDwarfLoadingScreen = (1 << 5),

    /// <summary>
    /// In the original you can only pick up items from chests, but
    /// not place items there. With this option you can. It breaks
    /// compatibility with original savegames.
    /// </summary>
    AllowChestItemStorage = (1 << 6),

    None = 0,
    All = uint.MaxValue,
    Default = All & ~Free3DMovement & ~ShowDwarfLoadingScreen,
}

// Optional
/*- Ambermoon style dungeon map
-WASD, QEWASD, Left/Right arrow turns or strafes
- Direct item drag&drop
- Free 3D movement like Ambermoon
- Show loading screen(with dwarf) instead of fading
- Increased light duration
- Replace "give item" buttons with "distribute items"
- Store items in chests (will break original savegames)
- Multiple save slots(not sure if we want to force it)

In general a new savegame format might be good as an option.
And I would say it should be the default.If the legacy
savegame format is chosen, some other options are automatically
deactivated and disabled.

// Not optional
- Shortcut keys for stuff like "use item", "equip item", etc.Maybe also double clicks, or Shift/Ctrl + Click.*/

partial class Game
{
    // TODO: Store them in config file later (and read them from there as well)
    GameOptions gameOptions = GameOptions.UnmaskedDraggedItem; // TODO: for now we implement the original, later set this to GameOptions.Default

    // TODO: Add more and more of them.
    static readonly GameOptions supportedGameOptions = GameOptions.UnmaskedDraggedItem;

    public bool IsOptionSet(GameOptions option) => supportedGameOptions.HasFlag(option) && gameOptions.HasFlag(option);
}
